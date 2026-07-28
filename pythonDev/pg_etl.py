"""Simple ETL helpers to create schema and upsert CSVs into Postgres `visits` table.

Usage examples:
  python3 -c "from pg_etl import create_schema; import psycopg2; create_schema({'host':'...','dbname':'...','user':'...','password':'...'})"

  python3 -c "from pg_etl import etl_csv_to_visits; etl_csv_to_visits('postgresql://user:pw@host/db', 'staging.csv')"
"""
from typing import Dict
import pandas as pd


def create_schema(conn_params: Dict):
    """Create minimal normalized schema and required indexes.

    conn_params: dict acceptable by psycopg2.connect
    """
    import psycopg2

    ddl = """
    CREATE TABLE IF NOT EXISTS clinics (
        id TEXT PRIMARY KEY,
        name TEXT
    );

    CREATE TABLE IF NOT EXISTS visitors (
        id BIGINT PRIMARY KEY
    );

    CREATE TABLE IF NOT EXISTS visits (
        id SERIAL PRIMARY KEY,
        visitor_id BIGINT REFERENCES visitors(id),
        clinic_id TEXT REFERENCES clinics(id),
        visit_date DATE NOT NULL,
        visits INT NOT NULL DEFAULT 1,
        UNIQUE(visitor_id, clinic_id, visit_date)
    );

    CREATE INDEX IF NOT EXISTS idx_visits_clinic_date ON visits (clinic_id, visit_date);
    CREATE INDEX IF NOT EXISTS idx_visits_visitor_date ON visits (visitor_id, visit_date);
    """

    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()
    cur.execute(ddl)
    conn.commit()
    cur.close()
    conn.close()


def etl_csv_to_visits(db_url: str, csv_path: str):
    """Load a CSV with columns [visitor_id,clinic_id,visit_date,visits] and upsert into visits.

    db_url: sqlalchemy-style URL e.g. postgresql://user:pw@host/db
    """
    from sqlalchemy import create_engine, text

    df = pd.read_csv(csv_path, parse_dates=['visit_date'])
    # normalize column names
    df = df.rename(columns={
        'visitor_id': 'visitor_id',
        'clinic_id': 'clinic_id',
        'visit_date': 'visit_date',
        'visits': 'visits'
    })

    engine = create_engine(db_url)
    # write staging table
    df.to_sql('staging_visits', engine, if_exists='replace', index=False)

    # upsert aggregated rows into visits
    upsert_sql = text("""
    INSERT INTO visits (visitor_id, clinic_id, visit_date, visits)
    SELECT visitor_id, clinic_id, visit_date::date, SUM(visits) FROM staging_visits GROUP BY visitor_id, clinic_id, visit_date
    ON CONFLICT (visitor_id, clinic_id, visit_date) DO UPDATE SET visits = visits + EXCLUDED.visits;
    """)

    with engine.begin() as conn:
        conn.execute(upsert_sql)

    # drop staging
    with engine.begin() as conn:
        conn.execute(text('DROP TABLE IF EXISTS staging_visits'))

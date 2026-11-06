#!/usr/bin/env bash
# Deploys the NorthwindCatalog database by running the scripts in order.
#
# Usage:
#   SQL_SERVER=localhost,1433 SA_PASSWORD='Your_strong_Passw0rd' \
#   APP_LOGIN=catalog_api APP_PASSWORD='Another_strong_Passw0rd' ./deploy.sh
#
# Also used by the db-init container in docker-compose.yml.
set -euo pipefail

SQL_SERVER="${SQL_SERVER:-localhost,1433}"
SA_USER="${SA_USER:-sa}"
: "${SA_PASSWORD:?SA_PASSWORD must be set}"
APP_LOGIN="${APP_LOGIN:-catalog_api}"
: "${APP_PASSWORD:?APP_PASSWORD must be set}"

SQLCMD="${SQLCMD:-$(command -v sqlcmd || echo /opt/mssql-tools18/bin/sqlcmd)}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/scripts"

run() {
  echo ">> $1"
  # -C trusts the self-signed dev certificate; -b exits non-zero on errors
  "$SQLCMD" -S "$SQL_SERVER" -U "$SA_USER" -P "$SA_PASSWORD" -C -b -I \
    -v AppLogin="$APP_LOGIN" AppPassword="$APP_PASSWORD" \
    -i "$SCRIPT_DIR/$1"
}

# Wait for SQL Server to accept connections (container startup)
for i in {1..30}; do
  if "$SQLCMD" -S "$SQL_SERVER" -U "$SA_USER" -P "$SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  echo "Waiting for SQL Server ($i/30)..."; sleep 2
done

run 01_CreateDatabase.sql
run 02_Schema.sql
run 03_SeedData.sql
run 04_Security.sql
echo "Database deployment complete."

# Medical information module
# create a basic pandas dataframe with some medical information
# Distinct # of visitors for a particular year by clinic.
# 
import pandas as pd
clinic_data = {
    "Clinic": ["A", "B", "C", "A", "B", "C"],
    "Year": [2025, 2025, 2025, 2024, 2024, 2024],
    "Visitors": [26, 31, 13, 130, 160, 110]
}

# create a visitor dataframe with unique visitors for a particular year by clinic       
unique_visitors_data = {
    "VisitorId": [1, 2, 3, 4, 2],
    "Clinic": ["A", "B", "C", "A", "B"],
    "Year": [2025, 2025, 2025, 2025, 2025],
    "Visits": [10, 15, 13, 12, 16]
}
    

df_clinic = pd.DataFrame(clinic_data)
df_unique_visitors = pd.DataFrame(unique_visitors_data)

def get_distinct_visitors_by_year(df_clinic, unique_visitors_df, year: int):
    
    # use unique_visitors_df to get distinct visitors for each clinic and year and 
    # use that data in data dataframe to Group by Year and Clinic, then count distinct visitors
    
    # distinct_visitors = unique_visitors_df.groupby(['Clinic', 'Year'])['VisitorId'].nunique().reset_index(name='VisitorId')
    # distinct_v_from_data = pd.merge(df_clinic[df_clinic['Year'] == year], distinct_visitors, on=['Clinic', 'Year'], how='left')
    
    #return distinct_v_from_data
    
    # (1) validate/clean visitor rows,
    # (2) collapse multiple rows per visitor/clinic/year by summing visits,
    # (3) aggregate per clinic-year to get total visits, distinct visitor count, and visitor ids,
    # (4) merge into df_clinic and return enriched columns
    uv = unique_visitors_df.dropna(subset=['VisitorId']).copy()
    uv['VisitorId'] = uv['VisitorId'].astype(int)

    per_visitor = uv.groupby(['VisitorId', 'Clinic', 'Year'], as_index=False)['Visits'].sum()

    agg = per_visitor.groupby(['Clinic', 'Year'], as_index=False).agg(
        Visits=('Visits', 'sum'),
        DistinctVisitors=('VisitorId', 'nunique'),
        VisitorIds=('VisitorId', lambda s: sorted(s.unique().tolist()))
    )

    merged = df_clinic[df_clinic['Year'] == year].merge(agg, on=['Clinic', 'Year'], how='left')
    merged['Visits'] = merged['Visits'].fillna(0).astype(int)
    merged['DistinctVisitors'] = merged['DistinctVisitors'].fillna(0).astype(int)
    merged['VisitorIds'] = merged['VisitorIds'].apply(lambda x: x if isinstance(x, list) else [])
    merged['VisitorId'] = merged['VisitorIds'].apply(lambda ids: ids[0] if len(ids) == 1 else ids)
    return merged[['Clinic','Year','Visits','VisitorId','VisitorIds','DistinctVisitors']]

medical_info = get_distinct_visitors_by_year(df_clinic, df_unique_visitors, 2025)
print(medical_info)


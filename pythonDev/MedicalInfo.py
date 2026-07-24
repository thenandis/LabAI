# Medical information module
# create a basic pandas dataframe with some medical information
# Distinct # of visitors for a particular year by clinic.
# 
import pandas as pd
data = {
    "Clinic": ["A", "B", "C", "A", "B", "C"],
    "Year": [2025, 2025, 2025, 2024, 2024, 2024],
    "Visitors": [22, 31, 13, 130, 160, 110]
}

# create a visitor dataframe with unique visitors for a particular year by clinic       
unique_visitors_data = {
    "VisitorId": [1, 2, 3, 4, 2],
    "Clinic": ["A", "B", "C", "A", "B"],
    "Year": [2025, 2025, 2025, 2025, 2025],
    "Visits": [10, 15, 13, 12, 16]
}
    

df = pd.DataFrame(data)

def get_distinct_visitors_by_year(df, unique_visitors_df, year: int):
    
    # use unique_visitors_df to get distinct visitors for each clinic and year and 
    # use that data in data dataframe to Group by Year and Clinic, then count distinct visitors
    
    distinct_visitors = unique_visitors_df.groupby(['Clinic', 'Year'])['VisitorId'].nunique().reset_index()
    distinct_v_from_data = pd.merge(df[df['Year'] == year], distinct_visitors, on=['Clinic', 'Year'], how='left')
    return distinct_v_from_data

medical_info = get_distinct_visitors_by_year(df, pd.DataFrame(unique_visitors_data), 2025)
print(medical_info)


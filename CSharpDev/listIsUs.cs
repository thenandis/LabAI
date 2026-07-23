// From a list<list<string>> which has country names and id. If country name is not there then there is a -1.
// Example: List<List<string>> countryList = new List<List<string>> { new List<string> { "1", "USA" }, 
// new List<string> { "2", "Canada" }, new List<string> { "3", "-1" },  
// new List<string> { "4", "Canada" } };
// the output should be a list with counts of each country name.

using System;
using System.Collections.Generic;

class Program
{
    public static void Main(string[] args)
    {
        List<List<string>> countryList = new List<List<string>>
        {
            new List<string> { "1", "USA" },
            new List<string> { "2", "Canada" },
            new List<string> { "3", "-1" },
            new List<string> { "4", "Canada" }
        };

        Dictionary<string, int> countryCounts = CountCountries(countryList);

        foreach (var kvp in countryCounts)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }

    static Dictionary<string, int> CountCountries(List<List<string>> countryList)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (var entry in countryList)
        {
            string countryName = entry[1];

            if (countryName != "-1")
            {
                if (counts.ContainsKey(countryName))
                {
                    counts[countryName]++;
                }
                else
                {
                    counts[countryName] = 1;
                }
            }
        }

        return counts;
    }
}
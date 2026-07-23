using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        List<int> numbers = new List<int> { 10, 5, 20, 15, 30, 25 };
        
        int secondHighest = FindSecondHighest(numbers);
        Console.WriteLine($"Second highest number: {secondHighest}");
    }
    
    static int FindSecondHighest(List<int> numbers)
    {
        return numbers.OrderByDescending(x => x).Distinct().ElementAt(1);
    }
}

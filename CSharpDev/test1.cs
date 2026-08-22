using System;
using System.Collections.Generic;
using System.Linq;

/*
To run test1.cs, uncomment the following line in CSharpDev.csproj:
<Compile Include="test1.cs" />
terminal command: dotnet run --project CSharpDev/CSharpDev.csproj

FindShortestSubarrayWithSameDegree - Reads the two-line challenge input when provided.
Prints only the shortest subarray length, as required.
Preserves the original second-highest-number example when no input is present.
Uses a one-pass O(T) time algorithm with dictionaries for frequency and first position.
*/
class Program
{
    
    static void Main()
    {
        if (Console.In.Peek() != -1)
        {
            int elementCount = int.Parse(Console.ReadLine()!);
            List<int> inputNumbers = Console.ReadLine()!
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(elementCount)
                .Select(int.Parse)
                .ToList();

            Console.WriteLine(FindShortestSubarrayWithSameDegree(inputNumbers));
            return;
        }

        List<int> numbers = new List<int> { 10, 5, 20, 15, 30, 25 };
        
        int secondHighest = FindSecondHighest(numbers);
        Console.WriteLine($"Second highest number: {secondHighest}");

        // Example input: 5 followed by 1 2 2 3 1. Prints 2.
        int smallestLength = FindShortestSubarrayWithSameDegree(new List<int> { 1, 2, 2, 3, 1 });
        Console.WriteLine(smallestLength);
    }
    
    static int FindSecondHighest(List<int> numbers)
    {
        return numbers.OrderByDescending(x => x).Distinct().ElementAt(1);
    }

    static int FindShortestSubarrayWithSameDegree(List<int> numbers)
    {
        Dictionary<int, int> frequencies = new Dictionary<int, int>();
        Dictionary<int, int> firstIndices = new Dictionary<int, int>();
        int degree = 0;
        int shortestLength = numbers.Count;

        for (int index = 0; index < numbers.Count; index++)
        {
            int number = numbers[index];

            if (!frequencies.ContainsKey(number))
            {
                frequencies[number] = 0;
                firstIndices[number] = index;
            }

            frequencies[number]++;
            int length = index - firstIndices[number] + 1;

            if (frequencies[number] > degree)
            {
                degree = frequencies[number];
                shortestLength = length;
            }
            else if (frequencies[number] == degree)
            {
                shortestLength = Math.Min(shortestLength, length);
            }
        }

        return shortestLength;
    }
}

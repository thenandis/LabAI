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
        // if (Console.In.Peek() != -1)
        // {
        //     int elementCount = int.Parse(Console.ReadLine()!);
        //     List<int> inputNumbers = Console.ReadLine()!
        //         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        //         .Take(elementCount)
        //         .Select(int.Parse)
        //         .ToList();

        //     Console.WriteLine(FindShortestSubarrayWithSameDegree(inputNumbers));
        //     return;
        // }

        // List<int> numbers = new List<int> { 10, 5, 20, 15, 30, 25 };
        
        // int secondHighest = FindSecondHighest(numbers);
        // Console.WriteLine($"Second highest number: {secondHighest}");

        // // Example input: 5 followed by 1 2 2 3 1. Prints 2.
        // int smallestLength = FindShortestSubarrayWithSameDegree(new List<int> { 1, 2, 2, 3, 1 });
        // Console.WriteLine(smallestLength);

        // // Example input: [2, 7, 11, 15], target: 9. Prints [0, 1].
        // int[] indices = TwoSum(new int[] { 2, 6, 11, 15 }, 9);
        // Console.WriteLine($"Two sum indices: [{indices[0]}, {indices[1]}]");  

        // // Example input: "babad". Prints "bab" or "aba".
        // string longestPalindromeSimple = LongestPalindromeSimple("racecarxyz");
        // Console.WriteLine($"Longest palindromic substring (simple): {longestPalindromeSimple}");

        // // Example input: "babad". Prints "bab" or "aba".
        // string longestPalindrome = LongestPalindrome("racecarxyz");
        // Console.WriteLine($"Longest palindromic substring: {longestPalindrome}");

        // // Example input: "cbbd". Prints "bb".
        // string longestPalindromeManacher = LongestPalindromeManacher("racecarxyz");
        // Console.WriteLine($"Longest palindromic substring (Manacher's): {longestPalindromeManacher}"); 

        // // Example input: "ababa". Prints 9 and ["a", "b", "aba", "bab", "ababa"].
        // var (count, palindromes) = findAllPalindromes("racecarxyx");
        // Console.WriteLine($"Total palindromic substrings: {count}");
        // Console.WriteLine($"All palindromic substrings: {string.Join(", ", palindromes)}");
    
        // var s = "x*b"; // string to match
        // var p = "..b"; // pattern to match
        // bool isMatch = IsMatch(s,p); // Example usage of IsMatch
        // Console.WriteLine($"does {s} match {p}: {isMatch}");
    
        // var nums = new int[] { -2,1,-3,4,-1,2,1,-5,4 }; //{0,2,0,2}; 
        // int maxSubArraySum = MaxSubArray(nums); // Example usage of MaxSubArray
        // Console.WriteLine($"Maximum subarray sum: {maxSubArraySum}");

        // var palindromeCheck = "racecar";
        // bool isPalindrome = IsPalindrome(palindromeCheck); // Example usage of IsPalindrome
        // Console.WriteLine($"Is '{palindromeCheck}' a palindrome? {isPalindrome}");

        // var testString = "missswiss";
        // char? firstNonRepeatingChar = FirstNonRepeatingCharacter(testString); // Example usage of FirstNonRepeatingCharacter
        // if (firstNonRepeatingChar.HasValue) {
        //     Console.WriteLine($"First non-repeating character in '{testString}': {firstNonRepeatingChar.Value}");
        // } else {
        //     Console.WriteLine($"No non-repeating character found in '{testString}'");  
        // }

        // var parenthesesString = "{[()]})";
        // bool isBalanced = IsBalancedParentheses(parenthesesString); // Example usage of IsBalancedParentheses
        // Console.WriteLine($"Is '{parenthesesString}' balanced? {isBalanced}");

        // var transactions = new List<(string Account, decimal Amount)> {
        //     ("A123", 100.50m),
        //     ("B456", 200.75m),
        //     ("A123", 50.25m),
        //     ("C789", 300.00m),
        //     ("B456", 150.00m)
        // };
        // var groupedTransactions = GroupTransactionsByAccount(transactions); // Example usage of GroupTransactionsByAccount
        // Console.WriteLine("Grouped Transactions by Account:");
        // foreach (var kvp in groupedTransactions) {
        //     Console.WriteLine($"Account: {kvp.Key}, Total Amount: {kvp.Value}");
        // }

        // // WRONG — binary floating point cannot represent 0.1 exactly
        // double a = 0.1, b = 0.2;
        // Console.WriteLine(a + b == 0.3);        // False
        // Console.WriteLine(a + b);               // 0.30000000000000004

        // // CORRECT — decimal is base-10, exact for currency
        // decimal x = 0.1m, y = 0.2m;
        // Console.WriteLine(x + y == 0.3m);       // True

        // // Rounding: banker's rounding is the default and is usually what you want
        // decimal amount = 2.5m;
        // Math.Round(amount);                          // 2  (to even — default)
        // Math.Round(amount, MidpointRounding.AwayFromZero);   // 3

        // var rounded = Math.Round(2.675m, 2); // 2.67 (not 2.68) — banker's rounding
        // Console.WriteLine(rounded); // 2.67

        // Console.WriteLine(FibonacciIterative(50)); // 12586269025

        var nums = new int[] { 100, 4, 200, 1, 3, 2 };
        int longestStreak = new Program().LongestConsecutive(nums);
        Console.WriteLine($"Longest consecutive sequence length: {longestStreak}");

        var longestSequence = FindLongestConsecutiveSequence(nums);
        Console.WriteLine($"Longest consecutive sequence: {string.Join(", ", longestSequence)}");
    }

    public int LongestConsecutive(int[] nums)
    {
        if (nums == null || nums.Length == 0) return 0;

        // 1. Load all elements into a HashSet for O(1) existence checks.
        // This eliminates duplicates naturally.
        HashSet<int> numSet = new HashSet<int>(nums);
        int longestStreak = 0;

        // 2. Iterate through the numbers using a sequential collection or loop
        foreach (int num in numSet)
        {
            // CRITICAL STEP FOR LEAD PERFORMANCE:
            // Check if 'num' is the absolute START of a sequence.
            // If num - 1 exists in the set, 'num' is NOT the start, so we skip it.
            // This ensures each sequence is only traversed EXACTLY ONCE.
            if (!numSet.Contains(num - 1))
            {
                int currentNum = num;
                int currentStreak = 1;

                // 3. Incrementally trace the sequence forward using O(1) lookups
                while (numSet.Contains(currentNum + 1))
                {
                    currentNum += 1;
                    currentStreak += 1;
                }

                // Keep track of the maximum streak length found
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
        }

        return longestStreak;
    }

    public static List<int> FindLongestConsecutiveSequence(int[] nums)
    {
        // Edge case: return an empty list if input is null or empty
        if (nums == null || nums.Length == 0) 
        {
            return new List<int>();
        }

        // 1. Populate the HashSet for O(1) lookups
        HashSet<int> numSet = new HashSet<int>(nums);
        List<int> longestSequence = new List<int>();
        Console.WriteLine($"Input numbers: {string.Join(", ", numSet)}");

        // 2. Scan the set to identify sequence boundaries
        foreach (int num in numSet)
        {
            Console.WriteLine($"Evaluating number: {num}");
            // Only start building a sequence if 'num' is the absolute minimum anchor
            if (!numSet.Contains(num - 1))
            {
                int currentNum = num;
                List<int> currentSequence = new List<int>();
                Console.WriteLine($"Starting new sequence at: {currentNum}");

                // 3. Trace the sequence forward and append to our ordered list
                while (numSet.Contains(currentNum))
                {
                    currentSequence.Add(currentNum);
                    currentNum++;
                    Console.WriteLine($"Added {currentNum} to current sequence.");
                }
                Console.WriteLine($"Finished sequence starting at {num}: {string.Join(", ", currentSequence)}");
                

                // 4. Track the longest list found
                if (currentSequence.Count > longestSequence.Count)
                {
                    longestSequence = currentSequence;
                }
            }
        }

        return longestSequence;
    }


    // Iterative — O(n) time, O(1) space, no stack depth risk
    public static long FibonacciIterative(int n)
    {
        if (n <= 1) return n;
        long prev = 0, curr = 1;
        for (int i = 2; i <= n; i++) {
            (prev, curr) = (curr, prev + curr); // tuple swap to avoid temporary variable
            Console.WriteLine($"Fibonacci({i}) = {curr}");
        }
        return curr;
    }

    // Thread-safe transfer between accounts
    // static class Account {
    //     private static string Id { get; } // add required modifier for Id to be accessible in Transfer method
    //     static decimal Balance { get; set; }

    //     static Account(string id, decimal initialBalance) {
    //         Id = id;
    //         Balance = initialBalance;
    //     }
    // }
    // static void Transfer(Account from, Account to, decimal amount) {
    //     if (from == null || to == null) throw new ArgumentNullException();
    //     if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");

    //     // Lock both accounts in a consistent order to avoid deadlocks
    //     var firstLock = from.Id.CompareTo(to.Id) < 0 ? from : to;
    //     var secondLock = from.Id.CompareTo(to.Id) < 0 ? to : from;

    //     lock (firstLock) {
    //         lock (secondLock) {
    //             if (from.Balance < amount) throw new InvalidOperationException("Insufficient funds.");
    //             from.Balance -= amount;
    //             to.Balance += amount;
    //         }
    //     }
    // }   

    


    // Group Transactions by account and calculate the total amount for each account.
    static Dictionary<string, decimal> GroupTransactionsByAccount(List<(string Account, decimal Amount)> transactions) {
        var groupedTransactions = new Dictionary<string, decimal>();

        // groupedTransactions = transactions
        //     .GroupBy(t => t.Account)
        //     .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var summary = transactions
            .GroupBy(t => t.Account)
            .Select(g => new { 
                Account = g.Key, 
                TotalAmount = g.Sum(t => t.Amount),
                Count = g.Count(),
                LatestTransaction = g.Max(t => t.Amount) // Example of getting the latest transaction amount
            });

        foreach (var item in summary) {
            Console.WriteLine($"Account: {item.Account}, Total Amount: {item.TotalAmount}, Count: {item.Count}, Latest Transaction: {item.LatestTransaction}"); 
            groupedTransactions[item.Account] = item.TotalAmount;
        }
        // foreach (var transaction in transactions) {
        //     if (groupedTransactions.ContainsKey(transaction.Account)) {
        //         groupedTransactions[transaction.Account] += transaction.Amount;
        //     } else {
        //         groupedTransactions[transaction.Account] = transaction.Amount;
        //     }
        // }

        return groupedTransactions;
    }
    static bool IsBalancedParentheses(string s) {
        if (string.IsNullOrEmpty(s)) return true;

        Stack<char> stack = new Stack<char>();
        Dictionary<char, char> matchingPairs = new Dictionary<char, char> {
            { ')', '(' },
            { '}', '{' },
            { ']', '[' }
        };

        foreach (char c in s) {
            if (matchingPairs.ContainsValue(c)) {
                stack.Push(c);
            } else if (matchingPairs.ContainsKey(c)) {
                if (stack.Count == 0 || stack.Pop() != matchingPairs[c]) {
                    return false;
                }
            }
        }

        return stack.Count == 0;
    }   

    static char? FirstNonRepeatingCharacter(string s) {
        if (string.IsNullOrEmpty(s)) return null;
        Dictionary<char, int> charCount = new Dictionary<char, int>();

        // Count occurrences of each character
        foreach (char c in s) {
            if (charCount.ContainsKey(c)) {
                charCount[c]++;
            } else {
                charCount[c] = 1;
            }
        }
        // Find the first character with a count of 1
        foreach (char c in s) {
            if (charCount[c] == 1) {
                return c;
            }
        }

        return null; // Return null if no non-repeating character is found
    }
    static bool IsPalindrome(string s) {
        if (string.IsNullOrEmpty(s)) return true;

        int left = 0;
        int right = s.Length - 1;

        while (left < right) {
            if (s[left] != s[right]) {
                return false;
            }
            left++;
            right--;
        }

        return true;
    }
    static int MaxSubArray(int[] nums) {
        if (nums == null || nums.Length == 0) 
            return 0;
          
        int currentSum = nums[0];
        int maxSum = nums[0];

        if (nums.Length == 1)
            return maxSum;
          
        for (int i=1;i<nums.Length;i++)
        {
            currentSum = Math.Max(nums[i], (currentSum + nums[i]));
            Console.WriteLine("current {0}, max {1}",currentSum,maxSum);
            if (currentSum > maxSum)
            {
                maxSum = currentSum;
            }
        }

        return maxSum;
    }


    //Given a string s, return the longest palindromic substring in s. You may assume that the maximum length of s is 1000.
    static string LongestPalindromeSimple(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        int start = 0, maxLen = 1;
        for (int i = 0; i < s.Length; i++) {
            // odd-length center at i
            int l = i, r = i;
            while (l >= 0 && r < s.Length && s[l] == s[r]) {
                int len = r - l + 1;
                if (len > maxLen) { start = l; maxLen = len; }
                l--; r++;
            }
            // even-length center between i and i+1
            l = i; r = i + 1;
            while (l >= 0 && r < s.Length && s[l] == s[r]) {
                int len = r - l + 1;
                if (len > maxLen) { start = l; maxLen = len; }
                l--; r++;
            }
        }
        return s.Substring(start, maxLen);
    }
    
    static string LongestPalindrome(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        int start = 0, end = 0;
        for (int i = 0; i < s.Length; i++) {
            int len1 = ExpandAroundCenter(s, i, i); // Odd length
            int len2 = ExpandAroundCenter(s, i, i + 1); // Even length
            int len = Math.Max(len1, len2);
            if (len > end - start) {
                start = i - (len - 1) / 2;
                end = i + len / 2;
            }
        }
        return s.Substring(start, end - start + 1);
    }

    static int ExpandAroundCenter(string s, int left, int right) {
        while (left >= 0 && right < s.Length && s[left] == s[right]) {
            left--;
            right++;
        }
        return right - left - 1;
    }

static string LongestPalindromeManacher(string s) {
    // handle trivial case
    if (string.IsNullOrEmpty(s)) return "";

    // Transform input to avoid even/odd cases and bounds checks:
    // example "aba" -> "^#a#b#a#$" ^ and $ are stoppers, # are separators
    var sb = new System.Text.StringBuilder("^");
    foreach (char c in s) { sb.Append('#'); sb.Append(c); }
    sb.Append("#$");
    string t = sb.ToString(); // transformed string

    int n = t.Length;
    int[] p = new int[n]; // p[i] = radius of palindrome centered at i in t
    int center = 0;       // center of the rightmost palindrome seen so far
    int right = 0;        // right boundary (exclusive) of that palindrome

    for (int i = 1; i < n - 1; i++) {
        int mirror = 2 * center - i; // mirror position of i around current center

        // If i is inside current right boundary, we can initialize p[i]
        // from its mirror to avoid recomputing characters already known to be palindromic.
        if (i < right) {
            p[i] = Math.Min(right - i, p[mirror]);
        }

        // Try to expand palindrome centered at i beyond the initialized radius.
        // Compare characters outward one step at a time.
        while (t[i + 1 + p[i]] == t[i - 1 - p[i]]) {
            p[i]++;
        }

        // If expanded past right, update center and right to the new palindrome.
        if (i + p[i] > right) {
            center = i;
            right = i + p[i];
        }
    }

    // Find the maximum radius in p to locate the longest palindrome.
    int maxLen = 0, centerIndex = 0;
    for (int i = 1; i < n - 1; i++) {
        if (p[i] > maxLen) {
            maxLen = p[i];
            centerIndex = i;
        }
    }

    // Map the result back to original string indices:
    // start = (centerIndex - 1 - maxLen) / 2
    int start = (centerIndex - 1 - maxLen) / 2;
    return s.Substring(start, maxLen);
}

static (int, List<string>) findAllPalindromes(string s) {
    int count = 0;
    List<string> palindromes = new List<string>();

    for (int i = 0; i < s.Length; i++) {
        // Odd length palindromes
        ExpandAndCollect(s, i, i, ref count, palindromes);
        // Even length palindromes
        ExpandAndCollect(s, i, i + 1, ref count, palindromes);
    }

    return (count, palindromes);
}
static void ExpandAndCollect(string s, int left, int right, ref int count, List<string> palindromes) {
    while (left >= 0 && right < s.Length && s[left] == s[right]) {
        count++;
        palindromes.Add(s.Substring(left, right - left + 1));
        left--;
        right++;
    }
}

static bool IsMatch(string s, string p) {
        int m = s.Length;
        int n = p.Length;
        
        // We only need two rows at any given time
        bool[] prevRow = new bool[n + 1];
        
        // Base case: Empty string matches empty pattern
        prevRow[0] = true;
        
        // Initialize the first row (matching empty string 's' against pattern 'p')
        for (int j = 2; j <= n; j++) {
            if (p[j - 1] == '*') {
                prevRow[j] = prevRow[j - 2];
            }
        }
        
        for (int i = 1; i <= m; i++) {
            bool[] currentRow = new bool[n + 1];
            // currentRow[0] is automatically false since a non-empty string 's' 
            // cannot match an empty pattern 'p'
            
            for (int j = 1; j <= n; j++) {
                if (p[j - 1] == s[i - 1] || p[j - 1] == '.') {
                    currentRow[j] = prevRow[j - 1];
                }
                else if (p[j - 1] == '*') {
                    // Match 0 occurrences
                    currentRow[j] = currentRow[j - 2];
                    
                    // Match 1 or more occurrences
                    char precedingChar = p[j - 2];
                    if (precedingChar == s[i - 1] || precedingChar == '.') {
                        currentRow[j] = currentRow[j] || prevRow[j];
                    }
                }
            }
            // Move to the next row
            prevRow = currentRow;
        }
        
        return prevRow[n];
    }

static int[] TwoSum(int[] nums, int target) {
        Dictionary<int, int> numToIndex = new Dictionary<int, int>();
        for (int i = 0; i < nums.Length; i++)
        {
            int complement = target - nums[i];
            if (numToIndex.ContainsKey(complement))
            {
                return new int[] { numToIndex[complement], i };
            }
            numToIndex[nums[i]] = i;
        }
        return new int[] {-1,-1}; // Return an -1 array if no solution is found
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

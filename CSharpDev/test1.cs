using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Practice set modeled after a typical CodeSignal-style coding assessment.
// Four stages of increasing difficulty, each with a solution + quick self-tests in Main.

public static class Program
{
    public static void Main()
    {
        Stage1_Warmup();
        Stage2_Easy();
        Stage3_Medium();
        Stage4_Hard();
    }

    // ---------------------------------------------------------------
    // STAGE 1 (Warmup / very easy): basic loops, string/array handling
    // Problem: Given an array of integers, return the count of numbers
    // that are strictly greater than the average of the array.
    // ---------------------------------------------------------------
    public static int CountAboveAverage(int[] nums)
    {
        if (nums == null || nums.Length == 0) return 0;
        double avg = nums.Average();          // compute the average once
        return nums.Count(n => n > avg);       // count elements strictly greater than it
    }

    private static void Stage1_Warmup()
    {
        Console.WriteLine("=== Stage 1: Warmup ===");
        Print(CountAboveAverage(new[] { 1, 2, 3, 4, 5 }), 2);      // avg=3 -> {4,5}
        Print(CountAboveAverage(new[] { 10, 10, 10 }), 0);
        Print(CountAboveAverage(new int[0]), 0);
    }

    // ---------------------------------------------------------------
    // STAGE 2 (Easy): hash maps / frequency counting
    // Problem: Given a string, find the first non-repeating character
    // and return its index. Return -1 if none exists.
    // ---------------------------------------------------------------
    public static int FirstUniqueChar(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;

        // Pass 1: count how many times each character appears in the string.
        var counts = new Dictionary<char, int>();
        foreach (char c in s)
            counts[c] = counts.GetValueOrDefault(c, 0) + 1;

        // Pass 2: walk the string in order and return the index of the
        // first character whose total count is exactly 1.
        for (int i = 0; i < s.Length; i++)
            if (counts[s[i]] == 1) return i;

        return -1; // no unique character found
    }

    private static void Stage2_Easy()
    {
        Console.WriteLine("=== Stage 2: Easy ===");
        Print(FirstUniqueChar("leetcode"), 0);
        Print(FirstUniqueChar("loveleetcode"), 2);
        Print(FirstUniqueChar("aabb"), -1);
    }

    // ---------------------------------------------------------------
    // STAGE 3 (Medium): two pointers / sliding window
    // Problem: Given a string, find the length of the longest substring
    // without repeating characters.
    // ---------------------------------------------------------------
    public static int LongestUniqueSubstring(string s)
    {
        // lastSeen maps each character to the most recent index where it appeared.
        var lastSeen = new Dictionary<char, int>();

        // [start, end] is the current window that contains no duplicate characters.
        // "end" advances one step at a time; "start" only jumps forward when a
        // duplicate is found inside the current window.
        int start = 0, best = 0;

        for (int end = 0; end < s.Length; end++)
        {
            char c = s[end];

            // If we've seen this character before AND that earlier occurrence is
            // still inside our current window, the window must shrink: move
            // "start" to just after that earlier occurrence so the duplicate
            // is excluded from the window.
            if (lastSeen.TryGetValue(c, out int idx) && idx >= start)
                start = idx + 1;

            // Record/update the most recent position of this character.
            lastSeen[c] = end;

            // Current window size is (end - start + 1); keep the best seen so far.
            best = Math.Max(best, end - start + 1);
        }

        return best;
    }

    private static void Stage3_Medium()
    {
        Console.WriteLine("=== Stage 3: Medium ===");
        Print(LongestUniqueSubstring("abcabcbb"), 3);
        Print(LongestUniqueSubstring("bbbbb"), 1);
        Print(LongestUniqueSubstring("pwwkew"), 3);
        Print(LongestUniqueSubstring(""), 0);
    }

    // ---------------------------------------------------------------
    // STAGE 4 (Hard): dynamic programming
    // Problem: Given a list of coin denominations and a target amount,
    // return the minimum number of coins needed to make that amount.
    // Return -1 if it cannot be made.
    // ---------------------------------------------------------------
    public static int MinCoins(int[] coins, int amount)
    {
        // dp[a] = minimum number of coins needed to make exactly amount "a".
        // We build this up bottom-up from a=0 to a=amount ("tabulation").
        int[] dp = new int[amount + 1];
        /*
            Array.Fill(dp, int.MaxValue);
            ps through every position from index 0 to the very end of the array
            and assigns it int.MaxValue (2,147,483,647). 
            Example result: Your array now looks like this: 
            [2147483647, 2147483647, 2147483647, 2147483647, 2147483647]
        */
        Array.Fill(dp, int.MaxValue);   // int.MaxValue acts as "impossible / not computed yet"
        dp[0] = 0;                      // base case: 0 coins are needed to make amount 0

        // For every amount "a" from 1 up to the target...
        for (int a = 1; a <= amount; a++)
        {
            // ...try using each coin denomination as the "last coin" placed.
            foreach (int coin in coins)
            {
                // Only consider this coin if it's small enough to fit, and if the
                // remaining amount (a - coin) is actually reachable.
                if (coin <= a && dp[a - coin] != int.MaxValue)
                {
                    // Using this coin costs 1 + however many coins it took to make
                    // the remainder (a - coin). Keep the cheapest option found so far.
                    dp[a] = Math.Min(dp[a], dp[a - coin] + 1);
                }
            }
        }

        // If dp[amount] was never updated, the amount can't be made with these coins.
        return dp[amount] == int.MaxValue ? -1 : dp[amount];
    }

    private static void Stage4_Hard()
    {
        Console.WriteLine("=== Stage 4: Hard ===");
        Print(MinCoins(new[] { 1, 2, 5 }, 11), 3);   // 5+5+1
        Print(MinCoins(new[] { 2 }, 3), -1);
        Print(MinCoins(new[] { 1 }, 0), 0);
    }

    // ---------------------------------------------------------------
    // Helper: print actual vs expected result
    // ---------------------------------------------------------------
    private static void Print(int actual, int expected)
    {
        string status = actual == expected ? "PASS" : "FAIL";
        Console.WriteLine($"[{status}] actual={actual}, expected={expected}");
    }
}

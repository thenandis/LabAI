using System;
using System.Collections.Generic;
using System.Linq;

public class Transaction
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? AccountType { get; set; }
}

public class TransactionProcessor
{
    public async Task<decimal> GetCheckingBalanceAsync(int accountId)
    {
        // Simulate an asynchronous operation to fetch the checking balance
        await Task.Delay(200); // Simulating delay
        Console.WriteLine($"Checking Timestamp: {DateTime.Now}, Fetching checking balance for account {accountId}");
        return 500.00m; // Returning a dummy balance
    }

    public async Task<decimal> GetSavingsBalanceAsync(int accountId)
    {
        // Simulate an asynchronous operation to fetch the savings balance
        await Task.Delay(100); // Simulating delay
        Console.WriteLine($"Savings Timestamp: {DateTime.Now}, Fetching savings balance for account {accountId}");
        return 1000.00m; // Returning a dummy balance
    }

    public async Task<decimal> GetTotalBalanceAsync(int accountId)
    {
        // Fetch both balances concurrently
        var checkingTask = GetCheckingBalanceAsync(accountId);
        var savingsTask = GetSavingsBalanceAsync(accountId);

        await Task.WhenAll(checkingTask, savingsTask);

        // Sum the results
        return checkingTask.Result + savingsTask.Result;
    }
        
    public static void Main(string[] args)
    {
        var processor = new TransactionProcessor();

        processor.GetTotalBalanceAsync(123).ContinueWith(task =>
        {
            Console.WriteLine($"Total Balance: {task.Result}");
        }).Wait();

        List<Transaction> transactions = new List<Transaction>
        {
            new Transaction { TransactionId = "T1", Amount = 100.50m, AccountType = "Savings" },
            new Transaction { TransactionId = "T2", Amount = 200.75m, AccountType = "Checking" },
            new Transaction { TransactionId = "T3", Amount = 150.00m, AccountType = "Savings" },
            new Transaction { TransactionId = "T4", Amount = 300.25m, AccountType = "Checking" }
        };

        // Group, Filter (HAVING count >= 2), and Aggregate (SUM)
        var result = transactions
            .GroupBy(t => t.AccountType)
            .Select(g => new { 
                AccountType = g.Key, 
                TotalAmount1 = g.Sum(t => t.Amount),
                TransactionCount1 = g.Count(),
                MoreInfo = g.Select(t => t.AccountType).ToList()
                })
            .Where(x => x.TransactionCount1 >= 1); // Filter groups with at least 1 transaction;
            
        foreach (var item in result)
        {
            Console.WriteLine($"Account Type: {item.AccountType}, " +
                              $"Total Amount: {item.TotalAmount1}, " +
                              $"Transaction Count: {item.TransactionCount1}, " +
                              $"More Info: {string.Join(", ", item.MoreInfo)}");
        }


    }
    
}


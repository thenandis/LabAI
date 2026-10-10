using System;
using System.Collections.Generic;

// A real-world banking system would normally store transactions in a database,
// but this class demonstrates the core logic used to keep balances valid and auditable.
public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferIn,
    TransferOut
}

// Transaction represents one financial event for a bank account.
// It is immutable after creation, which helps prevent accidental tampering with the ledger.
public sealed class Transaction
{
    public Guid Id { get; }
    public string AccountNumber { get; }
    public decimal Amount { get; }
    public TransactionType Type { get; }
    public DateTime TimestampUtc { get; }
    public string Description { get; }
    public string? Reference { get; }

    public Transaction(
        string accountNumber,
        decimal amount,
        TransactionType type,
        string description,
        DateTime timestampUtc,
        string? reference = null)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));

        if (amount == 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Transaction amount cannot be zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        Amount = amount;
        Type = type;
        TimestampUtc = timestampUtc;
        Description = description;
        Reference = reference;
    }
}

// BankAccount models a single customer account.
// The balance is only changed through controlled methods to preserve consistency.
public sealed class BankAccount
{
    // Every account has its own lock so that account-level operations are thread-safe.
    private readonly object _syncRoot = new();

    // This keeps the ledger of all money movements for the account.
    private readonly List<Transaction> _transactions = new();

    public string AccountNumber { get; }
    public string CustomerName { get; }
    public decimal Balance { get; private set; }

    // Exposes the transaction history without allowing callers to change it directly.
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    public BankAccount(string accountNumber, string customerName, decimal openingBalance = 0m)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));

        if (openingBalance < 0m)
            throw new ArgumentOutOfRangeException(nameof(openingBalance), "Opening balance cannot be negative.");

        AccountNumber = accountNumber;
        CustomerName = customerName;
        Balance = openingBalance;
    }

    // Deposit adds money to the account.
    // It validates the amount before it updates the balance.
    public void Deposit(decimal amount, string description = "Deposit", string? reference = null)
    {
        if (amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be greater than zero.");

        lock (_syncRoot)
        {
            Balance += amount;
            _transactions.Add(new Transaction(
                AccountNumber,
                amount,
                TransactionType.Deposit,
                description,
                DateTime.UtcNow,
                reference));
        }
    }

    // Withdraw removes money from the account.
    // It protects against negative values and overdraft.
    public void Withdraw(decimal amount, string description = "Withdrawal", string? reference = null)
    {
        if (amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be greater than zero.");

        lock (_syncRoot)
        {
            if (amount > Balance)
                throw new InvalidOperationException("Insufficient funds for this withdrawal.");

            Balance -= amount;
            _transactions.Add(new Transaction(
                AccountNumber,
                -amount,
                TransactionType.Withdrawal,
                description,
                DateTime.UtcNow,
                reference));
        }
    }

    // Transfer moves money from one account to another.
    // It uses a consistent lock order to avoid deadlocks when two transfers happen simultaneously.
    public static void Transfer(BankAccount from, BankAccount to, decimal amount, string description = "Bank transfer")
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (ReferenceEquals(from, to))
            throw new InvalidOperationException("A transfer cannot be made to the same account.");

        if (amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Transfer amount must be greater than zero.");

        // Always lock in the same order based on account number to avoid deadlock.
        var first = string.CompareOrdinal(from.AccountNumber, to.AccountNumber) <= 0 ? from : to;
        var second = ReferenceEquals(first, from) ? to : from;

        lock (first._syncRoot)
        {
            lock (second._syncRoot)
            {
                if (amount > from.Balance)
                    throw new InvalidOperationException($"Account {from.AccountNumber} does not have enough funds.");

                from.Balance -= amount;
                to.Balance += amount;

                from._transactions.Add(new Transaction(
                    from.AccountNumber,
                    -amount,
                    TransactionType.TransferOut,
                    description,
                    DateTime.UtcNow,
                    to.AccountNumber));

                to._transactions.Add(new Transaction(
                    to.AccountNumber,
                    amount,
                    TransactionType.TransferIn,
                    description,
                    DateTime.UtcNow,
                    from.AccountNumber));
            }
        }
    }
}

// Example usage:
public static class BankingDemo
{
    public static void Main()
    {
        var checking = new BankAccount("1001", "Alice Johnson", 2500m);
        var savings = new BankAccount("2002", "Alice Johnson", 5000m);

        checking.Deposit(500m, "Payroll deposit", "PAY-001");
        checking.Withdraw(150m, "Groceries", "INV-004");

        BankAccount.Transfer(checking, savings, 300m, "Monthly transfer");

        Console.WriteLine($"Checking balance: {checking.Balance:C}");
        Console.WriteLine($"Savings balance: {savings.Balance:C}");

        Console.WriteLine("Transactions:");
        foreach (var tx in checking.Transactions)
        {
            Console.WriteLine($"{tx.TimestampUtc:O} | {tx.Type} | {tx.Amount:C} | {tx.Description}");
        }
    }
}
namespace BankingApi.Models;

public class Account
{
    public int Id { get; private set; }
    public string Owner { get; private set; }
    public decimal Balance { get; private set; }

    // Used by EF Core when materializing entities.
    private Account()
    {
        Owner = string.Empty;
    }

    public Account(string owner, decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner is required.", nameof(owner));
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(initialBalance));

        Owner = owner;
        Balance = initialBalance;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive.");
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive.");
        if (amount > Balance)
            throw new InsufficientFundsException(Balance, amount);
        Balance -= amount;
    }
}

public class InsufficientFundsException(decimal balance, decimal requested)
    : Exception($"Insufficient funds: balance {balance}, requested {requested}.");

namespace BankingApi.Models;

public record CreateAccountRequest(string Owner, decimal InitialBalance = 0);

public record AmountRequest(decimal Amount);

public record AccountResponse(int Id, string Owner, decimal Balance);

public record BalanceResponse(int AccountId, decimal Balance);

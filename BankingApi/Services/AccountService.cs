using BankingApi.Data;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface IAccountService
{
    Task<Account> CreateAsync(string owner, decimal initialBalance);
    Task<List<Account>> ListAsync();
    Task<Account?> GetAsync(int id);
    Task<Account?> DepositAsync(int id, decimal amount);
    Task<Account?> WithdrawAsync(int id, decimal amount);
}

public class AccountService(BankingDbContext db) : IAccountService
{
    public async Task<Account> CreateAsync(string owner, decimal initialBalance)
    {
        var account = new Account(owner, initialBalance);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    public Task<List<Account>> ListAsync() => db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync();

    public Task<Account?> GetAsync(int id) => db.Accounts.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Account?> DepositAsync(int id, decimal amount)
    {
        var account = await GetAsync(id);
        if (account is null) return null;
        account.Deposit(amount);
        await db.SaveChangesAsync();
        return account;
    }

    public async Task<Account?> WithdrawAsync(int id, decimal amount)
    {
        var account = await GetAsync(id);
        if (account is null) return null;
        account.Withdraw(amount);
        await db.SaveChangesAsync();
        return account;
    }
}

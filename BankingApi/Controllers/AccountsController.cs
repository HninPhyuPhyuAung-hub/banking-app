using BankingApi.Models;
using BankingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(IAccountService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> List() =>
        (await service.ListAsync()).Select(a => new AccountResponse(a.Id, a.Owner, a.Balance)).ToList();

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request)
    {
        try
        {
            var a = await service.CreateAsync(request.Owner, request.InitialBalance);
            return CreatedAtAction(nameof(GetBalance), new { id = a.Id }, new AccountResponse(a.Id, a.Owner, a.Balance));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:int}/balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(int id)
    {
        var a = await service.GetAsync(id);
        return a is null ? NotFound() : new BalanceResponse(a.Id, a.Balance);
    }

    [HttpPost("{id:int}/deposit")]
    public async Task<ActionResult<BalanceResponse>> Deposit(int id, AmountRequest request)
    {
        try
        {
            var a = await service.DepositAsync(id, request.Amount);
            return a is null ? NotFound() : new BalanceResponse(a.Id, a.Balance);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<BalanceResponse>> Withdraw(int id, AmountRequest request)
    {
        try
        {
            var a = await service.WithdrawAsync(id, request.Amount);
            return a is null ? NotFound() : new BalanceResponse(a.Id, a.Balance);
        }
        catch (InsufficientFundsException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

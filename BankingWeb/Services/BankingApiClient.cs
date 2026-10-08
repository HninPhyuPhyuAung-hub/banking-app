using System.Net;
using System.Net.Http.Json;

namespace BankingWeb.Services;

public record AmountRequest(decimal Amount);
public record AccountResponse(int Id, string Owner, decimal Balance);
public record BalanceResponse(int AccountId, decimal Balance);

public record ApiResult<T>(T? Value, string? Error)
{
    public bool Ok => Error is null;
}

public class BankingApiClient(HttpClient http)
{
    public Task<ApiResult<List<AccountResponse>>> ListAccountsAsync() =>
        SendAsync<List<AccountResponse>>(() => http.GetAsync("api/accounts"));

    public Task<ApiResult<BalanceResponse>> GetBalanceAsync(int id) =>
        SendAsync<BalanceResponse>(() => http.GetAsync($"api/accounts/{id}/balance"));

    public Task<ApiResult<BalanceResponse>> DepositAsync(int id, decimal amount) =>
        SendAsync<BalanceResponse>(() => http.PostAsJsonAsync($"api/accounts/{id}/deposit", new AmountRequest(amount)));

    public Task<ApiResult<BalanceResponse>> WithdrawAsync(int id, decimal amount) =>
        SendAsync<BalanceResponse>(() => http.PostAsJsonAsync($"api/accounts/{id}/withdraw", new AmountRequest(amount)));

    private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> call)
    {
        try
        {
            using var response = await call();
            if (response.IsSuccessStatusCode)
                return new ApiResult<T>(await response.Content.ReadFromJsonAsync<T>(), null);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApiResult<T>(default, "Account not found.");

            var message = (await response.Content.ReadAsStringAsync()).Trim('"');
            return new ApiResult<T>(default, string.IsNullOrWhiteSpace(message) ? $"Request failed ({(int)response.StatusCode})." : message);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(default, "Cannot reach the Banking API. Make sure it is running.");
        }
    }
}

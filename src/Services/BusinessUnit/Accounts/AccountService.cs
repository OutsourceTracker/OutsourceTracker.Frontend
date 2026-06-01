using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OutsourceTracker.BusinessUnit.Accounts;
using System.Net.Http.Json;

namespace OutsourceTracker.Services.BusinessUnit.Accounts;

public class AccountService : IAccountService
{
    private HttpClient Http { get; }
    private IMemoryCache Cache { get; }
    private ILogger Logger { get; }
    private const string CacheKey = "AllAccounts";

    public AccountService(IHttpClientFactory http, IMemoryCache cache, ILogger<AccountService> logger)
    {
        Http = http.CreateClient("API_Secured");
        Cache = cache;
        Logger = logger;
    }

    public async Task<List<OrganizationalAccount>> GetAllAsync()
    {
        if (Cache.TryGetValue(CacheKey, out List<OrganizationalAccount>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var accounts = await Http.GetFromJsonAsync<List<OrganizationalAccount>>("Account");
            if (accounts != null)
            {
                Cache.Set(CacheKey, accounts, TimeSpan.FromMinutes(10));
                return accounts;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load accounts from API.");
        }

        return new List<OrganizationalAccount>();
    }

    public async Task<OrganizationalAccount?> GetByIdAsync(Guid id)
    {
        try
        {
            return await Http.GetFromJsonAsync<OrganizationalAccount>($"Account/{id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get account {AccountId}", id);
            return null;
        }
    }

    public async Task<OrganizationalAccount?> CreateAsync(AccountCreateModel model)
    {
        try
        {
            using var response = await Http.PostAsJsonAsync("Account", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                // Backend currently returns 201 with no body on create for accounts.
                // Fetch the full list or rely on caller to refresh.
                return null; // Caller should refresh list
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Create account failed: {Status} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create account");
        }
        return null;
    }

    public async Task<OrganizationalAccount?> UpdateAsync(Guid id, AccountCreateModel model)
    {
        try
        {
            using var response = await Http.PutAsJsonAsync($"Account/{id}", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return await response.Content.ReadFromJsonAsync<OrganizationalAccount>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Update account failed: {Status} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update account {AccountId}", id);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            using var response = await Http.DeleteAsync($"Account/{id}");
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete account {AccountId}", id);
        }
        return false;
    }

    private void InvalidateCache()
    {
        Cache.Remove(CacheKey);
    }
}

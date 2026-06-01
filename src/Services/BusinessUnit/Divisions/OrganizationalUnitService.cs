using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OutsourceTracker.BusinessUnit.Divisions;
using System.Net.Http.Json;

namespace OutsourceTracker.Services.BusinessUnit.Divisions;

public class OrganizationalUnitService : IOrganizationalUnitService
{
    private HttpClient Http { get; }
    private IMemoryCache Cache { get; }
    private ILogger Logger { get; }
    private const string CacheKey = "AllOrganizationalUnits";

    public OrganizationalUnitService(IHttpClientFactory http, IMemoryCache cache, ILogger<OrganizationalUnitService> logger)
    {
        Http = http.CreateClient("API_Secured");
        Cache = cache;
        Logger = logger;
    }

    public async Task<List<OrganizationalUnit>> GetAllAsync()
    {
        if (Cache.TryGetValue(CacheKey, out List<OrganizationalUnit>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var units = await Http.GetFromJsonAsync<List<OrganizationalUnit>>("ou");
            if (units != null)
            {
                Cache.Set(CacheKey, units, TimeSpan.FromMinutes(10));
                return units;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load organizational units from API.");
        }

        return new List<OrganizationalUnit>();
    }

    public async Task<OrganizationalUnit?> GetByIdAsync(Guid id)
    {
        try
        {
            return await Http.GetFromJsonAsync<OrganizationalUnit>($"ou/{id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get organizational unit {OUId}", id);
            return null;
        }
    }

    public async Task<OrganizationalUnit?> CreateAsync(OrganizationalUnitCreateModel model)
    {
        try
        {
            using var response = await Http.PostAsJsonAsync("ou", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return null; // Backend returns 201 with no body
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Create OU failed: {Status} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create organizational unit");
        }
        return null;
    }

    public async Task<OrganizationalUnit?> UpdateAsync(Guid id, OrganizationalUnitCreateModel model)
    {
        try
        {
            using var response = await Http.PutAsJsonAsync($"ou/{id}", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return await response.Content.ReadFromJsonAsync<OrganizationalUnit>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Update OU failed: {Status} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update organizational unit {OUId}", id);
        }
        return null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            using var response = await Http.DeleteAsync($"ou/{id}");
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete organizational unit {OUId}", id);
        }
        return false;
    }

    public async Task<int> RecalculateAccountCountsAsync()
    {
        try
        {
            using var response = await Http.PostAsync("ou/recalculate-counts", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RecalculateCountsResult>();
                Logger.LogInformation("Recalculated OU account counts. Updated: {Updated}", result?.Updated ?? 0);
                return result?.Updated ?? 0;
            }
            else
            {
                Logger.LogWarning("Recalculate counts failed with status {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to recalculate organizational unit account counts");
        }
        return 0;
    }

    private record RecalculateCountsResult(int Updated, string Message);

    private void InvalidateCache()
    {
        Cache.Remove(CacheKey);
    }
}

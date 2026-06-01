using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OutsourceTracker.BusinessUnit.Zones;
using System.Net.Http.Json;

namespace OutsourceTracker.Services.Zones;

public class ZoneService : IZoneService
{
    private HttpClient Http { get; }
    private IMemoryCache Cache { get; }
    private ILogger Logger { get; }
    private const string CacheKey = "AllZones";

    public ZoneService(IHttpClientFactory http, IMemoryCache cache, ILogger<ZoneService> logger)
    {
        Http = http.CreateClient("API_Secured");
        Cache = cache;
        Logger = logger;
    }

    public async Task<List<Zone>> GetAllAsync()
    {
        if (Cache.TryGetValue(CacheKey, out List<Zone>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var zones = await Http.GetFromJsonAsync<List<Zone>>("Zone");
            if (zones != null)
            {
                Cache.Set(CacheKey, zones, TimeSpan.FromMinutes(10));
                return zones;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load zones from API.");
        }

        return new List<Zone>();
    }

    public async Task<Zone?> GetByIdAsync(Guid id)
    {
        try
        {
            return await Http.GetFromJsonAsync<Zone>($"Zone/{id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get zone {ZoneId}", id);
            return null;
        }
    }

    public async Task<Zone?> CreateAsync(ZoneCreateModel model)
    {
        try
        {
            using var response = await Http.PostAsJsonAsync("Zone", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                // Backend returns 201 with no body (consistent with Account/OU)
                return null;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Create zone failed: {Status} - {Error}", response.StatusCode, error);
                throw new Exception($"Create zone failed ({response.StatusCode}): {error}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to create zone");
            if (ex.Message.StartsWith("Create zone failed"))
                throw; // rethrow our formatted error so dialog shows it
            throw new Exception("Failed to create zone: " + ex.Message);
        }
    }

    public async Task<Zone?> UpdateAsync(Guid id, ZoneCreateModel model)
    {
        try
        {
            using var response = await Http.PutAsJsonAsync($"Zone/{id}", model);
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return await response.Content.ReadFromJsonAsync<Zone>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Update zone failed: {Status} - {Error}", response.StatusCode, error);
                throw new Exception($"Update zone failed ({response.StatusCode}): {error}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to update zone {ZoneId}", id);
            if (ex.Message.StartsWith("Update zone failed"))
                throw;
            throw new Exception($"Failed to update zone {id}: " + ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            using var response = await Http.DeleteAsync($"Zone/{id}");
            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Delete zone failed: {Status} - {Error}", response.StatusCode, error);
                throw new Exception($"Delete zone failed ({response.StatusCode}): {error}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Failed to delete zone {ZoneId}", id);
            if (ex.Message.StartsWith("Delete zone failed"))
                throw;
            throw new Exception($"Failed to delete zone {id}: " + ex.Message);
        }
    }

    public async Task<bool> SetBoundaryAsync(string shortCode, ZoneBoundaryRequest request)
    {
        if (string.IsNullOrWhiteSpace(shortCode) || request == null)
            return false;

        try
        {
            var normalized = shortCode.ToUpperInvariant().Trim();
            using var response = await Http.PostAsJsonAsync($"Zone/{normalized}", request);

            if (response.IsSuccessStatusCode)
            {
                InvalidateCache();
                Logger.LogInformation("Zone boundary updated for {ShortCode}", normalized);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Set boundary failed for {ShortCode}: {Status} - {Error}", normalized, response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set boundary for zone {ShortCode}", shortCode);
            return false;
        }
    }

    private void InvalidateCache()
    {
        Cache.Remove(CacheKey);
    }
}

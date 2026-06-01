using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;

namespace OutsourceTracker.Services.Equipment.Trailers;

public class TrailerService : IEquipmentService<TrailerModel>
{
    private HttpClient Http { get; }

    private ISnackbar Toast { get; }

    private IMemoryCache Cache { get; }

    private ILogger Logger { get; }

    private TimeSpan CacheDuration { get; }

    public TrailerService(IHttpClientFactory http, ISnackbar snacks, IMemoryCache cache, ILogger<TrailerService> logger)
    {
        Http = http.CreateClient("API_Secured");
        Toast = snacks;
        Cache = cache;
        Logger = logger;
        CacheDuration = TimeSpan.FromMinutes(5);
    }


    public async Task<TrailerModel?> GetAsync(Guid id, bool ignoreCache = false)
    {
        if (!ignoreCache)
        {
            if (Cache.TryGetValue(GetTrailerCacheKey(id), out TrailerModel? cachedTrailer) && cachedTrailer != null)
            {
                Logger.LogDebug("Trailer with ID {TrailerId} retrieved from cache.", id);
                return cachedTrailer;
            }
        }

        using HttpRequestMessage request = new(HttpMethod.Get, $"trailers/{id}");
        using HttpResponseMessage response = await Http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            TrailerModel? trailer = await response.Content.ReadFromJsonAsync<TrailerModel>();
            if (trailer != null)
            {
                SetOrUpdateCache(trailer);
                Logger.LogDebug("Trailer with ID {TrailerId} retrieved from API and cached.", id);
                return trailer;
            }
            else
            {
                Logger.LogWarning("Trailer with ID {TrailerId} not found in API response. | Status Code: {CODE}", id, response.StatusCode);
                throw new Exception($"Trailer with ID {id} not found.");
            }

        }
        else
        {
            throw new Exception($"Failed to retrieve trailer with ID {id}. Status Code: {response.StatusCode}");

        }
    }

    public async Task<TrailerModel?> CreateAsync(TrailerCreateRequest model)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "trailers")
        {
            Content = JsonContent.Create(model)
        };

        using HttpResponseMessage response = await Http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            TrailerModel? createdTrailer = await response.Content.ReadFromJsonAsync<TrailerModel>();
            if (createdTrailer != null)
            {
                SetOrUpdateCache(createdTrailer);
                Logger.LogInformation("Trailer with ID {TrailerId} created and cached successfully.", createdTrailer.Id);
                return createdTrailer;
            }
            else
            {
                Logger.LogWarning("Failed to parse created trailer from API response. Status Code: {StatusCode}", response.StatusCode);
                throw new Exception("Failed to parse created trailer from API response.");
            }
        }
        else
        {
            Logger.LogError("Failed to create trailer. Status Code: {StatusCode}", response.StatusCode);
            throw new Exception($"Failed to create trailer. Status Code: {response.StatusCode}");
        }
    }

    public async Task<BulkCreateResult<TrailerModel>?> CreateManyAsync(IEnumerable<TrailerCreateRequest> models)
    {
        var list = models?.ToList() ?? [];
        if (list.Count == 0)
        {
            return new BulkCreateResult<TrailerModel>();
        }

        using HttpRequestMessage request = new(HttpMethod.Post, "trailers/bulk")
        {
            Content = JsonContent.Create(list)
        };

        using HttpResponseMessage response = await Http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BulkCreateResult<TrailerModel>>();
            if (result != null)
            {
                // Cache the successfully created ones
                foreach (var created in result.Created)
                {
                    SetOrUpdateCache(created);
                }
                Logger.LogInformation("Bulk create completed. Created: {Created}, Failed: {Failed}", result.Created.Count, result.Failed.Count);
                return result;
            }
        }

        Logger.LogError("Bulk trailer creation failed. Status Code: {StatusCode}", response.StatusCode);
        // Still try to read the body in case the server returned a result with errors even on non-2xx
        try
        {
            var result = await response.Content.ReadFromJsonAsync<BulkCreateResult<TrailerModel>>();
            return result;
        }
        catch
        {
            throw new Exception($"Bulk trailer creation failed. Status Code: {response.StatusCode}");
        }
    }

    public async Task<BulkDeleteResult?> DeleteManyAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Distinct().ToList() ?? [];
        if (idList.Count == 0)
        {
            return new BulkDeleteResult();
        }

        // Remove from cache proactively for the ones we attempt
        foreach (var id in idList)
        {
            Cache.Remove(GetTrailerCacheKey(id));
        }

        using HttpRequestMessage request = new(HttpMethod.Post, "trailers/bulk-delete")
        {
            Content = JsonContent.Create(idList)
        };

        using HttpResponseMessage response = await Http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BulkDeleteResult>();
            Logger.LogInformation("Bulk delete completed. Deleted: {Deleted}, Failed: {Failed}",
                result?.SuccessfulIds?.Length ?? 0, result?.Failed?.Count ?? 0);
            return result;
        }

        Logger.LogError("Bulk trailer delete failed. Status Code: {StatusCode}", response.StatusCode);
        try
        {
            return await response.Content.ReadFromJsonAsync<BulkDeleteResult>();
        }
        catch
        {
            throw new Exception($"Bulk delete failed. Status Code: {response.StatusCode}");
        }
    }

    public async Task<BulkUpdateResult<TrailerModel>?> UpdateManyAsync(IEnumerable<Guid> ids, IDictionary<string, object> changes)
    {
        var idList = ids?.Distinct().ToList() ?? [];
        if (idList.Count == 0 || changes == null || changes.Count == 0)
        {
            return new BulkUpdateResult<TrailerModel>();
        }

        var payload = new
        {
            Ids = idList.ToArray(),
            Changes = changes
        };

        using HttpRequestMessage request = new(HttpMethod.Post, "trailers/bulk-update")
        {
            Content = JsonContent.Create(payload)
        };

        using HttpResponseMessage response = await Http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BulkUpdateResult<TrailerModel>>();
            if (result != null)
            {
                foreach (var updated in result.Updated)
                {
                    SetOrUpdateCache(updated);
                }
                Logger.LogInformation("Bulk update completed. Updated: {Updated}, Failed: {Failed}",
                    result.Updated.Count, result.Failed.Count);
                return result;
            }
        }

        Logger.LogError("Bulk trailer update failed. Status Code: {StatusCode}", response.StatusCode);
        try
        {
            return await response.Content.ReadFromJsonAsync<BulkUpdateResult<TrailerModel>>();
        }
        catch
        {
            throw new Exception($"Bulk update failed. Status Code: {response.StatusCode}");
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Cache.Remove(GetTrailerCacheKey(id));
        using HttpRequestMessage request = new(HttpMethod.Delete, $"trailers/{id}");
        using HttpResponseMessage response = await Http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Logger.LogInformation("Trailer with ID {TrailerId} deleted successfully.", id);
            return true;

        }
        else
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Logger.LogWarning("Trailer with ID {TrailerId} not found for deletion. Status Code: {StatusCode}", id, response.StatusCode);
                return false;
            }

            Logger.LogError("Failed to delete trailer with ID {TrailerId}. Status Code: {StatusCode}", id, response.StatusCode);
            return false;
        }
    }

    public async Task<EquipmentLocationUpdateResponse<Guid>?> SpotAsync(EquipmentLocationUpdateRequest<Guid> model)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, "trailers/spot")
        {
            Content = JsonContent.Create(model)
        };
        using HttpResponseMessage response = await Http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            EquipmentLocationUpdateResponse<Guid>? update = await response.Content.ReadFromJsonAsync<EquipmentLocationUpdateResponse<Guid>>();

            if (update != null)
            {
                foreach (var successfulId in update.SuccessfulTrailers)
                {
                    TrailerModel? trailer = await GetAsync(successfulId, ignoreCache: true);
                }

                return update;
            }

            Logger.LogInformation("Trailer location updated successfully for IDs: {TrailerIds}", string.Join(", ", model.Ids));
            return null;
        }
        else
        {
            Logger.LogError("Failed to update trailer location for IDs: {TrailerIds}. Status Code: {StatusCode}", string.Join(", ", model.Ids), response.StatusCode);
            throw new Exception($"Failed to update trailer location. Status Code: {response.StatusCode}");
        }
    }

    public async IAsyncEnumerable<TrailerModel> List(object? searchQuery = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "trailers");
        using HttpResponseMessage response = await Http.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var trailers = response.Content.ReadFromJsonAsAsyncEnumerable<TrailerModel>();

            await foreach (var trailer in trailers)
            {
                if (trailer != null)
                {
                    SetOrUpdateCache(trailer);
                    yield return trailer;
                }
            }
        }
        else
        {
            Logger.LogError("Failed to retrieve trailers list. Status Code: {StatusCode}", response.StatusCode);
            throw new Exception($"Failed to retrieve trailers list. Status Code: {response.StatusCode}");
        }
    }

    private static string GetTrailerCacheKey(Guid id) => $"Trailer_{id}";

    private void SetOrUpdateCache(TrailerModel model)
    {
        if (model == null || model.Id == Guid.Empty)
        {
            return;
        }

        string cacheKey = GetTrailerCacheKey(model.Id);
        if (!Cache.TryGetValue(cacheKey, out TrailerModel? cache))
        {
            Cache.Set(cacheKey, model, CacheDuration);
            return;
        }

        cache.ApplyObjectToModel(model);
        Cache.Set(cacheKey, cache, CacheDuration);
    }
}

using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using OutsourceTracker.Equipment.Trailers;
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
                Cache.Set(GetTrailerCacheKey(id), trailer, CacheDuration);
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
                Cache.Set(GetTrailerCacheKey(createdTrailer.Id), createdTrailer, CacheDuration);
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

    

    public async IAsyncEnumerable<TrailerModel> ListAsync(object? searchQuery = null)
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
                    Cache.Set(GetTrailerCacheKey(trailer.Id), trailer, CacheDuration);
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
}

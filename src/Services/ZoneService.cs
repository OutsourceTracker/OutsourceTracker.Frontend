using OutsourceTracker.Models.Zones;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services;

public class ZoneService : BaseBackendService, IModelLookupService<ZoneViewModel>
{
    public ZoneService(IServiceProvider services) : base(services)
    {
    }

    public async Task<ZoneViewModel?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"zone/{id}");
        HttpResponseMessage resp = await SendMessage(message, cancellationToken);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ZoneViewModel>(cancellationToken);
    }

    public async IAsyncEnumerable<ZoneViewModel> Search(object? searchOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "zone");
        HttpResponseMessage resp = await SendMessage(message, cancellationToken);
        resp.EnsureSuccessStatusCode();
        
        var e = resp.Content.ReadFromJsonAsAsyncEnumerable<ZoneViewModel>(cancellationToken);
        await foreach (var zone in e)
        {
            if (zone != null)
            {
                yield return zone;
            }
        }
    }
}

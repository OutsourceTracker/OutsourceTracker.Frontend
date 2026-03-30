using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services;

public class TrailerService : BaseBackendService, IModelCreateService<TrailerViewModel, HttpResponseMessage>, IModelLookupService<TrailerViewModel>, IModelDeleteService<TrailerViewModel, HttpResponseMessage>, IModelUpdateService<TrailerViewModel, HttpResponseMessage>, ITrackableLocationService<TrailerViewModel, HttpResponseMessage>
{
    public TrailerService(IServiceProvider sp) : base(sp)
    {
    }

    public async Task<HttpResponseMessage> Create(TrailerViewModel? model = null, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "trailers");
        return await SendMessage(message, cancellationToken);
    }

    public async Task<TrailerViewModel?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        var message = new HttpRequestMessage(HttpMethod.Get, $"trailers/{id}");
        using var response = await SendMessage(message, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrailerViewModel>();
    }

    public async IAsyncEnumerable<TrailerViewModel> Search(object? searchOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "trailers");

        using HttpResponseMessage response = await SendMessage(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var haystack = response.Content.ReadFromJsonAsAsyncEnumerable<TrailerViewModel>(cancellationToken);
        await foreach (var item in haystack)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }

    public async Task<HttpResponseMessage> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"trailers/{id}");
        return await SendMessage(message, cancellationToken);
    }

    public async Task<HttpResponseMessage> Update(Guid id, object request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"trailers/{id}");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        return await SendMessage(message, cancellationToken);
    }

    public async Task<HttpResponseMessage> UpdateLocation(Guid id, Vector2 mapCoordinates, double? accuracy = null, CancellationToken cancellationToken = default)
    {
        string messageUri = $"/trailers/{id}/spot";

        if (accuracy.HasValue)
        {
            messageUri += $"?acc={accuracy.Value}";
        }

        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, messageUri);
        message.Content = JsonContent.Create(mapCoordinates);
        return await SendMessage(message, cancellationToken);
    }
}

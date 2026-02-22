using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services;

public class TrailerService : IModelCreateService<TrailerViewModel, HttpResponseMessage>, IModelLookupService<TrailerViewModel>, IModelDeleteService<TrailerViewModel, HttpResponseMessage>, IModelUpdateService<TrailerViewModel, HttpResponseMessage>, ITrackableLocationService<TrailerViewModel, HttpResponseMessage>
{
    protected HttpClient Client { get; }
    protected ILogger Logger { get; }

    public TrailerService(IHttpClientFactory http, ILogger<TrailerService> logger)
    {
        Client = http.CreateClient("API");
        Logger = logger;
    }

    public async Task<HttpResponseMessage> Create(CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "trailers");
        return await Client.SendAsync(message, cancellationToken);
    }

    public async Task<TrailerViewModel?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        return await Client.GetFromJsonAsync<TrailerViewModel>($"trailers/{id}", cancellationToken);
    }

    public async IAsyncEnumerable<TrailerViewModel> Search(object? searchOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "trailers");

        //if (query != null)
        //{
        //    var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        //    if (!string.IsNullOrWhiteSpace(request.Prefix))
        //    {
        //        query["prefix"] = request.Prefix;
        //    }
        //    if (!string.IsNullOrWhiteSpace(request.Name))
        //    {
        //        query["name"] = request.Name;
        //    }
        //    if (!string.IsNullOrWhiteSpace(request.SpottedBy))
        //    {
        //        query["spottedBy"] = request.SpottedBy;
        //    }
        //    string queryString = query.ToString() ?? string.Empty;
        //    message.RequestUri = new Uri($"{message.RequestUri}?{queryString}", UriKind.Relative);
        //}

        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
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
        return await Client.SendAsync(message, cancellationToken);
    }

    public async Task<HttpResponseMessage> Update(Guid id, object request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"trailers/{id}");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        return await Client.SendAsync(message, cancellationToken);
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
        return await Client.SendAsync(message);
    }
}

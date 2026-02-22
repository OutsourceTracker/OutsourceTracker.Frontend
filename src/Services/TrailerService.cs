using OutsourceTracker.Equipment;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services;

public class TrailerService : IModelCreateService<TrailerViewModel>, IModelLookupService<TrailerViewModel>, IModelDeleteService<TrailerViewModel>, IModelUpdateService<TrailerViewModel>, ITrackableLocationService<TrailerViewModel>
{
    protected HttpClient Client { get; }
    protected ILogger Logger { get; }

    public TrailerService(IHttpClientFactory http, ILogger<TrailerService> logger)
    {
        Client = http.CreateClient("API");
        Logger = logger;
    }

    public async Task<Guid?> Create(CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "trailers");
        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        CreateResponse? r = await response.Content.ReadFromJsonAsync<CreateResponse>(cancellationToken: cancellationToken);
        return r.Value.Id;
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

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"trailers/{id}");
        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<TrailerViewModel?> Update(Guid id, object request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"trailers/{id}");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrailerViewModel>(cancellationToken: cancellationToken);
    }

    public async Task UpdateLocation(Guid id, Vector2 mapCoordinates, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"/trailers/{id}/spot");
        message.Content = JsonContent.Create(mapCoordinates);
        await Client.SendAsync(message);
    }

    private struct CreateResponse
    {
        public Guid? Id { get; set; }
    }
}

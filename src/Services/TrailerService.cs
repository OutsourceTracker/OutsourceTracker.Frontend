using OutsourceTracker.Models.Trailers;
using OutsourceTracker.ModelService;
using OutsourceTracker.ModelService.Requests;
using OutsourceTracker.ModelService.Requests.Trailers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace OutsourceTracker.Services;

public class TrailerService : IModelService<Guid, TrailerViewModel, TrailerFindRequest>, IWritableModelService<Guid, TrailerViewModel, TrailerCreateRequest, TrailerUpdateRequest, DeleteRequest>
{
    protected HttpClient Client { get; }
    protected ILogger Logger { get; }

    public TrailerService(HttpClient client, ILogger<TrailerService> logger)
    {
        Client = client;
        Logger = logger;
    }

    public async Task<TrailerViewModel?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        return await Client.GetFromJsonAsync<TrailerViewModel>($"trailer/{id}", cancellationToken);
    }

    public async IAsyncEnumerable<TrailerViewModel> Find(TrailerFindRequest? request = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "trailer");

        if (request != null)
        {
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrWhiteSpace(request.Prefix))
            {
                query["prefix"] = request.Prefix;
            }
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query["name"] = request.Name;
            }
            if (!string.IsNullOrWhiteSpace(request.SpottedBy))
            {
                query["spottedBy"] = request.SpottedBy;
            }
            string queryString = query.ToString() ?? string.Empty;
            message.RequestUri = new Uri($"{message.RequestUri}?{queryString}", UriKind.Relative);
        }

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

    public async Task<Guid> Create(TrailerCreateRequest? request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "trailer");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        CreateResponse r = await response.Content.ReadFromJsonAsync<CreateResponse>(cancellationToken: cancellationToken);
        return r.Value!.Value;
    }

    public async Task<TrailerViewModel?> Update(Guid id, TrailerUpdateRequest? request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"trailer/{id}");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrailerViewModel>(cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> Delete(Guid id, DeleteRequest? request, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"trailer/{id}");

        if (request != null)
        {
            message.Content = JsonContent.Create(request);
        }

        HttpResponseMessage response = await Client.SendAsync(message, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private struct CreateResponse
    {
        public Guid? Value { get; set; }
    }
}

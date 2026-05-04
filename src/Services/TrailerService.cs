using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.DataModels;
using System.Net.Http.Json;

namespace OutsourceTracker.Services;

public class TrailerService : BaseBackendService<Guid, TrailerViewModel>
{
    protected override string EndPoint => "trailers";

    public TrailerService(IServiceProvider sp) : base(sp)
    {
    }

    public async Task<ModelResult> UpdateLocation(Guid id, Vector2 mapCoordinates, double? accuracy = null, CancellationToken cancellationToken = default)
    {
        string messageUri = $"/trailers/{id}/spot";

        if (accuracy.HasValue)
        {
            messageUri += $"?acc={accuracy.Value}";
        }

        using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, messageUri);
        message.Content = JsonContent.Create(mapCoordinates);
        using HttpResponseMessage response = await SendMessage(message, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await ForceCacheUpdateAsync(id, cancellationToken);
            return ModelResult.Builder().WithResult(result).WithSuccess().Build();
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return ModelResult.Builder().AddError("ServerError", errorContent).Build();
        }
    }
}

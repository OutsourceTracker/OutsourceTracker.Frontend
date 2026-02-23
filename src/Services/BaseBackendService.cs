using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace OutsourceTracker.Services;

public abstract class BaseBackendService
{
    private HttpClient HttpClient { get; }
    private NavigationManager Navigation { get; }

    protected BaseBackendService(IServiceProvider services)
    {
        HttpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("API");
        Navigation = services.GetRequiredService<NavigationManager>();
    }

    protected virtual async Task<HttpResponseMessage> SendMessage(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.SendAsync(message, cancellationToken);
            return response;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}

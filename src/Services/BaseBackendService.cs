using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace OutsourceTracker.Services;

public abstract class BaseBackendService
{
    private HttpClient HttpClient { get; }
    private NavigationManager Navigation { get; }

    protected ILogger Logger { get; }

    protected BaseBackendService(IServiceProvider services)
    {
        ILoggerFactory factory = services.GetRequiredService<ILoggerFactory>();
        Logger = factory.CreateLogger(GetType());
        HttpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("API");
        Navigation = services.GetRequiredService<NavigationManager>();
    }

    protected virtual async Task<HttpResponseMessage> SendMessage(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Sending {METHOD} to {URI}", message.Method, message.RequestUri);
            var response = await HttpClient.SendAsync(message, cancellationToken);
            Logger.LogDebug("Received Response: {CODE}", response.StatusCode);
            return response;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}

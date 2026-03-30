namespace OutsourceTracker.Services;

public abstract class BaseBackendService
{
    private HttpClient HttpClient { get; }

    protected ILogger Logger { get; }

    protected BaseBackendService(IServiceProvider services)
    {
        ILoggerFactory factory = services.GetRequiredService<ILoggerFactory>();
        Logger = factory.CreateLogger(GetType());
        HttpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    }

    protected virtual async Task<HttpResponseMessage> SendMessage(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Sending {METHOD} to {URI}", message.Method, message.RequestUri);
        var response = await HttpClient.SendAsync(message, cancellationToken);
        Logger.LogDebug("Received Response: {CODE}", response.StatusCode);
        return response;
    }
}

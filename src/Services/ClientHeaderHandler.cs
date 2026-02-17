using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace OutsourceTracker.Services;

public sealed class ClientHeaderHandler : AuthorizationMessageHandler
{
    private IJSRuntime JS { get; }

    private string? TimeZone { get; set; }
    private string? Language { get; set; }


    public ClientHeaderHandler(IJSRuntime runtime, IAccessTokenProvider provider, NavigationManager nav) : base(provider, nav)
    {
        JS = runtime;
        ConfigureHandler(
            authorizedUrls: new[]
            {
                "https://localhost:7253",
                "https://api.vandersluistrucking.com"
            },
            scopes: new[]
            {
                "api://51032fa1-b18c-464f-9a43-34819040dfa7/access_as_user"
            }
        );
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Accept.Any())
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        TimeZone ??= await GetTimezoneAsync();
        Language ??= await GetLanguageAsync();

        request.Headers.TryAddWithoutValidation("X-Client-Timezone", TimeZone);
        request.Headers.TryAddWithoutValidation("Accept-Language", Language);
        request.Headers.TryAddWithoutValidation("X-Client-Type", "BlazorWasm");

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetTimezoneAsync()
    {
        try
        {
            return await JS.InvokeAsync<string>("getClientTimezone") ?? "UTC";
        }
        catch
        {
            return "UTC";
        }
    }

    private async Task<string> GetLanguageAsync()
    {
        try
        {
            return await JS.InvokeAsync<string>("getClientLanguage") ?? "en-US";
        }
        catch
        {
            return "en-US";
        }
    }
}

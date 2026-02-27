using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace OutsourceTracker.Services;

public sealed class ClientHeaderHandler : AuthorizationMessageHandler
{
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly IAccessTokenProvider _tokenProvider;

    private string? _timeZone;
    private string? _language;

    public ClientHeaderHandler(
        IAccessTokenProvider tokenProvider,
        NavigationManager nav,
        IJSRuntime js)
        : base(tokenProvider, nav)
    {
        _nav = nav;
        _js = js;
        _tokenProvider = tokenProvider;

        ConfigureHandler(
            authorizedUrls: new[]
            {
                "https://localhost:7253/",
                "https://api.vandersluistrucking.com/"
            },
            scopes: new[]
            {
                "api://51032fa1-b18c-464f-9a43-34819040dfa7/access_as_user"
            });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // This calls MSAL → tries silent refresh if needed
            var tokenResult = await _tokenProvider.RequestAccessToken();

            if (tokenResult.TryGetToken(out var token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            }
            else
            {
                // Rare case — token not available but no exception thrown
                _nav.NavigateTo("/login", forceLoad: true);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        if (!request.Headers.Accept.Any())
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        _timeZone ??= await GetTimezoneAsync();
        _language ??= await GetLanguageAsync();

        request.Headers.TryAddWithoutValidation("X-Client-Timezone", _timeZone);
        request.Headers.TryAddWithoutValidation("Accept-Language", _language);
        request.Headers.TryAddWithoutValidation("X-Client-Type", "BlazorWasm");

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetTimezoneAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("getClientTimezone") ?? "UTC";
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
            return await _js.InvokeAsync<string>("getClientLanguage") ?? "en-US";
        }
        catch
        {
            return "en-US";
        }
    }
}
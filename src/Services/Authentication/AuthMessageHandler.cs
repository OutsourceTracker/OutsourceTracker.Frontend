using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace OutsourceTracker.Services.Authentication;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly TokenService _authService;
    private readonly IJSRuntime _js;
    private string? _timeZone;
    private string? _language;

    public AuthMessageHandler(AuthenticationStateProvider authService, IJSRuntime js)
    {
        _authService = authService as TokenService ?? throw new InvalidCastException();
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (!request.Headers.Accept.Any())
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        _timeZone ??= await GetTimezoneAsync();
        _language ??= await GetLanguageAsync();

        request.Headers.TryAddWithoutValidation("X-Client-Timezone", _timeZone);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(_language));
        request.Headers.TryAddWithoutValidation("X-Client-Language", _language);
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OutsourceTracker.Authentication;
using System.Net;
using System.Net.Http.Json;

namespace OutsourceTracker.Services.Authentication;

public class LoginService
{
    private const string TOKEN_KEY = "AUTH_TOKEN";
    private readonly HttpClient _http;
    private readonly NavigationManager _nav;
    private readonly ILogger _logger;
    private readonly IJSRuntime _js;
    private readonly TokenService _tokenService;

    public LoginService(IHttpClientFactory http, IJSRuntime js, NavigationManager nav, ILogger<LoginService> logger, AuthenticationStateProvider asp)
    {
        _http = http.CreateClient("API");
        _nav = nav;
        _logger = logger;
        _js = js;
        _tokenService = asp as TokenService ?? throw new ArgumentException("AuthenticationStateProvider must be of type TokenService");
    }

    public async Task<bool> LoginAsync(LoginModel model, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("authentication/login", model, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var tokenData = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

            if (tokenData?.Token == null)
                return false;

            await _tokenService.SetTokenAsync(tokenData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _tokenService.ClearTokenAsync();
        _nav.NavigateTo("/login", forceLoad: true);
    }

    public async Task<HttpStatusCode> RegisterAsync(RegisterModel model, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("authentication/register", model, cancellationToken);
            return response.StatusCode;
        }
        catch
        {
            return HttpStatusCode.InternalServerError;
        }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OutsourceTracker.Authentication;
using OutsourceTracker.Services.DataModels;
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

    public async Task<ModelResult> LoginAsync(LoginModel model, CancellationToken cancellationToken)
    {
        IModelResultBuilder r = ModelResult.Builder();
        try
        {
            var response = await _http.PostAsJsonAsync("authentication/login", model, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string? reason = await response.Content.ReadFromJsonAsync<string>(cancellationToken);
                return r.WithResult(reason!)
                    .Build();
            }

            var tokenData = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

            if (tokenData?.Token == null)
                return r.AddError("TokenError", "Failed to retrieve authentication token")
                    .Build();

            await _tokenService.SetTokenAsync(tokenData);
            return r.WithSuccess()
                .WithResult(tokenData)
                .Build();
        }
        catch
        {
            return r.AddError("LoginError", "An error occurred during login")
                .Build();
        }
    }

    public async Task LogoutAsync()
    {
        await _tokenService.ClearTokenAsync();
        _nav.NavigateTo("/login", forceLoad: true);
    }

    public async Task<ModelResult> RegisterAsync(RegisterModel model, CancellationToken cancellationToken)
    {
        IModelResultBuilder r = ModelResult.Builder();
        try
        {
            var response = await _http.PostAsJsonAsync("authentication/register", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string? reason = await response.Content.ReadFromJsonAsync<string>(cancellationToken);
                return r.WithResult(reason!)
                    .Build();
            }

            return r.WithSuccess()
                .Build();
        }
        catch (Exception ex)
        {
            return r.AddError("RegistrationError", "An error occurred during registration: " + ex.Message)
                .Build();
        }
    }
}

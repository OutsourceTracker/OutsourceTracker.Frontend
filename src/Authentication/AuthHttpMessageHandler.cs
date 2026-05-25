using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;

namespace OutsourceTracker.Authentication;

public class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly ILogger _logger;
    private readonly NavigationManager _navigationManager;

    public AuthHttpMessageHandler(ITokenService tokenService, AuthenticationStateProvider authStateProvider, ILogger<AuthHttpMessageHandler> logger, NavigationManager navigationManager)
    {
        _tokenService = tokenService;
        _authStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
        _logger = logger;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            if (_tokenService.IsTokenExpired(token))
            {
                _logger.LogWarning("Token expired. Clearing and redirecting to login.");
                await _tokenService.ClearTokenAsync();
                await _authStateProvider.NotifyStateChangedAsync();
                _navigationManager.NavigateTo("/", true);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("Attached Bearer token to request: {Uri}", request.RequestUri);
        }
        else
        {
            _logger.LogDebug("No token available for request: {Uri}", request.RequestUri);
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to {Uri}", request.RequestUri);
            throw;
        }
    }
}
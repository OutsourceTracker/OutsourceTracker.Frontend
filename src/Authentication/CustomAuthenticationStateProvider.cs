using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OutsourceTracker.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenService _tokenService;
    private readonly ILogger _logger;

    public CustomAuthenticationStateProvider(ITokenService tokenService, ILogger<CustomAuthenticationStateProvider> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        using var scope = _logger.BeginScope("Getting authentication state");

        try
        {
            var token = await _tokenService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("No token found → unauthenticated");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            if (_tokenService.IsTokenExpired(token))
            {
                _logger.LogWarning("Token expired → unauthenticated");
                await _tokenService.ClearTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var principal = await _tokenService.ValidateTokenAsync(token);

            if (principal == null)
            {
                _logger.LogWarning("Token validation failed → unauthenticated");
                await _tokenService.ClearTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            _logger.LogInformation("User authenticated: {User}", principal.Identity?.Name);
            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    // Helper methods for Login/Logout flows
    public async Task NotifyStateChangedAsync()
    {
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await _tokenService.SetTokenAsync(token);
        await NotifyStateChangedAsync();
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await _tokenService.ClearTokenAsync();
        await NotifyStateChangedAsync();
    }
}
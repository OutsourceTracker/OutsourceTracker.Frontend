using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OutsourceTracker.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    private ClaimsPrincipal? _claims;
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;
    public bool AuthPending { get; set; }

    public UserService(AuthenticationStateProvider state)
    {
        _authStateProvider = state;
    }

    /// <summary>
    /// Returns the current user's ClaimsPrincipal (cached for performance)
    /// </summary>
    public async Task<ClaimsPrincipal> GetClaimsPrincipalAsync()
    {
        if (_claims != null)
            return _claims;

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            _claims = authState.User ?? new ClaimsPrincipal(new ClaimsIdentity());
            _isAuthenticated = _claims.Identity?.IsAuthenticated ?? false;
        }
        catch
        {
            _claims = new ClaimsPrincipal(new ClaimsIdentity());
            _isAuthenticated = false;
        }

        return _claims;
    }

    /// <summary>
    /// Gets the current user's email (for Gravatar, profile, etc.)
    /// </summary>
    public async Task<string?> GetCurrentUserEmailAsync()
    {
        var user = await GetClaimsPrincipalAsync();
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value;   // fallback for custom claim name
    }

    /// <summary>
    /// Gets the current user's ID (sub claim from JWT)
    /// </summary>
    public async Task<string?> GetCurrentUserIdAsync()
    {
        var user = await GetClaimsPrincipalAsync();
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Gets the current user's display name
    /// </summary>
    public async Task<string?> GetCurrentUserNameAsync()
    {
        var user = await GetClaimsPrincipalAsync();
        return user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name;
    }

    /// <summary>
    /// Checks if the current user is in a specific role
    /// </summary>
    public async Task<bool> IsInRoleAsync(string role)
    {
        var user = await GetClaimsPrincipalAsync();
        return user.IsInRole(role);
    }

    /// <summary>
    /// Clears cached claims (call this after login/logout)
    /// </summary>
    public void ClearCache()
    {
        _claims = null;
        _isAuthenticated = false;
    }

    /// <summary>
    /// Optional: Wait for authentication to complete (useful during app startup)
    /// </summary>
    public async Task WaitForAuthenticationAsync(TimeSpan? timeout = null)
    {
        if (!AuthPending) return;

        var start = DateTime.UtcNow;
        var maxWait = timeout ?? TimeSpan.FromSeconds(5);

        while (AuthPending && (DateTime.UtcNow - start) <= maxWait)
        {
            await Task.Delay(100);
        }
    }
}

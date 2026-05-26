using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Runtime;
using System.Security.Claims;

namespace OutsourceTracker.Authentication;

public class JwtTokenService : ITokenService
{
    private IJSRuntime JS { get; }

    private ILogger Logger { get; }

    private JwtSecurityTokenHandler Handler { get; }

    private TokenValidationParameters ValidationParameters { get; }

    private const string DefaultStoreKey = "authKey";

    public JwtTokenService(IJSRuntime js, ILogger<JwtTokenService> logger)
    {
        JS = js;
        Logger = logger;
        Handler = new JwtSecurityTokenHandler();
        ValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false,
            ValidIssuer = "https://api.vandersluistrucking.com",
            ValidAudience = "https://api.vandersluistrucking.com",
            ClockSkew = TimeSpan.FromMinutes(2),
            RequireSignedTokens = false
        };
    }

    public async Task SetTokenAsync(string token, string? storeKey = null)
    {
        storeKey ??= DefaultStoreKey;
        using var scope = Logger.BeginScope("Storing token in storage with key: {StoreKey}", storeKey);
        await JS.InvokeVoidAsync("localStorage.setItem", storeKey, token);
        Logger.LogInformation("Token stored successfully.");
    }

    public async Task<string> GetTokenAsync(string? storeKey = null)
    {
        storeKey ??= DefaultStoreKey;
        using var scope = Logger.BeginScope("Retrieving token from storage with key: {StoreKey}", storeKey);
        string tokenKey = await JS.InvokeAsync<string>("localStorage.getItem", storeKey);

        if (string.IsNullOrEmpty(tokenKey))
        {
            Logger.LogError("No token found in storage with key: {StoreKey}", storeKey);
            return string.Empty;
        }

        return tokenKey;
    }

    public async Task ClearTokenAsync(string? storeKey = null)
    {
        storeKey ??= DefaultStoreKey;
        using var scope = Logger.BeginScope("Clearing token from storage with key: {StoreKey}", storeKey);
        await JS.InvokeVoidAsync("localStorage.removeItem", storeKey);
        Logger.LogInformation("Token cleared successfully.");
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var _jwt = Handler.ReadJwtToken(token);
            return _jwt.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public IEnumerable<Claim> GetClaims(string token)
    {
        using var scope = Logger.BeginScope("Extracting claims from token");
        try
        {
            var _jwt = Handler.ReadJwtToken(token);
            return _jwt.Claims;

        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read claims from token.");
            return [];
        }
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        using var scope = Logger.BeginScope("Validating token (client-side)");
        try
        {
            var jwt = Handler.ReadJwtToken(token);
            if (!string.Equals(jwt.Issuer, ValidationParameters.ValidIssuer, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Invalid issuer: {Issuer}", jwt.Issuer);
                return null;
            }

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                Logger.LogWarning("Token expired");
                await ClearTokenAsync();
                return null;
            }

            if (!jwt.Audiences.Contains(ValidationParameters.ValidAudience))
            {
                Logger.LogWarning("Invalid audience");
                return null;
            }

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            Logger.LogInformation("Token validated successfully. User: {User}", principal.Identity?.Name);
            return principal;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Client-side token validation failed");
            return null;
        }
    }
}

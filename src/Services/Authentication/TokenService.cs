using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OutsourceTracker.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OutsourceTracker.Services.Authentication;

public class TokenService : AuthenticationStateProvider
{
    private const string TOKEN_KEY = "AUTH_TOKEN";
    private readonly IJSRuntime _js;

    public TokenService(IJSRuntime js)
    {
        _js = js;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY);

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task SetTokenAsync(TokenResponse tokenData)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, tokenData.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearTokenAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    public async Task<string> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

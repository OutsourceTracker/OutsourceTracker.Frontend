using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Security.Claims;

namespace OutsourceTracker.Services;

public class UserService
{
    private AuthenticationStateProvider Provider { get; set; } = default!;
    private IAccessTokenProvider TokenService { get; set; }= default!;
    private ClaimsPrincipal? Claims { get; set; }
    private AccessToken? TokenResult { get; set; }
    public bool AuthPending { get; set; }
    public bool IsAuthenticated { get; set; }

    public UserService(AuthenticationStateProvider state, IAccessTokenProvider tokens)
    {
        Provider = state;
        TokenService = tokens;
    }

    public async Task<ClaimsPrincipal> GetClaim()
    {
        try
        {
            if (Claims == null)
            {
                var request = await Provider.GetAuthenticationStateAsync();

                if (request == null)
                {
                    return new ClaimsPrincipal(new ClaimsIdentity());
                }

                Claims = request.User;
            }
        }
        catch (Exception ex)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
        

        return Claims;
    }

    public async Task<AccessToken?> GetToken()
    {
        if (TokenResult != null)
        {
            return TokenResult;
        }

        try
        {
            var tokenResult = await TokenService.RequestAccessToken(
                new AccessTokenRequestOptions
                {
                    Scopes = new[] { "User.Read" }
                });


            if (tokenResult.TryGetToken(out var token))
            {
                TokenResult = token;
                return token;
            }
        }
        catch { }

        return null;
    }

    public Task WaitForAuthentication(TimeSpan? timeout = null) => Task.Run(async () =>
    {
        DateTime now = DateTime.Now;
        TimeSpan span = timeout ?? TimeSpan.FromSeconds(5);
        while (AuthPending && (DateTime.Now - now) <= span)
        {
            await Task.Delay(100);
        }
    });
}

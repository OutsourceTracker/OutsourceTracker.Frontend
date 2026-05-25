using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Net.Http.Json;
using System.Security.Claims;

namespace OutsourceTracker.Authentication;

public class UserService
{
    private HttpClient Http { get; }

    private ILogger Logger { get; }

    private ISnackbar Toast { get; }

    private CustomAuthenticationStateProvider Auth { get; }

    private ITokenService Token { get; }

    private NavigationManager Navigation { get; }

    public UserService(IHttpClientFactory http, ILogger<UserService> logger, ISnackbar snacks, NavigationManager navigation, AuthenticationStateProvider auth, ITokenService token)
    {
        Http = http.CreateClient("API");
        Logger = logger;
        Toast = snacks;
        Navigation = navigation;
        Token = token;
        Auth = (CustomAuthenticationStateProvider)auth;
    }

    public async Task LoginAsync(LoginModel model)
    {
        Logger.LogDebug("Attempting to log in user with email: {Email}", model.Email);
        using HttpRequestMessage requestMessage = new(HttpMethod.Post, "authentication/login");
        requestMessage.Content = JsonContent.Create(model);
        using HttpResponseMessage responseMessage = await Http.SendAsync(requestMessage);
        if (responseMessage.IsSuccessStatusCode)
        {
            string tokenResponse = await responseMessage.Content.ReadAsStringAsync();
            // TODO: Use TokenService to manage token instead of local storage directly
            if (!string.IsNullOrWhiteSpace(tokenResponse))
            {
                await Auth.MarkUserAsAuthenticatedAsync(tokenResponse);

                ClaimsPrincipal user = (await Token.ValidateTokenAsync(tokenResponse))!;

                Toast.Add($"Login successful! Welcome {user.Identity?.Name}", Severity.Success);
                Navigation.NavigateTo("/dashboard");
            }
            else
            {
                Toast.Add("Login failed: No token received.", Severity.Error);
            }
        }
        else
        {
            string error = await responseMessage.Content.ReadAsStringAsync();
            Toast.Add($"Login failed: {error}", Severity.Error);
        }
    }

    public async Task RegisterUserAsync(RegisterModel model)
    {
        Logger.LogDebug("Attempting to register user with email: {Email}", model.Email);
        using HttpRequestMessage requestMessage = new(HttpMethod.Post, "authentication/register");
        requestMessage.Content = JsonContent.Create(model);
        using HttpResponseMessage responseMessage = await Http.SendAsync(requestMessage);

        if (responseMessage.IsSuccessStatusCode)
        {
            Toast.Add("Verification email has been sent! Please check your inbox before logging in.", Severity.Success);
            Navigation.NavigateTo("/");
        }
        else
        {
            Dictionary<string, string[]> errors = await responseMessage.Content.ReadFromJsonAsync<Dictionary<string, string[]>>() ?? new();

            foreach (var error in errors)
            {
                if (error.Value == null)
                {
                    continue;
                }

                if (error.Value.Length > 1)
                {
                    string e = "<div>" +
                        $"<h3>{error.Key}</h3>" +
                        $"<ul>";
                    foreach (var v in error.Value)
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            continue;
                        }

                        e += $"<li>{v}</li>";
                    }
                    e += "</ul>" +
                        "</div>";

                    Toast.Add(e, Severity.Error);
                }
                else
                {
                    Toast.Add($"{error.Key}: {error.Value[0]}", Severity.Error);
                }
                
            }
        }
    }

    public async Task ValidateLogin()
    {
        string? token = await Token.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token) && !Token.IsTokenExpired(token))
        {
            Toast.Add("User is already logged in. Redirecting to Dashboard.", Severity.Success);
            Navigation.NavigateTo("/dashboard");
        }
    }

    public async Task LogoutAsync()
    {
        await Auth.MarkUserAsLoggedOutAsync();
        Toast.Add("You have been logged out.", Severity.Info);
        Navigation.NavigateTo("/");
    }
}
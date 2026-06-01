using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Net.Http.Json;
using System.Security.Claims;

namespace OutsourceTracker.Authentication;

public class UserService
{
    private HttpClient Http { get; }
    private HttpClient SecuredHttp { get; }

    private ILogger Logger { get; }

    private ISnackbar Toast { get; }

    private CustomAuthenticationStateProvider Auth { get; }

    private ITokenService Token { get; }

    private NavigationManager Navigation { get; }

    public UserService(IHttpClientFactory http, ILogger<UserService> logger, ISnackbar snacks, NavigationManager navigation, AuthenticationStateProvider auth, ITokenService token)
    {
        Http = http.CreateClient("API");
        SecuredHttp = http.CreateClient("API_Secured");
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

    public async Task<UserProfileModel?> GetProfileAsync()
    {
        try
        {
            var profile = await SecuredHttp.GetFromJsonAsync<UserProfileModel>("authentication/profile");
            return profile;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load user profile");
            Toast.Add("Failed to load profile", Severity.Error);
            return null;
        }
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest model)
    {
        try
        {
            var response = await SecuredHttp.PutAsJsonAsync("authentication/profile", model);
            if (response.IsSuccessStatusCode)
            {
                Toast.Add("Profile updated successfully", Severity.Success);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Toast.Add($"Failed to update profile: {error}", Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update user profile");
            Toast.Add("Failed to update profile", Severity.Error);
            return false;
        }
    }

    // ==================== PASSKEY SUPPORT ====================

    public async Task<string?> GetPasskeyRegistrationOptionsAsync(string? displayName = null)
    {
        try
        {
            var url = "authentication/passkey/registration-options";
            if (!string.IsNullOrWhiteSpace(displayName))
                url += $"?displayName={Uri.EscapeDataString(displayName)}";

            return await SecuredHttp.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get passkey registration options");
            Toast.Add("Failed to prepare passkey registration", Severity.Error);
            return null;
        }
    }

    public async Task<bool> CompletePasskeyRegistrationAsync(object credentialResponse)
    {
        try
        {
            var response = await SecuredHttp.PostAsJsonAsync("authentication/passkey/complete-registration", credentialResponse);
            if (response.IsSuccessStatusCode)
            {
                Toast.Add("Passkey registered successfully!", Severity.Success);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Toast.Add($"Failed to register passkey: {error}", Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Passkey registration completion failed");
            Toast.Add("Passkey registration failed", Severity.Error);
            return false;
        }
    }

    public async Task<string?> GetPasskeyAssertionOptionsAsync()
    {
        try
        {
            return await Http.GetStringAsync("authentication/passkey/assertion-options");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get passkey assertion options");
            Toast.Add("Failed to prepare passkey login", Severity.Error);
            return null;
        }
    }

    public async Task<bool> CompletePasskeyAssertionAsync(object assertionResponse, bool rememberMe = false)
    {
        try
        {
            // Forward the full response object exactly as returned by the browser (via passkey.js).
            // It already has the correct shape (id/rawId/type + nested response + clientExtensionResults)
            // that AuthenticatorAssertionRawResponse expects. Using query string for rememberMe
            // to match the backend controller signature.
            var url = $"authentication/passkey/complete-assertion?rememberMe={rememberMe.ToString().ToLowerInvariant()}";
            var response = await Http.PostAsJsonAsync(url, assertionResponse);

            if (response.IsSuccessStatusCode)
            {
                string tokenResponse = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(tokenResponse))
                {
                    await Auth.MarkUserAsAuthenticatedAsync(tokenResponse);
                    var user = (await Token.ValidateTokenAsync(tokenResponse))!;
                    Toast.Add($"Welcome back, {user.Identity?.Name} (via passkey)", Severity.Success);
                    Navigation.NavigateTo("/dashboard");
                    return true;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Toast.Add($"Passkey login failed: {error}", Severity.Error);
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Passkey assertion completion failed");
            Toast.Add("Passkey login failed", Severity.Error);
            return false;
        }
    }

    public async Task<List<PasskeyInfo>?> GetUserPasskeysAsync()
    {
        try
        {
            return await SecuredHttp.GetFromJsonAsync<List<PasskeyInfo>>("authentication/passkeys");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load passkeys");
            return new List<PasskeyInfo>();
        }
    }

    public async Task<bool> DeletePasskeyAsync(string credentialId)
    {
        try
        {
            var response = await SecuredHttp.DeleteAsync($"authentication/passkeys/{credentialId}");
            if (response.IsSuccessStatusCode)
            {
                Toast.Add("Passkey removed", Severity.Success);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Toast.Add($"Failed to remove passkey: {error}", Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete passkey");
            Toast.Add("Failed to remove passkey", Severity.Error);
            return false;
        }
    }

    public record PasskeyInfo
    {
        public string CredentialId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? LastUsedOn { get; set; }
        public string[]? Transports { get; set; }
    }
}
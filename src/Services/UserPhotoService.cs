using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace OutsourceTracker.Services;

public class UserPhotoService
{
    private IHttpClientFactory HttpFactory { get; }

    private UserService UserService { get; }

    private IJSRuntime JS { get; }

    private const string CacheKey = "userProfilePhoto";

    public UserPhotoService(IHttpClientFactory http, UserService user, IJSRuntime js)
    {
        HttpFactory = http;
        UserService = user;
        JS = js;
    }

    public async Task<string> GetAvatarUrlAsync(string fallbackName = "User")
    {
        const string CacheKey = "userProfilePhoto";
        var cached = await JS.InvokeAsync<string>("localStorage.getItem", CacheKey);
        if (!string.IsNullOrEmpty(cached))
            return cached;

        var tokenResult = await UserService.GetToken();

        if (tokenResult == null)
        {
            var name = Uri.EscapeDataString(fallbackName);
            return $"https://ui-avatars.com/api/?name={name}&background=0d6efd&color=fff&size=128&bold=true&rounded=true";
        }

        var client = HttpFactory.CreateClient("Graph");
        using var request = new HttpRequestMessage(HttpMethod.Get, "v1.0/me/photo/$value");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

        var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var dataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";

            await JS.InvokeVoidAsync("localStorage.setItem", CacheKey, dataUrl);
            return dataUrl;
        }
        else
        {
            var name = Uri.EscapeDataString(fallbackName);
            return $"https://ui-avatars.com/api/?name={name}&background=0d6efd&color=fff&size=128&bold=true&rounded=true";
        }
    }

    public async Task ClearCacheAsync()
    {
        await JS.InvokeVoidAsync("localStorage.removeItem", CacheKey);
    }
}

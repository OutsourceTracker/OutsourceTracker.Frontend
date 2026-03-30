using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace OutsourceTracker.Services;

public class UserPhotoService
{
    private IJSRuntime JS { get; }
    private UserService _userService;
    const string SESSIONKEY = "EMAIL-HASH-";

    public UserPhotoService(UserService userService, IJSRuntime js)
    {
        JS = js;
        _userService = userService;
    }

    public async Task<string> GetAvatarUrlAsync(int size = 512, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            email = await _userService.GetCurrentUserEmailAsync() ?? string.Empty;
        }

        string avatarUrl;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var hashedEmail = await ComputeEmailHash(email.Trim().ToLower());
            avatarUrl = $"https://www.gravatar.com/avatar/{hashedEmail}?s={size}&d=robohash&r=r";
        }
        else
        {
            avatarUrl = $"https://ui-avatars.com/api/?name=JohnDoe&background=0d6efd&color=fff&size={size}&bold=true&rounded=true";
        }

        return avatarUrl;
    }

    public async Task ClearCacheAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        
        email = email.Trim().ToLowerInvariant();
        await JS.InvokeVoidAsync("sessionStorage.removeItem", SESSIONKEY + email);
    }

    private async Task<string> ComputeEmailHash(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        email = email.Trim().ToLowerInvariant();
        string cacheKey = SESSIONKEY + email;

        string? hash = await JS.InvokeAsync<string>("sessionStorage.getItem", cacheKey);

        if (!string.IsNullOrWhiteSpace(hash))
        {
            return hash;
        }

        try
        {
            hash = await JS.InvokeAsync<string>("computeSha256Hex", email.Trim().ToLowerInvariant());
            await JS.InvokeVoidAsync("sessionStorage.setItem", cacheKey, hash);
            return hash ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SHA-256 hash failed: {ex.Message}");
            return Uri.EscapeDataString(email.Trim().ToLowerInvariant());
        }
    }
}

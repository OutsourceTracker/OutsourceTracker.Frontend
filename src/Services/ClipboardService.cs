using Microsoft.JSInterop;

namespace OutsourceTracker.Services;

public class ClipboardService
{
    private IJSRuntime JS { get; }

    public ClipboardService(IJSRuntime js)
    {
        JS = js;
    }

    public async Task WriteTextAsync(string text)
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    public async Task<string> ReadTextAsync()
    { 
        return await JS.InvokeAsync<string>("navigator.clipboard.readText");
    }

    public async Task ClearAsync()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", string.Empty);
    }
}

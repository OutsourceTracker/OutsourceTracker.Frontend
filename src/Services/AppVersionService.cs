using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OutsourceTracker.Services.Versioning;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OutsourceTracker.Services;

/// <summary>
/// Service responsible for loading, caching, and comparing the application version information.
/// Supports detecting updates by comparing local (cached) version with remote version.json.
/// Uses localStorage for persistence across sessions and throttles remote checks.
/// </summary>
public class AppVersionService
{
    private readonly HttpClient _httpClient;
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly ILogger<AppVersionService> _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigation;

    private VersionInfo? _localVersion;
    private VersionInfo? _remoteVersion;
    private DateTime _lastRemoteCheck = DateTime.MinValue;

    private const string LocalStorageKey = "OutsourceTracker:AppVersion";
    private const int RemoteCheckIntervalMinutes = 10;

    /// <summary>
    /// Gets a value indicating whether a newer version is available based on commit hash comparison.
    /// </summary>
    public bool IsUpdateAvailable { get; private set; }

    /// <summary>
    /// Gets the currently loaded (local/cached) version information.
    /// </summary>
    public VersionInfo? LocalVersion => _localVersion;

    /// <summary>
    /// Gets the remote (server) version information.
    /// </summary>
    public VersionInfo? RemoteVersion => _remoteVersion;

    /// <summary>
    /// Indicates whether version information has been successfully loaded at least once.
    /// </summary>
    public bool HasLoaded { get; private set; }

    /// <summary>
    /// Gets a user-friendly display string for the current version (e.g. "v1.2.3 (develop)").
    /// </summary>
    public string VersionDisplay => _localVersion?.Display ?? "loading...";

    /// <summary>
    /// Gets just the semantic version part (e.g. "1.2.3").
    /// </summary>
    public string ShortVersion => _localVersion?.Version ?? "dev";

    /// <summary>
    /// Indicates whether this appears to be a development/preview build.
    /// </summary>
    public bool IsDevelopmentBuild => _localVersion?.Branch is "develop" or "dev" or "feature" or null;

    public AppVersionService(
        IHttpClientFactory httpClientFactory,
        ILogger<AppVersionService> logger,
        IWebAssemblyHostEnvironment env,
        IJSRuntime jsRuntime,
        NavigationManager navigation)
    {
        _httpClient = httpClientFactory.CreateClient("Default"); // Use named client if configured, or fallback
        _logger = logger;
        _env = env;
        _jsRuntime = jsRuntime;
        _navigation = navigation;

        // Determine base URI for version.json
        var baseUri = _env.IsDevelopment()
            ? new Uri("https://dispatch.vandersluistrucking.com/")
            : new Uri(_env.BaseAddress);

        _httpClient.BaseAddress = baseUri;
    }

    /// <summary>
    /// Loads the application version from localStorage (if available) and checks the remote version.json.
    /// Safe to call multiple times — skips redundant work after first successful load.
    /// </summary>
    /// <returns>A task that completes when loading is done.</returns>
    public async Task LoadAsync()
    {
        if (HasLoaded)
            return;

        try
        {
            // Try to load from localStorage first
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LocalStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                _localVersion = JsonSerializer.Deserialize<VersionInfo>(json);
                _logger.LogInformation("Loaded version from localStorage: {Version}", _localVersion?.Version);
            }

            // Always attempt to refresh remote version (with throttling)
            await CheckRemoteVersionAsync();

            // If no local version exists yet, fall back to remote or default
            if (_localVersion == null)
            {
                if (_remoteVersion != null)
                {
                    _localVersion = _remoteVersion;
                    await SaveToLocalStorageAsync(_remoteVersion);
                    _logger.LogInformation("Initialized local version from remote");
                }
                else
                {
                    _localVersion = new VersionInfo();
                    _logger.LogWarning("No version information available — using fallback");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load or compare version information");
            _localVersion ??= new VersionInfo();
        }
        finally
        {
            HasLoaded = true;
        }
    }

    /// <summary>
    /// Checks the remote version.json file and updates update availability status.
    /// Throttled to once every {RemoteCheckIntervalMinutes} minutes.
    /// </summary>
    /// <param name="forced">Bypass the cooldown interval</param>
    public async Task CheckRemoteVersionAsync(bool forced = false)
    {
        if (!forced && ((DateTime.UtcNow - _lastRemoteCheck) < TimeSpan.FromMinutes(RemoteCheckIntervalMinutes)))
        {
            _logger.LogDebug("Remote version check skipped — last check was recent");
            return;
        }

        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "version.json");
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true, NoStore = true, MaxAge = TimeSpan.Zero };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            var remote = await response.Content.ReadFromJsonAsync<VersionInfo>();

            if (remote == null)
            {
                _logger.LogWarning("Remote version.json returned null or empty");
                return;
            }

            _remoteVersion = remote;
            _lastRemoteCheck = DateTime.UtcNow;

            if (_localVersion != null)
            {
                IsUpdateAvailable = _localVersion.Commit != _remoteVersion.Commit;

                _logger.LogInformation("Version check: Local {LocalCommit} vs Remote {RemoteCommit} → Update {Status}",
                    _localVersion.Commit, _remoteVersion.Commit, IsUpdateAvailable ? "available" : "not needed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch remote version.json");
        }
    }

    /// <summary>
    /// Convenience method: ensures version is loaded, then returns the display string.
    /// </summary>
    public async Task<string> GetVersionDisplayAsync()
    {
        await LoadAsync();
        return VersionDisplay;
    }

    /// <summary>
    /// Forces a full page reload to apply the latest version (bypasses cache where possible).
    /// For PWA/service-worker scenarios, combine with JS interop to skipWaiting.
    /// </summary>
    public async Task ForceUpdate() => await _jsRuntime.InvokeVoidAsync("window.application.forceUpdateAndReload");

    private async Task SaveToLocalStorageAsync(VersionInfo version)
    {
        try
        {
            var json = JsonSerializer.Serialize(version);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save version to localStorage");
        }
    }
}
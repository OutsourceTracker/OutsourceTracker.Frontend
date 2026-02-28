using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OutsourceTracker.Services.Versioning;
using System.Net.Http.Json;

namespace OutsourceTracker.Services
{
    public class AppVersionService
    {
        private readonly HttpClient _httpClient;
        private readonly IWebAssemblyHostEnvironment _env;
        private readonly ILogger _logger;
        private readonly IJSRuntime _js;

        private VersionInfo? _versionInfoLocal;
        private VersionInfo? _versionInfoRemote;
        private const string VERSION_KEY = "APP_VERSION";
        private bool _hasLoaded;

        public AppVersionService(IHttpClientFactory httpClient, ILogger<AppVersionService> logger, IWebAssemblyHostEnvironment env, IJSRuntime js)
        {
            _httpClient = httpClient.CreateClient();
            _logger = logger;
            _env = env;
            _js = js;
        }

        public VersionInfo? CurrentVersion => _versionInfoLocal;

        public bool HasLoaded => _hasLoaded;

        public string VersionDisplay
            => _versionInfoLocal?.Display ?? "loading...";

        public string ShortVersion
            => _versionInfoLocal?.Version ?? "dev";

        public bool IsDevelopmentBuild
            => _versionInfoLocal?.Branch is "develop" or "dev" or "feature" or null;

        /// <summary>
        /// Loads version information from wwwroot/version.json
        /// Safe to call multiple times — only fetches once
        /// </summary>
        public async Task LoadAsync()
        {
            if (_hasLoaded)
                return;

            Uri baseAddress = new Uri(_env.BaseAddress, UriKind.Absolute);
            Uri versionUrl = new Uri("version.json", UriKind.Relative);

            try
            {
                _versionInfoRemote = await _httpClient.GetFromJsonAsync<VersionInfo>(new Uri(baseAddress, versionUrl));
                _versionInfoLocal = await _js.InvokeAsync<VersionInfo>("localStorage.getItem", VERSION_KEY);
                if (_versionInfoLocal != null)
                {
                    _logger?.LogInformation("Loaded version from localStorage: {Version} ({Branch})", _versionInfoLocal.Version, _versionInfoLocal.Branch);
                }
                else if (_versionInfoRemote != null)
                {
                    _versionInfoLocal = _versionInfoRemote;
                    await _js.InvokeVoidAsync("localStorage.setItem", VERSION_KEY, _versionInfoLocal);
                    _logger?.LogInformation("Loaded version from version.json: {Version} ({Branch})", _versionInfoLocal.Version, _versionInfoLocal.Branch);
                }

                
                if (_versionInfoLocal == null)
                {
                    _versionInfoLocal = new VersionInfo();
                    _logger?.LogWarning("version.json was empty or invalid");
                }
                else
                {
                    _logger?.LogInformation("Loaded version: {Version} ({Branch})",
                        _versionInfoLocal.Version, _versionInfoLocal.Branch);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load version.json — using fallback");
                _versionInfoLocal = new VersionInfo();
            }
            finally
            {
                _hasLoaded = true;
            }
        }

        /// <summary>
        /// Convenience method: load if not already loaded, then return display string
        /// </summary>
        public async Task<string> GetVersionDisplayAsync()
        {
            await LoadAsync();
            return VersionDisplay;
        }
    }
}

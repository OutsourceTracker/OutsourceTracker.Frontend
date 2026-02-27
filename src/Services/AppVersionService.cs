using OutsourceTracker.Services.Versioning;
using System.Net.Http.Json;

namespace OutsourceTracker.Services
{
    public class AppVersionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private VersionInfo? _versionInfo;
        private bool _hasLoaded;

        public AppVersionService(IHttpClientFactory httpClient, ILogger<AppVersionService> logger)
        {
            _httpClient = httpClient.CreateClient();
            _logger = logger;
        }

        public VersionInfo? CurrentVersion => _versionInfo;

        public bool HasLoaded => _hasLoaded;

        public string VersionDisplay
            => _versionInfo?.Display ?? "loading...";

        public string ShortVersion
            => _versionInfo?.Version ?? "dev";

        public bool IsDevelopmentBuild
            => _versionInfo?.Branch is "develop" or "dev" or "feature" or null;

        /// <summary>
        /// Loads version information from wwwroot/version.json
        /// Safe to call multiple times — only fetches once
        /// </summary>
        public async Task LoadAsync()
        {
            if (_hasLoaded)
                return;

            try
            {
                _versionInfo = await _httpClient.GetFromJsonAsync<VersionInfo>("version.json");

                if (_versionInfo == null)
                {
                    _versionInfo = new VersionInfo();
                    _logger?.LogWarning("version.json was empty or invalid");
                }
                else
                {
                    _logger?.LogInformation("Loaded version: {Version} ({Branch})",
                        _versionInfo.Version, _versionInfo.Branch);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load version.json — using fallback");
                _versionInfo = new VersionInfo();
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

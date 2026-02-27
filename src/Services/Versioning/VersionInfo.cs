namespace OutsourceTracker.Services.Versioning
{
    public class VersionInfo
    {
        public string Version { get; set; } = "unknown";
        public string Commit { get; set; } = "unknown";
        public string Built { get; set; } = "unknown";
        public string Branch { get; set; } = "unknown";
        public string BuildId { get; set; } = "unknown";

        public bool IsProduction => Branch == "master" || Branch == "main";
        public string Display => IsProduction
            ? $"v{Version}"
            : $"v{Version} ({Branch})";
    }
}

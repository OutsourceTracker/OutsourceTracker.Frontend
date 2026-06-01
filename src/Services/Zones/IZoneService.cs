using OutsourceTracker.BusinessUnit.Zones;
using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Services.Zones;

public interface IZoneService
{
    Task<List<Zone>> GetAllAsync();

    Task<Zone?> GetByIdAsync(Guid id);

    Task<Zone?> CreateAsync(ZoneCreateModel model);

    Task<Zone?> UpdateAsync(Guid id, ZoneCreateModel model);

    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Creates or updates the geometry (boundary polygon + entry/exit/dock points) for a zone using its ShortCode.
    /// This calls the special boundary endpoint.
    /// </summary>
    Task<bool> SetBoundaryAsync(string shortCode, ZoneBoundaryRequest request);
}

/// <summary>
/// Payload for setting zone geometry (matches backend ZoneBuilder shape).
/// </summary>
public class ZoneBoundaryRequest
{
    public string? FullName { get; set; }
    public List<Vector2> BoundryPoints { get; set; } = new();
    public List<Vector2> EntryPoints { get; set; } = new();
    public List<Vector2> ExitPoints { get; set; } = new();
    public List<Vector2> DockPoints { get; set; } = new();
    public List<Vector2> TrailerPools { get; set; } = new();
}

/// <summary>
/// DTO matching the backend ZoneCreateModel for create/update operations.
/// </summary>
public class ZoneCreateModel
{
    public string ShortCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

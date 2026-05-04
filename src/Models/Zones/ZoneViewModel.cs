using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Models.Zones;

public class ZoneViewModel : IZone<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Polygon Boundry { get; set; }

    public string ShortCode { get; set; }

    public string FullName { get; set; }

    public ICollection<Vector2> EntryPoints { get; set; }

    public ICollection<Vector2> ExitPoints { get; set; }

    public ICollection<Vector2> DockPoints { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public bool Equals(Guid other) => Id.Equals(other);
}

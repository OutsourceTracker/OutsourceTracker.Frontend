using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Models.Zones;

public class ZoneViewModel : IZone<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Polygon Boundry { get; set; }
}

using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using System.Text.Json.Serialization;

namespace OutsourceTracker.Models.Trailers;

public class TrailerViewModel : ITrailer<Guid>
{
    public Guid Id { get; set; }

    public string Prefix { get; set; }

    public string Name { get; set; }

    [JsonIgnore]
    public string FullName => Prefix + Name;

    public TrailerType Type { get; set; }

    public EquipmentState State { get; set; }

    public Guid? AccountId { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public MapCoordinates? Location { get; set; }

    public string? LocatedBy { get; set; }

    public DateTimeOffset? LocatedDate { get; set; }
    
}

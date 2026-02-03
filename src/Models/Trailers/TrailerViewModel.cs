namespace OutsourceTracker.Models.Trailers;

public class TrailerViewModel : ICommericalTrailer<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Prefix { get; set; }

    public double? SpottedLatitude { get; set; }

    public double? SpottedLongitude { get; set; }

    public double? SpottedAccuracy { get; set; }

    public string? SpottedBy { get; set; }

    public DateTimeOffset? SpottedOn { get; set; }
}

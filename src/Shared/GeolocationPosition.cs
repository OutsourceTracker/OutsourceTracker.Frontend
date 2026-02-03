namespace OutsourceTracker.Shared
{
    public class GeolocationPosition
    {
        public GeolocationCoordinates Coords { get; set; }
        public class GeolocationCoordinates
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Accuracy { get; set; }
        }
    }
}

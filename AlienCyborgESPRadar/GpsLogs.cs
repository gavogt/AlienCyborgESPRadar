namespace AlienCyborgESPRadar
{
    public class GpsLogs
    {
        public long Id { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public DateTimeOffset TimestampUtc { get; set; }
        public bool? GpsFix { get; set; }
        public bool? GpsPresent { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? Satellites { get; set; }
        public int? HdopX100 { get; set; }
        public int? FixAgeMs { get; set; }

        public string? RawJson { get; set; } = string.Empty;

    }
}

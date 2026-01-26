namespace AlienCyborgESPRadar
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class GpsLogs
    {
        // Use RadarLogId as the primary key and foreign key to RadarLog.Id
        [Key]
        [ForeignKey("RadarLog")]
        public long RadarLogId { get; set; }

        public RadarLog RadarLog { get; set; } = null!;

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

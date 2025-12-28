namespace AlienCyborgESPRadar
{
    public class RadarLog
    {

        public long Id { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public bool Motion { get; set; }
        public long? TsMs { get; set; }
        public DateTimeOffset TimestampUtc { get; set; }
        public string RawJson { get; set; } = string.Empty;
    }
}

namespace AlienCyborgESPRadar
{
    public sealed class CameraCapture
    {
        public long Id { get; set; }
        public string NodeId { get; set; } = string.Empty;

        public long? TsMs { get; set; }

        public DateTimeOffset TimestampUtc { get; set; }

        public long? RadarLogId { get; set; }
        public RadarLog? RadarLog { get; set; }

        // Image Metadata
        public string ContentType { get; set; } = "image/jpeg";
        public int LengthBytes { get; set; }

        // The actual jpeg bytes
        public byte[] Jpeg { get; set; } = Array.Empty<byte>();

        public string? Reason { get; set; }
    }
}

namespace AlienCyborgESPRadar
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public sealed class BatteryLog
    {
        // Use RadarLogId as primary key and FK to RadarLog.Id
        [Key]
        [ForeignKey("RadarLog")]
        public long RadarLogId { get; set; }

        public RadarLog RadarLog { get; set; } = null!;

        public string NodeId { get; set; } = string.Empty;  

        public DateTime TimestampUtc { get; set; }

        public bool? BatteryOk {  get; set; }         
        public double? BatteryVoltage { get; set; }
        public double? BatteryPercent { get; set; }

        public byte? Max17048ChipId { get; set; }
    }
}

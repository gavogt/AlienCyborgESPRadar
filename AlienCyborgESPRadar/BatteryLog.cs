namespace AlienCyborgESPRadar
{
    public sealed class BatteryLog
    {
        public long Id { get; set; }

        public string NodeId { get; set; } = string.Empty;  

        public DateTime TimestampUtc { get; set; }

        public bool? BatteryOk {  get; set; }         
        public double? BatteryVoltage { get; set; }
        public double? BatteryPercent { get; set; }

        public byte? Max17048ChipId { get; set; }
    }
}

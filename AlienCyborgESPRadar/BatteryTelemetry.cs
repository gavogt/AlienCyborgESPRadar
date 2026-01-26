namespace AlienCyborgESPRadar
{
    public sealed class BatteryTelemetry
    {

        public bool Ok { get; init; }
        public double VoltageV { get; init; }
        public double StateOfChargePct { get; init; }
        public double? RatePctPerHour { get; init; }
        public byte? ChipId { get; init; }
    }
}

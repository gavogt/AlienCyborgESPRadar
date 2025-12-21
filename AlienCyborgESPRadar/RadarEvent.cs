public sealed class RadarEvent
{
    public string NodeId { get; set; } = "";
    public bool Motion { get; set; }
    public string TsMs { get; set; } = "";
}
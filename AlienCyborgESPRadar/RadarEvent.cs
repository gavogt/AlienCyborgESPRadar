using System.Text.Json.Serialization;

public sealed class RadarEvent
{
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = "";

    [JsonPropertyName("motion")]
    public bool Motion { get; set; }

    [JsonPropertyName("tsMs")]
    public string TsMs { get; set; } = "";
}
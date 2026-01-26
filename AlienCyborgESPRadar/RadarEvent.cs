using System.Text.Json.Serialization;

public sealed class RadarEvent
{
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = "";

    [JsonPropertyName("motion")]
    public bool Motion { get; set; }

    [JsonPropertyName("tsMs")]
    public string TsMs { get; set; } = "";

    [JsonPropertyName("gpsPresent")]
    public bool? GpsPresent { get; set; }

    [JsonPropertyName("gpsFix")]
    public bool? GpsFix { get; set; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double? Longitude { get; set; }

    [JsonPropertyName("sats")]
    public int? Satellites { get; set; }

    [JsonPropertyName("hdopX100")]
    public int? HdopX100 { get; set; }

    [JsonPropertyName("fixAgeMs")]
    public int? FixAgeMs { get; set; }

    [JsonPropertyName("battOk")]
    public bool? BattOk { get; set; }

    [JsonPropertyName("battV")]
    public double? BattV { get; set; }

    [JsonPropertyName("battPct")]
    public int? BattPct { get; set; }

    [JsonPropertyName("max17048ChipId")]
    public string? Max17048ChipId { get; set; }
}
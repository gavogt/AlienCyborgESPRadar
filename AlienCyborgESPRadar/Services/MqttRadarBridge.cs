using AlienCyborgESPRadar;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

public sealed class MqttRadarBridge : BackgroundService
{
    private readonly ILogger<MqttRadarBridge> _logger;
    private readonly IHubContext<RadarHub> _hub;
    private IMqttClient? _client;

    private readonly MqttOptions _mqtt;

    // Subscribe to one node:
    private const string Topic = "/RADR-uno-1";
  
    public MqttRadarBridge(ILogger<MqttRadarBridge> logger, IHubContext<RadarHub> hub, IOptions<MqttOptions> opt)
    {
        _logger = logger;
        _hub = hub;
        _mqtt = opt.Value;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var payload = e.ApplicationMessage.PayloadSegment;
                var json = Encoding.UTF8.GetString(payload);

                _logger.LogInformation("MQTT {Topic}: {Json}", e.ApplicationMessage.Topic, json);

                // Parse JSON from Arduino: {"nodeId":"RADR-uno-1","motion":true,"tsMs":"1734740000123"}
                var evt = JsonSerializer.Deserialize<RadarEvent>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (evt is null || string.IsNullOrWhiteSpace(evt.NodeId))
                    return;

                long tsMsNum = 0;
                _ = long.TryParse(evt.TsMs, out tsMsNum);

                await _hub.Clients.All.SendAsync("radarEvent", new
                {
                    nodeId = evt.NodeId,
                    motion = evt.Motion,
                    tsMs = tsMsNum // send number if possible
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MQTT message");
            }
        };

        _client.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("MQTT disconnected: {Reason}", e.ReasonString);
            // Simple reconnect loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, stoppingToken);
                    await ConnectAndSubscribe(stoppingToken);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reconnect attempt failed");
                }
            }
        };

        await ConnectAndSubscribe(stoppingToken);

        // keep service alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ConnectAndSubscribe(CancellationToken ct)
    {
        if (_client is null) return;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqtt.Host, _mqtt.Port)
            // .WithCredentials("user","pass") // if enabled auth in mosquitto
            .WithClientId("aspnet-radar-bridge")
            .WithCleanSession()
            .Build();

        _logger.LogInformation("Connecting to MQTT broker {Host}:{Port} ...", _mqtt.Host, _mqtt.Port);
        await _client.ConnectAsync(options, ct);

        _logger.LogInformation("Subscribing to {Topic}", Topic);
        await _client.SubscribeAsync(Topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is not null && _client.IsConnected)
            await _client.DisconnectAsync();
        await base.StopAsync(cancellationToken);
    }
}
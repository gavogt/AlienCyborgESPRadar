using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MQTTnet;
using MQTTnet.Client;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AlienCyborgESPRadar;

public sealed class IngestWorker : BackgroundService
{
    private IConnection? _rabbitConn;
    private IChannel? _rabbitCh;
    private IMqttClient? _mqttClient;
    private ILogger<IngestWorker> _logger;
    private readonly MqttOptions _mqttOptions = new();

    public IngestWorker(ILogger<IngestWorker> logger)
    {
        _logger = logger;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true

    };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        _rabbitConn = await factory.CreateConnectionAsync(ct);
        _rabbitCh = await _rabbitConn.CreateChannelAsync(
            options: null,
            cancellationToken: ct
            );

        await _rabbitCh.ExchangeDeclareAsync("radar.events", ExchangeType.Topic, durable: true, cancellationToken: ct);

        // create/bind queues here 
        await _rabbitCh.QueueDeclareAsync("radar.persist", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _rabbitCh.QueueBindAsync("radar.persist", "radar.events", "motion.*", cancellationToken: ct);

        // MQTT hookup (publish example)
        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic ?? "";
            var payload = e.ApplicationMessage.PayloadSegment.Array is null ? ""
                : Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogInformation("MQTT IN topic={topic} payload={payload}", topic, payload);

            if (topic.EndsWith("status", StringComparison.OrdinalIgnoreCase))
                return;

            RadarEvent? evtObj = null;
            try
            {
                evtObj = JsonSerializer.Deserialize<RadarEvent>(payload, JsonOpts);

            }
            catch (JsonException)
            {
                // log bad JSON?
                return;
            }

            var routingKey = $"motion.{evtObj?.NodeId}";
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evtObj));

            var props = new BasicProperties { Persistent = true };

            await _rabbitCh.BasicPublishAsync(
                exchange: "radar.events",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

        };

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttOptions.Host, _mqttOptions.Port)
            .Build();

        await _mqttClient.ConnectAsync(mqttOptions, ct);
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(_mqttOptions.Topic)
            .WithAtMostOnceQoS()
            .Build());

        while (!ct.IsCancellationRequested)
            await Task.Delay(1000, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_rabbitCh is not null) await _rabbitCh.CloseAsync(cancellationToken);
        if (_rabbitConn is not null) await _rabbitConn.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
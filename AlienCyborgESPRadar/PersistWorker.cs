using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AlienCyborgESPRadar;

public sealed class PersistWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _conn;
    private IChannel? _ch;

    private readonly ILogger<PersistWorker> _logger;
    private readonly IHubContext<RadarHub> _hub;

    private static string? ExtractNodeId(string s) => null;
    private static bool ExtractMotion(string s) => s.Contains("motion", StringComparison.OrdinalIgnoreCase);
    private static string? ExtractTsMs(string s) => null;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PersistWorker(IServiceScopeFactory scopeFactory, ILogger<PersistWorker> logger, IHubContext<RadarHub> hub)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("PersistWorker starting");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",

        };

        _conn = await factory.CreateConnectionAsync(ct);
        _ch = await _conn.CreateChannelAsync(options: null, cancellationToken: ct);

        // Ensure exchange/queue exist 
        await _ch.ExchangeDeclareAsync("radar.events", ExchangeType.Topic, durable: true, cancellationToken: ct);
        await _ch.QueueDeclareAsync("radar.persist", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _ch.QueueBindAsync("radar.persist", "radar.events", "motion.#", cancellationToken: ct);

        await _ch.BasicQosAsync(prefetchSize: 0, prefetchCount: 25, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_ch);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var rawJson = Encoding.UTF8.GetString(ea.Body.Span);

            try
            {
                var evtObj = JsonSerializer.Deserialize<RadarEvent>(rawJson, JsonOpts) ??
                throw new Exception("Bad radar json");

                long? tsMs = null;
                if (long.TryParse(evtObj.TsMs, out var parsed))
                    tsMs = parsed;

                var tsUtc = tsMs.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(tsMs.Value).UtcDateTime
                    : DateTime.UtcNow;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RadarDbContext>();

                var dbGps = scope.ServiceProvider.GetRequiredService<GpsDbContext>();

                db.RadarLogs.Add(new RadarLog
                {
                    NodeId = evtObj.NodeId,
                    Motion = evtObj.Motion,
                    TsMs = tsMs,
                    TimestampUtc = tsUtc,
                    RawJson = rawJson
                });

                dbGps.GpsLogs.Add(new GpsLogs
                {
                    NodeId = evtObj.NodeId,
                    TimestampUtc = tsUtc,
                    GpsPresent = evtObj.GpsPresent,
                    GpsFix = evtObj.GpsFix,
                    Latitude = evtObj.Latitude,
                    Longitude = evtObj.Longitude,
                    Satellites = evtObj.Satellites,
                    HdopX100 = evtObj.HdopX100,
                    FixAgeMs = evtObj.FixAgeMs,
                    RawJson = rawJson
                });

                _logger.LogInformation("RadarDb={db} | GpsDb={gpsDb}",
                db.Database.GetDbConnection().Database,
                dbGps.Database.GetDbConnection().Database);

                try
                {
                    await dbGps.SaveChangesAsync(ct);
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DB update failed. RawJson={RawJson}", rawJson);
                    throw; 
                }

                _logger.LogInformation("Persisted radar event from {NodeId} (motion={Motion})", evtObj.NodeId, evtObj.Motion);

                await _hub.Clients.All.SendAsync("radarEvent", new
                {
                    nodeId = evtObj.NodeId,
                    motion = evtObj.Motion,
                    tsMs = evtObj.TsMs,
                    GpsPresent = evtObj.GpsPresent,
                    GpsFix = evtObj.GpsFix,
                    Latitude = evtObj.Latitude,
                    Longitude = evtObj.Longitude,
                    Sats = evtObj.Satellites,
                    HdopX100 = evtObj.HdopX100,
                    FixAgeMs = evtObj.FixAgeMs,
                    timestampUtc = tsUtc,
                    BattOk = evtObj.BattOk,
                    BattV = evtObj.BattV,
                    BattPct = evtObj.BattPct,
                    Max17048ChipId = evtObj.Max17048ChipId

                }, ct);

                await _ch!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                // Log error and the raw JSON so failures are visible in logs
                _logger.LogError(ex, "Failed to persist radar event. RawJson: {RawJson}", rawJson);
                await _ch!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);

            }
        };

        await _ch.BasicConsumeAsync(
            queue: "radar.persist",
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        // Keep the background service alive
        while (!ct.IsCancellationRequested)
            await Task.Delay(1000, ct);

        _logger.LogInformation("PersistWorker stopping");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_ch is not null) await _ch.CloseAsync(cancellationToken);
        if (_conn is not null) await _conn.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

}
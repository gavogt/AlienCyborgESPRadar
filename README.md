# AlienCyborg ESP Radar

A lightweight Razor Pages dashboard and background services for ingesting, processing, and persisting motion events from ESP-based radar nodes. Events flow from MQTT → RabbitMQ → background workers → SQL Server, and live updates are pushed to browser dashboards via SignalR.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

## Features

- Real-time dashboard using SignalR and Leaflet (`wwwroot/js/radar.js`).
- MQTT bridge (`MqttRadarBridge`) to forward device messages.
- Ingest worker (`IngestWorker`) that publishes to RabbitMQ exchange `radar.events`.
- Persist worker (`PersistWorker`) that consumes from `radar.persist` and writes `RadarLog` entries to `RadarDb`.
- Optional LLM agents using `LmStudioClient` for analysis (Summarizer, AnomalyDetector, ActionAdvisor).

## Prerequisites

- .NET 10 SDK
- Microsoft SQL Server (or SQL Server Express)
- RabbitMQ (management plugin recommended)
- MQTT broker (e.g., Mosquitto)
- Optional: LM Studio or compatible LLM endpoint (default base URL in `Program.cs` is `http://localhost:1234/v1/`)

## Quick start (local)

1. Clone the repo:

   ```bash
   git clone https://github.com/gavogt/AlienCyborgESPRadar.git
   cd AlienCyborgESPRadar
   ```

2. Configure `appsettings.json` (or create `appsettings.Development.json`) to point to your SQL Server and MQTT broker. Important keys:

   - `ConnectionStrings:AuthConnection` - used by identity database (`AuthDbContext`).
   - `ConnectionStrings:RadarConnection` - used by radar logs (`RadarDbContext`).
   - `Mqtt:Host` and `Mqtt:Port` - MQTT broker for `MqttRadarBridge` and `IngestWorker`.

   Example (already present):

   ```json
   {
     "ConnectionStrings": {
       "AuthConnection": "Server=.;Database=AuthDb;Trusted_Connection=True;",
       "RadarConnection": "Server=.;Database=RadarDb;Trusted_Connection=True;"
     },
     "Mqtt": { "Host": "192.168.1.197", "Port": 1883 }
   }
   ```

3. Ensure EF tools are available (install if needed):

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet restore
   ```

4. Add / apply migrations and create databases. From the project folder where `.csproj` is located:

   ```bash
   # If migrations are not present, create them
   dotnet ef migrations add InitialAuth --context AuthDbContext
   dotnet ef migrations add InitialRadar --context RadarDbContext

   # Apply migrations
   dotnet ef database update --context AuthDbContext
   dotnet ef database update --context RadarDbContext
   ```

   Note: Migrations may already be included. If so, only run the `database update` steps.

5. Optional: start supporting services with Docker Compose (if `docker-compose.yml` defines RabbitMQ / LM Studio):

   ```bash
   docker-compose up -d
   ```

6. Run the app

   - Visual Studio 2026: open solution and press F5
   - CLI:

     ```bash
     dotnet run --project AlienCyborgESPRadar
     ```

7. Open the dashboard in your browser (defaults to `https://localhost:5001` or the port shown in the console). The dashboard page is a Razor Page (see `Pages/Dashboard.cshtml`).

## Testing ingest and persistence

- HTTP test endpoint (bypasses MQTT):

  ```bash
  curl -X POST https://localhost:5001/api/radar/event \
    -H "Content-Type: application/json" \
    -d '{"nodeId":"RADR-uno-1","motion":true,"tsMs":"1734740000123"}'
  ```

  This broadcasts to the SignalR clients but does not persist; `IngestWorker` or publishing to RabbitMQ is required for persistence pipeline.

- Example MQTT payload published by devices (JSON):

  ```json
  { "nodeId": "RADR-uno-1", "motion": true, "tsMs": "1734740000123" }
  ```

  `IngestWorker` subscribes to `/#` and forwards non-status messages into RabbitMQ exchange `radar.events` with routing key `motion.{nodeId}`.

- You can publish directly to RabbitMQ (exchange `radar.events`, routing key `motion.TEST`) to test persistence.

## LM Studio / Agents

- `LmStudioClient` is registered in `Program.cs` with base URL `http://localhost:1234/v1/`.
- Agents (`SummarizerAgent`, `AnomalyAgent`, `ActionAgent`) use placeholder model names `"your-model-name-here"`. Replace these with models available in your LM Studio instance.
- Ensure LM Studio is running and reachable; check `LmStudioClient` log output for HTTP failures.

## Troubleshooting

- No rows in `RadarLogs`:
  - Verify `PersistWorker` is running (hosted services start at app start). Look for `PersistWorker starting` in logs.
  - Confirm messages reach RabbitMQ and that queue `radar.persist` has a consumer. Use RabbitMQ Management UI.
  - Check `IngestWorker` is registered (it publishes MQTT -> RabbitMQ); if you only compiled earlier code it may have been missing.
  - Check database connection string and run migrations.

- LM Studio calls return empty:
  - Replace placeholder model names.
  - Ensure LM Studio is running and reachable at the configured base URL.
  - Enable logging in `LmStudioClient` to surface HTTP status and response body.

- SignalR clients not receiving updates:
  - Ensure `RadarHub` is mapped at `/radarHub` (see `Program.cs`).
  - Open browser console and watch for `signalr` errors.

## Useful commands

- Run migrations:

```
  dotnet ef database update --context RadarDbContext
  dotnet ef database update --context AuthDbContext
```

- Run project:

```
  dotnet run --project AlienCyborgESPRadar
```

- Inspect RabbitMQ queues (management UI usually at `http://localhost:15672`).

## Contributing

Contributions welcome — please open issues and PRs. Follow `.editorconfig` and `CONTRIBUTING.md` if present.

## License

See `LICENSE.txt`.

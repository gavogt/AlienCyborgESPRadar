using AlienCyborgESPRadar;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MQTTnet.Client;
using Microsoft.EntityFrameworkCore;
using AlienCyborgESPRadar.Data;
using AlienCyborgESPRadar.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection"))
    );

builder.Services.AddDbContext<RadarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RadarConnection"))
    );

builder.Services.AddDbContext<GpsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GpsConnection"))
    );

builder.Services.AddDbContext<BatteryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BatteryConnection"))
    );

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Register LLM
builder.Services.AddHttpClient<LmStudioClient>(http =>
{
    http.BaseAddress = new Uri("http://localhost:1234/v1/");
    http.Timeout = TimeSpan.FromSeconds(400);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttRadarBridge>();
// Bridge MQTT -> RabbitMQ for ingestion of radar events
builder.Services.AddHostedService<IngestWorker>();
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));

// Register Ai Agents
builder.Services.AddScoped<IAgent, SummarizerAgent>();
builder.Services.AddScoped<IAgent, ActionAgent>();
builder.Services.AddScoped<IAgent, AnomalyAgent>();

// Register Ai Agent Orchestrator
builder.Services.AddScoped<RadarAnalysisOrchestrator>();

// Register Ai background worker
builder.Services.AddHostedService<RadarAnalysisWorker>();

// Register Persist background worker
builder.Services.AddHostedService<PersistWorker>();

var app = builder.Build();
app.Logger.LogInformation("App booted: {AppName}", app.Environment.ApplicationName);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapPost("/api/radar/event", async (RadarEvent evt, IHubContext<RadarHub> hub) =>
{
    // Broadcast to all connected browsers
    await hub.Clients.All.SendAsync("radarEvent", evt);
    return Results.Ok();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<RadarHub>("/radarHub");

app.Run();

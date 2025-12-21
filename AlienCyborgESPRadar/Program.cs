using AlienCyborgESPRadar;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttRadarBridge>();

var app = builder.Build();

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


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<RadarHub>("/radarHub");

app.Run();

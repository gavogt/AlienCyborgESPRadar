using AlienCyborgESPRadar;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MQTTnet.Client;
using Microsoft.EntityFrameworkCore;
using AlienCyborgESPRadar.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection"))
    );

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttRadarBridge>();
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));

var app = builder.Build();

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

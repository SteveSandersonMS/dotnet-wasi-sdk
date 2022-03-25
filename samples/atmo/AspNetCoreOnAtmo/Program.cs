using Wasi.AspNetCore.Server.Atmo;
using AtmoCache = Wasi.AspNetCore.Server.Atmo.Services.Cache;

AtmoLogger.RedirectConsoleToAtmoLogs();
var builder = WebApplication.CreateBuilder(args).UseAtmoServer();
builder.Services.AddRazorPages();
builder.Services.AddAtmoCache();

var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();

app.MapGet("/", () => "Hello, world! See also: /api/getvalue/{key} and /api/setvalue/{key}/{value}");

// Demonstrate getting and setting values in Atmo's key-value store, which is backed by Redis

app.MapGet("/api/getvalue/{key}", (AtmoCache cache, string key) =>
{
    return cache.GetString(key) is string result
        ? Results.Ok(result)
        : Results.NotFound();
});

app.MapGet("/api/setvalue/{key}/{value}", (AtmoCache cache, string key, string value) =>
{
    cache.Set(key, value, TimeSpan.FromMinutes(1));
    return Results.Ok();
});

await app.StartAsync();

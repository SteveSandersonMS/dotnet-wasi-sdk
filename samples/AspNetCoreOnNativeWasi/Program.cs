var builder = WebApplication.CreateBuilder(args).UseWasiConnectionListener();
builder.Services.AddRazorPages();

var app = builder.Build();
app.UseBundledStaticFiles();
app.MapRazorPages();

app.MapGet("/", () => "Hello, world! See also: /weatherforecast and /myrazorpage");

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    var forecast = Enumerable.Range(1, 5).Select(index => new
    {
        Date = DateTime.Now.AddDays(index),
        TempC = Random.Shared.Next(-20, 55),
        Summary = summaries[Random.Shared.Next(summaries.Length)]
    }).ToArray();
    return forecast;
});

app.Start();

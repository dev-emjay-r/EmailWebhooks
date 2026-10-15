using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Amazon.SimpleNotificationService.Util;
using System.IO;
using EmailWebhooks.Models;

DotNetEnv.Env.TraversePath().Load();
// Load config
var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AutoMailer","conf.ini");
var SQLITE_PATH = Environment.GetEnvironmentVariable("SQLITE_PATH");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(o => o.ServiceName = "EmailWebhooksService");
// Register SQLite
builder.Services.AddDbContext<AppDbContext>(options =>options.UseSqlite($"Data Source={SQLITE_PATH}"));

builder.Services.AddHttpClient();
builder.Services.AddControllers();
var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/status/health", (
AppDbContext db,
 ILogger<Program> logger
) =>
{
    // Dapat dito mag query tayo ng health for the webhooks
    // Check natin if running si sqlite
        var path = db.Database.GetDbConnection().DataSource;
logger.LogInformation("Connecting to DB at: {path}", path);
    return Results.Ok();
});


app.Run();


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<DeliveryEvents> DeliveryEvents => Set<DeliveryEvents>();
    public DbSet<OutboxEmail> Outbox=> Set<OutboxEmail>();
    public DbSet<UnverifiedEmail> UnverifiedEmail => Set<UnverifiedEmail>();
}

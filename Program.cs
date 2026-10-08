using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Amazon.SimpleNotificationService.Util;
using System.IO;
using EmailWebhooks.Models;

// Load config
var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AutoMailer","conf.ini");
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AutoMailer","database.db");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(o => o.ServiceName = "EmailWebhooksService");
// Register SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddHttpClient();
var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();


app.MapGet("/status/health", (
AppDbContext db

) =>
{
    // Dapat dito mag query tayo ng health for the webhooks
    // Check natin if running si sqlite

    return Results.Ok();
});

app.MapPost("/webhooks/ses", async (
    HttpRequest request,
    IHttpClientFactory httpFactory,
    ILogger<Program> logger,
    AppDbContext db) =>
{
    // SNS sends JSON with Content-Type text/plain, so read the raw body
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    Message msg;
    try
    {
        msg = Message.ParseMessage(body);
    }
    catch
    {
        return Results.BadRequest();
    }

    // Verifies the signature so nobody can spoof events
    if (!msg.IsMessageSignatureValid())
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (msg.IsSubscriptionType)
    {
        // Confirms the subscription
        var client = httpFactory.CreateClient();
        var response = await client.GetAsync(msg.SubscribeURL);
        logger.LogInformation("Subscription confirm status: {Status}", response.StatusCode);
    }
    else if (msg.IsNotificationType)
    {
        // The actual SES event is a JSON string inside msg.MessageText
        using var doc = JsonDocument.Parse(msg.MessageText);
        var root = doc.RootElement;
        var sesModel = SesDeliveryEvent.FromJson(msg.MessageText);
        var eventType =
            root.TryGetProperty("eventType", out var et) ? et.GetString() :
            root.TryGetProperty("notificationType", out var nt) ? nt.GetString() :
            "unknown";
            // Get the header value message id
        
        string? targetId = null;
        // 2. Get the Nested Timestamp
        DateTime? mailTimestamp = null;
        
        if (root.TryGetProperty("mail", out var mail) &&
            mail.TryGetProperty("timestamp", out var ts) &&
            DateTime.TryParse(ts.GetString(), out var parsed))
        {
            mailTimestamp = parsed;
            if (mail.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Array)
        {
            var idObj = headers.EnumerateArray().FirstOrDefault(h => h.TryGetProperty("name", out var n) && n.GetString() == "Message-Id");
            //logger.LogInformation("targetID {Id}", idObj);
            //logger.LogInformation("targetID {Id}", headers);
            if (idObj.ValueKind != JsonValueKind.Undefined)
            {
                targetId = idObj.TryGetProperty("value", out var v) ? v.GetString() : null;
            }
        }
        }
        
        //logger.LogInformation("SES event: {EventType} {Payload}", eventType, msg.MessageText);
        if (!string.IsNullOrEmpty(targetId))
        {
        logger.LogInformation("targetID {Id}", targetId.Trim('<', '>'));
        
            // Save to SQLite
            mail.TryGetProperty("messageId", out var eventID);
            _ = db.DeliveryEvents.Add(new DeliveryEvents
            {
                ProviderEventId = sesModel?.Mail.MessageId ?? "",//eventID.GetString(), // Message ID from AWS 
                MessageId = targetId ?? "",
                EventType = sesModel?.EventType ?? "",
                Detail = sesModel?.Delivery.SmtpResponse,
                ReceivedUtc = sesModel?.Delivery.Timestamp.UtcDateTime ?? DateTime.UtcNow,
                Processed = false
            });
        }
        
            
        await db.SaveChangesAsync();
        }
        
   

    return Results.Ok();
});

app.Run();

    public class OutboxEmail
    {
        public long Id { get; set; }
        public string GeneralCode { get; set; }= string.Empty;
        public string ClientName { get; set; }= string.Empty;
        public string ToAddress { get; set; }= string.Empty;
        public string InvoicePath { get; set; }= string.Empty;
        public string InvoiceNumber { get; set; }= string.Empty;
        public EmailDeliveryStatus Status { get; set; }
        public int Attempts { get; set; }
        public DateTime? NextAttemptUtc { get; set; }
        public string? LastError { get; set; } = string.Empty;
        public string MessageId { get; set; }= string.Empty;
        public DateTime CreatedUtc { get; set; }
        public DateTime? SentUtc { get; set; }
        public DateTime? StatusUtc { get; set; }

    }
// Database Models

  public enum EmailDeliveryStatus
    {
        Unknown,
        Pending,
        Sending,
        Sent, // Handed off the mail server successfully
        Delivered, // Confirmed received by the recipient's server
        Bounced, //failure to delivery to the recipient
        Failed, // failed before reaching the mail server
        Rejected, // Rejected by the mail server 
        HardBounce,
        SoftBounce,
        NetworkFailure
    }

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<DeliveryEvents> DeliveryEvents => Set<DeliveryEvents>();
    public DbSet<OutboxEmail> Outbox=> Set<OutboxEmail>();
}

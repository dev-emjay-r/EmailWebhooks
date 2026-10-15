using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Amazon.SimpleNotificationService.Util;
using EmailWebhooks.Models;
namespace EmailWebhooks.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WebhooksController> _logger;
    private readonly AppDbContext _db;


     public WebhooksController(
        IHttpClientFactory httpFactory,
        ILogger<WebhooksController> logger,
        AppDbContext db)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _db = db;
    }

    [HttpPost("ses")]
    public async Task<IActionResult> HandleSes()
    {
         //SNS sends JSON with Content-Type text/plain, so read the raw body
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();

    Message msg;
    try
    {
        msg = Message.ParseMessage(body);
    }
    catch
    {
        return BadRequest();
    }

    // Verifies the signature so nobody can spoof events
    if (!msg.IsMessageSignatureValid())
        return StatusCode(StatusCodes.Status403Forbidden);

    if (msg.IsSubscriptionType)
    {
        // Confirms the subscription
        var client = _httpFactory.CreateClient();
        var response = await client.GetAsync(msg.SubscribeURL);
        _logger.LogInformation("Subscription confirm status: {Status}", response.StatusCode);
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
        _logger.LogInformation("targetID {Id}", targetId.Trim('<', '>'));
        
            // Save to SQLite
            mail.TryGetProperty("messageId", out var eventID);
            _ = _db.DeliveryEvents.Add(new DeliveryEvents
            {
                ProviderEventId = sesModel?.Mail.MessageId ?? "",//eventID.GetString(), // Message ID from AWS 
                MessageId = targetId ?? "",
                EventType = sesModel?.EventType ?? "",
                Detail = sesModel?.Delivery.SmtpResponse,
                ReceivedUtc = sesModel?.Delivery.Timestamp.UtcDateTime ?? DateTime.UtcNow,
                Processed = false
            });
        }
        else
        {
            //logger.LogInformation("{obj}",msg.MessageText);
            _ = _db.UnverifiedEmail.Add(
                new UnverifiedEmail
                {
                    EventType = sesModel?.EventType ?? "",
                    RecipientEmail = sesModel?.Mail?.CommonHeaders.To[0] ?? "",
                    SenderEmail = sesModel?.Mail?.CommonHeaders.From[0] ?? "",
                    Details = msg.MessageText,
                    Timestamp = sesModel?.Mail?.Timestamp ?? DateTimeOffset.Now
                }
            );
        }
        
            
        await _db.SaveChangesAsync();
        }
        
   

    return Ok();
    }
}
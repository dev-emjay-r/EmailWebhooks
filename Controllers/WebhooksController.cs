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



    [HttpPost("mailpit")]
    public async Task<IActionResult> HandleMailpit()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        // _logger.LogInformation("Mailpit event: {Payload}", body);


        // Check if the body is a valid JSON
        try
        {
            using var document = JsonDocument.Parse(body);

            List<MailpitDeliveryEvent> mailpitEvents;
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                mailpitEvents = MailpitDeliveryEvent.FromJsonArray(body);
            }
            else
            {
                var mailpitEvent = MailpitDeliveryEvent.FromJson(body);
                mailpitEvents = mailpitEvent is null
                    ? new()
                    : new() { mailpitEvent };
            }

            foreach (var mailpitEvent in mailpitEvents)
            {
                _logger.LogInformation(
                    "Mailpit event {EventId}: {EventType} for {Email}",
                    mailpitEvent.SgEventId,
                    mailpitEvent.Event,
                    mailpitEvent.Email);

                _ = _db.DeliveryEvents.Add(new DeliveryEvents
                {
                    delivery_provider_event_id = mailpitEvent.SgEventId,
                    delivery_message_id = mailpitEvent.SmtpId,
                    delivery_event_type = mailpitEvent.Event[0].ToString().ToUpper() + mailpitEvent.Event[1..], // capitalize first letter
                    delivery_detail = $"Pool: {mailpitEvent.Pool.Name}",
                    delivery_received_utc = mailpitEvent.TimestampUtc.UtcDateTime,
                    delivery_processed = false
                });
            }

            await _db.SaveChangesAsync();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Parse Invalid JSON received from Mailpit: {Payload}, Error: {Error}", body, ex.Message);
            return BadRequest("Invalid JSON");
        }
        

        return Ok();
    }

    [HttpGet("ses")]
    public IActionResult StatusSes()
    {
        return Ok("SES Webhook is working");
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
                delivery_provider_event_id = sesModel?.Mail.MessageId ?? "",//eventID.GetString(), // Message ID from AWS 
                delivery_message_id = targetId ?? "",
                delivery_event_type = sesModel?.EventType ?? "",
                delivery_detail = sesModel?.Delivery.SmtpResponse,
                delivery_received_utc = sesModel?.Delivery.Timestamp.UtcDateTime ?? DateTime.UtcNow,
                delivery_processed = false
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
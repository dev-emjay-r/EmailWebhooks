using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmailWebhooks.Models
{

    public class SesDeliveryEvent
    {
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("mail")]
        public SesMail Mail { get; set; } = new();

        [JsonPropertyName("delivery")]
        public SesDelivery Delivery { get; set; } = new();

        [JsonPropertyName("bounce")]
        public SesBounce? Bounce { get; set; }
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static SesDeliveryEvent? FromJson(string json) =>
            JsonSerializer.Deserialize<SesDeliveryEvent>(json, Options);
    }

  
    public class SesMail
    {
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("sourceArn")]
        public string SourceArn { get; set; } = string.Empty;

        [JsonPropertyName("sendingAccountId")]
        public string SendingAccountId { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public List<string> Destination { get; set; } = new();

        [JsonPropertyName("headersTruncated")]
        public bool HeadersTruncated { get; set; }

        [JsonPropertyName("headers")]
        public List<SesHeader> Headers { get; set; } = new();

        [JsonPropertyName("commonHeaders")]
        public SesCommonHeaders CommonHeaders { get; set; } = new();

        // Keys look like "ses:operation", values are arrays of strings.
        [JsonPropertyName("tags")]
        public Dictionary<string, List<string>> Tags { get; set; } = new();
    }

    public class SesHeader
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class SesCommonHeaders
    {
        [JsonPropertyName("from")]
        public List<string> From { get; set; } = new();

        // The Date header is an RFC 2822 string, so it is kept as text.
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public List<string> To { get; set; } = new();

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;
    }

    public class SesDelivery
    {
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("processingTimeMillis")]
        public int ProcessingTimeMillis { get; set; }

        [JsonPropertyName("recipients")]
        public List<string> Recipients { get; set; } = new();

        [JsonPropertyName("smtpResponse")]
        public string SmtpResponse { get; set; } = string.Empty;

        [JsonPropertyName("remoteMtaIp")]
        public string RemoteMtaIp { get; set; } = string.Empty;

        [JsonPropertyName("reportingMTA")]
        public string ReportingMta { get; set; } = string.Empty;
    }

    // Add these new classes at the bottom of the file
    public class SesBounce
    {
        [JsonPropertyName("feedbackId")]
        public string FeedbackId { get; set; } = string.Empty;

        [JsonPropertyName("bounceType")]
        public string BounceType { get; set; } = string.Empty;

        [JsonPropertyName("bounceSubType")]
        public string BounceSubType { get; set; } = string.Empty;

        [JsonPropertyName("bouncedRecipients")]
        public List<SesBouncedRecipient> BouncedRecipients { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("remoteMtaIp")]
        public string RemoteMtaIp { get; set; } = string.Empty;

        [JsonPropertyName("reportingMTA")]
        public string ReportingMta { get; set; } = string.Empty;
    }

    public class SesBouncedRecipient
    {
        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("diagnosticCode")]
        public string DiagnosticCode { get; set; } = string.Empty;
    }

}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmailWebhooks.Models
{
    public class MailpitDeliveryEvent
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("pool")]
        public MailpitPool Pool { get; set; } = new();

        [JsonPropertyName("sg_event_id")]
        public string SgEventId { get; set; } = string.Empty;

        [JsonPropertyName("sg_message_id")]
        public string SgMessageId { get; set; } = string.Empty;

        [JsonPropertyName("smtp-id")]
        public string SmtpId { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        public DateTimeOffset TimestampUtc =>
            DateTimeOffset.FromUnixTimeSeconds(Timestamp);

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static MailpitDeliveryEvent? FromJson(string json) =>
            JsonSerializer.Deserialize<MailpitDeliveryEvent>(json, Options);

        public static List<MailpitDeliveryEvent> FromJsonArray(string json) =>
            JsonSerializer.Deserialize<List<MailpitDeliveryEvent>>(json, Options) ?? new();
    }

    public class MailpitPool
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
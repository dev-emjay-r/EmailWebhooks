
namespace  EmailWebhooks.Models
{

        public class DeliveryEvents
        {
            public long Id { get; set; }
            public string? ProviderEventId { get; set; }   // provider's id, used for de-duplication
            public string MessageId { get; set; } = string.Empty;   // matches Outbox.MessageId
            public string EventType { get; set; } = string.Empty;   // delivered, soft_bounce, hard_bounce, ...
            public string? Detail { get; set; }
            public DateTime ReceivedUtc { get; set; }
            public bool Processed { get; set; }

        }
}
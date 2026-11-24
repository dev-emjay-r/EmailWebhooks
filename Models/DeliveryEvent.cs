
using System.ComponentModel.DataAnnotations;

namespace  EmailWebhooks.Models
{

        public class DeliveryEvents
        {
            [Key]
            public long delivery_event_id { get; set; }
            public string? delivery_provider_event_id { get; set; }   // provider's id, used for de-duplication
            public string delivery_message_id { get; set; } = string.Empty;   // matches Outbox.MessageId
            public string delivery_event_type { get; set; } = string.Empty;   // delivered, soft_bounce, hard_bounce, ...
            public string? delivery_detail { get; set; }
            public DateTime delivery_received_utc { get; set; }
            public bool delivery_processed { get; set; } = false;

        }
}
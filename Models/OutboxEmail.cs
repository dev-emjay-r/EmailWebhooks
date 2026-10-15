namespace EmailWebhooks.Models
{
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
}
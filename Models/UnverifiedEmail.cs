

namespace EmailWebhooks.Models
{
    
        public class UnverifiedEmail
        {
            public long Id {get;set;}
            public string EventType {get;set;} = string.Empty;
            public string RecipientEmail {get;set;} = string.Empty;
            public string SenderEmail {get;set;} = string.Empty;
            public string Details {get;set;} = string.Empty;
            public DateTimeOffset Timestamp {get;set;} = DateTimeOffset.Now;
        }
}
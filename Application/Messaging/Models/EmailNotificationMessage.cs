namespace Application.Messaging.Models
{
    public class EmailNotificationMessage
    {
        public string To { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
    }
}

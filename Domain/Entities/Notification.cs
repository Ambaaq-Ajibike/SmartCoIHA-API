using Domain.Enums;

namespace Domain.Entities
{
    public class Notification(string title, string message, NotificationType type, Guid patientId, Guid? dataRequestId)
    {
        public Notification() : this(string.Empty, string.Empty, NotificationType.DataRequestCreated, Guid.Empty, null) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PatientId { get; private set; } = patientId;
        public string Title { get; private set; } = title ?? throw new ArgumentNullException(nameof(title));
        public string Message { get; private set; } = message ?? throw new ArgumentNullException(nameof(message));
        public NotificationType Type { get; private set; } = type;
        public bool IsRead { get; private set; } = false;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public Guid? DataRequestId { get; private set; } = dataRequestId;

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}

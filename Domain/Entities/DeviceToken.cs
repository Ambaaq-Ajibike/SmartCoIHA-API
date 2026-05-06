namespace Domain.Entities
{
    public class DeviceToken(Guid patientId, string token, string platform)
    {
        public DeviceToken() : this(Guid.Empty, string.Empty, string.Empty) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PatientId { get; private set; } = patientId;
        public string Token { get; private set; } = token ?? throw new ArgumentNullException(nameof(token));
        public string Platform { get; private set; } = platform ?? throw new ArgumentNullException(nameof(platform));
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

        public void UpdateToken(string newToken)
        {
            Token = newToken;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

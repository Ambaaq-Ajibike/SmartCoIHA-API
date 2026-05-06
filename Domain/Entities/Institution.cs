using Domain.Enums;

namespace Domain.Entities
{
    public class Institution(string name, string address, string registrationId)
    {
        public Institution() : this(string.Empty, string.Empty, string.Empty) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public string RegistrationId { get; private set; } = registrationId ?? throw new ArgumentNullException(nameof(registrationId));
        public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));
        public string Address { get; set; } = address ?? throw new ArgumentNullException(nameof(address));
        public List<InstituteBaserUrl> FhirEndpoints { get; private set; } = [];
        public VerificationStatus VerificationStatus { get; private set; } = VerificationStatus.Pending;

        public void UpdateVerificationStatus(VerificationStatus status)
        {
            VerificationStatus = status;
        }
    }
}
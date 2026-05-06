using Domain.Enums;

namespace Domain.Entities
{
    public class InstituteBaserUrl(string url, Guid institutionId)
    {
        private InstituteBaserUrl() : this(string.Empty, Guid.Empty) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid InstitutionID { get; private set; } = institutionId;
        public Institution Institution { get; private set; } = null!;
        public string Url { get; private set; } = url ?? throw new ArgumentNullException(nameof(url));
        public List<FhirResourceStatus> ResourceStatuses { get; private set; } = [];
        public VerificationStatus VerificationStatus { get; private set; } = VerificationStatus.Pending;

        public async Task UpdateVerificationStatus(VerificationStatus status)
        {
            VerificationStatus = status;
        }

        public async Task UpdateUrl(string url)
        {
            Url = url;
        }
    }
}

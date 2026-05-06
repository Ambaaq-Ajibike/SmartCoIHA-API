using Domain.Enums;

namespace Domain.Entities
{
    public class Patients(string name, string email, Guid institutionId, string institutePatientId)
    {
        public Patients() : this(string.Empty, string.Empty, Guid.Empty, string.Empty) { }

        public Guid ID { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = name ?? throw new ArgumentNullException(nameof(name));
        public string Email { get; private set; } = email ?? throw new ArgumentNullException(nameof(email));
        public string InstitutePatientId { get; private set; } = institutePatientId;
        public Guid InstitutionID { get; private set; } = institutionId;
        public Institution Institution { get; private set; } = null!;
        public VerificationStatus EnrollmentStatus { get; private set; } = VerificationStatus.Pending;
        public string? FingerPrint { get; private set; }

        public async Task UpdateEmail(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                throw new ArgumentException("Email cannot be empty", nameof(newEmail));

            Email = newEmail;
        }

        public async Task UpdateEnrollmentStatus(VerificationStatus newStatus)
        {
            EnrollmentStatus = newStatus;
        }

        public async Task UpdateFingerPrint(string newFingerPrint)
        {
            if (string.IsNullOrWhiteSpace(newFingerPrint))
                throw new ArgumentException("Fingerprint cannot be empty", nameof(newFingerPrint));
            EnrollmentStatus = VerificationStatus.Verified;
            FingerPrint = newFingerPrint;
        }
    }
}

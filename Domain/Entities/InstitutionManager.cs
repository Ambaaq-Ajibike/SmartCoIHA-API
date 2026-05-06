namespace Domain.Entities
{
    public class InstitutionManager(Guid institutionId, Guid userId)
    {
        private InstitutionManager() : this(Guid.Empty, Guid.Empty) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid InstitutionId { get; private set; } = institutionId;
        public Institution Institution { get; private set; } = null!;
        public Guid UserId { get; private set; } = userId;
        public User User { get; private set; } = null!;

        public bool CanPerformActions()
        {
            return User.IsEmailVerified && Institution.VerificationStatus == Enums.VerificationStatus.Verified;
        }
    }
}

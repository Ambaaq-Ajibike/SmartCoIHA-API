using Domain.Enums;

namespace Domain.Entities
{
    public class User(string email, string fullName, string passwordHash, Role role)
    {
        private User() : this(string.Empty, string.Empty, string.Empty, default) { }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Email { get; private set; } = email ?? throw new ArgumentNullException(nameof(email));
        public string FullName { get; private set; } = fullName ?? throw new ArgumentNullException(nameof(fullName));
        public string PasswordHash { get; private set; } = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        public Role Role { get; private set; } = role;
        public bool IsEmailVerified { get; private set; } = false;
        public string? EmailVerificationToken { get; private set; }
        public DateTime? EmailVerificationTokenExpiry { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; private set; }

        public void SetEmailVerificationToken(string token, DateTime expiry)
        {
            EmailVerificationToken = token;
            EmailVerificationTokenExpiry = expiry;
        }

        public void VerifyEmail()
        {
            IsEmailVerified = true;
            EmailVerificationToken = null;
            EmailVerificationTokenExpiry = null;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public bool IsVerificationTokenValid(string token)
        {
            return EmailVerificationToken == token &&
                   EmailVerificationTokenExpiry.HasValue &&
                   EmailVerificationTokenExpiry.Value > DateTime.UtcNow;
        }
    }
}
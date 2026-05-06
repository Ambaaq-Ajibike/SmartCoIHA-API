using Application.Dtos;
using Application.Dtos.Auth;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services.Implementations
{
    public class AuthService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Institution> institutionRepository,
        IGenericRepository<InstitutionManager> managerRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> _logger) : IAuthService
    {
        public async Task<BaseResponse<AuthResponseDto>> RegisterInstitutionManagerAsync(RegisterInstitutionManagerDto dto)
        {
            _logger.LogInformation("Attempting to register Institution Manager with Email: {Email} for Institution ID: {InstitutionRegistrationId}",
                dto.Email, dto.InstitutionRegistrationId);

            var validator = new RegisterInstitutionManagerValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Registration validation failed for Email: {Email}. Errors: {ValidationErrors}", dto.Email, errors);

                return new BaseResponse<AuthResponseDto>(false, errors, null);
            }

            // Check if user already exists
            var existingUser = await userRepository.GetByExpressionAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (existingUser is not null)
            {
                _logger.LogWarning("Registration failed. User with Email: {Email} already exists.", dto.Email);
                return new BaseResponse<AuthResponseDto>(false, "User with this email already exists", null);
            }

            // Check if institution registration ID already exists
            var existingInstitution = await institutionRepository.GetByExpressionAsync(i => i.RegistrationId == dto.InstitutionRegistrationId);
            if (existingInstitution is not null)
            {
                _logger.LogWarning("Registration failed. Institution with Registration ID: {InstitutionRegistrationId} already exists.", dto.InstitutionRegistrationId);
                return new BaseResponse<AuthResponseDto>(false, "Institution with this registration ID already exists", null);
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create user
            var user = new User(dto.Email, dto.FullName, passwordHash, Role.InstitutionManager);

            // Generate email verification token
            var verificationToken = GenerateVerificationToken();
            var tokenExpiry = DateTime.UtcNow.AddHours(24);
            user.SetEmailVerificationToken(verificationToken, tokenExpiry);

            await userRepository.AddAsync(user);

            // Create institution
            var institution = new Institution(
                dto.InstitutionName,
                dto.InstitutionAddress,
                dto.InstitutionRegistrationId
            );
            await institutionRepository.AddAsync(institution);

            // Create institution manager link
            var manager = new InstitutionManager(institution.Id, user.Id);
            await managerRepository.AddAsync(manager);

            // Send verification email
            _logger.LogInformation("Sending verification email to {Email}", user.Email);
            await SendVerificationEmail(user.Email, user.FullName, verificationToken);

            await managerRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully registered Institution Manager {Email} and Institution {InstitutionName} ({InstitutionId})",
                user.Email, institution.Name, institution.Id);

            return new BaseResponse<AuthResponseDto>
            (
                true,
                "Registration successful. Please check your email to verify your account.",
                new AuthResponseDto(
                    string.Empty,
                    user.Email,
                    user.FullName,
                    user.Role.ToString(),
                    false,
                    false,
                    institution.Id,
                    institution.Name
                )
            );
        }

        public async Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Login attempt for Email: {Email}", dto.Email);

            var validator = new LoginValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Login validation failed for Email: {Email}. Errors: {ValidationErrors}", dto.Email, errors);
                return new BaseResponse<AuthResponseDto>(false, errors, null);
            }

            // Find user
            var user = await userRepository.GetByExpressionAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Login failed. User with Email: {Email} not found.", dto.Email);
                return new BaseResponse<AuthResponseDto>(false, "Invalid email or password", null);
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed. Invalid password for Email: {Email}.", dto.Email);
                return new BaseResponse<AuthResponseDto>(false, "Invalid email or password", null);
            }

            // Check if email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login failed. Email: {Email} is not verified.", dto.Email);
                return new BaseResponse<AuthResponseDto>(false, "Please verify your email before logging in", null);
            }

            // Get institution manager details
            Institution? institution = null;
            bool isInstitutionVerified = false;

            if (user.Role == Role.InstitutionManager)
            {
                var manager = await managerRepository.GetByExpressionAsync(x => x.UserId == user.Id);

                if (manager == null)
                {
                    _logger.LogWarning("Login failed. Institution Manager profile not found for UserId: {UserId}", user.Id);
                    return new BaseResponse<AuthResponseDto>(false, "Institution manager profile not found", null);
                }

                institution = await institutionRepository.GetByIdAsync(manager.InstitutionId);
                isInstitutionVerified = institution.VerificationStatus == VerificationStatus.Verified;

                // Check if institution is verified
                if (!isInstitutionVerified)
                {
                    _logger.LogWarning("Login failed. Institution {InstitutionId} for UserId: {UserId} is pending verification.", institution.Id, user.Id);
                    return new BaseResponse<AuthResponseDto>(false, "Your institution is pending verification. You cannot login until it is approved.", null);
                }
            }

            // Update last login
            user.UpdateLastLogin();
            userRepository.Update(user);
            await userRepository.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user, institution);

            _logger.LogInformation("Login successful for Email: {Email}. Role: {Role}", user.Email, user.Role);

            return new BaseResponse<AuthResponseDto>
            (
                true,
                "Login successful",
                new AuthResponseDto(
                    token,
                    user.Email,
                    user.FullName,
                    user.Role.ToString(),
                    user.IsEmailVerified,
                    isInstitutionVerified,
                    institution?.Id,
                    institution?.Name
                )
            );
        }

        public async Task<BaseResponse<string>> VerifyEmailAsync(VerifyEmailDto dto)
        {
            _logger.LogInformation("Attempting to verify email for: {Email}", dto.Email);

            var user = await userRepository.GetByExpressionAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Email verification failed. User with Email: {Email} not found.", dto.Email);
                return new BaseResponse<string>(false, "User not found", null);
            }

            if (user.IsEmailVerified)
            {
                _logger.LogInformation("Email verification skipped. Email: {Email} is already verified.", dto.Email);
                return new BaseResponse<string>(false, "Email is already verified", null);
            }

            if (!user.IsVerificationTokenValid(dto.Token))
            {
                _logger.LogWarning("Email verification failed. Invalid or expired token for Email: {Email}.", dto.Email);
                return new BaseResponse<string>(false, "Invalid or expired verification token", null);
            }

            user.VerifyEmail();
            userRepository.Update(user);
            await userRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully verified email for: {Email}", dto.Email);

            return new BaseResponse<string>
            (
                true,
                "Email verified successfully. You can now login after your institution is approved.",
                "Email verified"
            );
        }

        public async Task<BaseResponse<string>> ResendVerificationEmailAsync(string email)
        {
            _logger.LogInformation("Attempting to resend verification email for: {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Resend verification email failed. Email was completely empty.");
                return new BaseResponse<string>(false, "Email is required", null);
            }

            var user = await userRepository.GetByExpressionAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Resend verification email failed. User with Email: {Email} not found.", email);
                return new BaseResponse<string>(false, "User not found", null);
            }

            if (user.IsEmailVerified)
            {
                _logger.LogInformation("Resend verification email skipped. Email: {Email} is already verified.", email);
                return new BaseResponse<string>(false, "Email is already verified", null);
            }

            // Generate new verification token
            var verificationToken = GenerateVerificationToken();
            var tokenExpiry = DateTime.UtcNow.AddHours(24);
            user.SetEmailVerificationToken(verificationToken, tokenExpiry);

            userRepository.Update(user);
            await userRepository.SaveChangesAsync();

            // Send verification email
            _logger.LogInformation("Sending new verification email to {Email}", user.Email);
            await SendVerificationEmail(user.Email, user.FullName, verificationToken);

            _logger.LogInformation("Successfully resent verification email to: {Email}", email);

            return new BaseResponse<string>
            (
                true,
                 "Verification email sent successfully",
                "Email sent"
            );
        }

        private string GenerateJwtToken(User user, Institution? institution)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("IsEmailVerified", user.IsEmailVerified.ToString())
            };

            if (institution != null)
            {
                claims.Add(new Claim("InstitutionId", institution.Id.ToString()));
                claims.Add(new Claim("InstitutionName", institution.Name));
                claims.Add(new Claim("IsInstitutionVerified", (institution.VerificationStatus == VerificationStatus.Verified).ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpiryHours"]!)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateVerificationToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task SendVerificationEmail(string email, string fullName, string token)
        {
            var verificationUrl = $"{configuration["AppSettings:FrontendUrl"]}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            var subject = "Verify Your Email - SmartCoIHA";
            var htmlContent = $@"
                <html>
                <body>
                    <h2>Welcome to SmartCoIHA, {fullName}!</h2>
                    <p>Thank you for registering as an Institution Manager.</p>
                    <p>Please verify your email by clicking the link below:</p>
                    <p><a href='{verificationUrl}'>Verify Email</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't register for this account, please ignore this email.</p>
                    <br/>
                    <p>Best regards,<br/>SmartCoIHA Team</p>
                </body>
                </html>";

            await emailService.SendEmailAsync(email, fullName, subject, htmlContent);
        }
        public async Task<List<User>> GetUsersAsync()
        {
            var users = await userRepository.GetAllAsync(x => true);
            return [.. users];
        }
    }
}
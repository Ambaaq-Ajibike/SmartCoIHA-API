using Application.Dtos.Auth;
using FluentValidation;

namespace Application.Validators
{
    public class RegisterInstitutionManagerValidator : AbstractValidator<RegisterInstitutionManagerDto>
    {
        public RegisterInstitutionManagerValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MinimumLength(3).WithMessage("Full name must be at least 3 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.InstitutionName)
                .NotEmpty().WithMessage("Institution name is required")
                .MinimumLength(3).WithMessage("Institution name must be at least 3 characters");

            RuleFor(x => x.InstitutionAddress)
                .NotEmpty().WithMessage("Institution address is required");

            RuleFor(x => x.InstitutionRegistrationId)
                .NotEmpty().WithMessage("Institution registration ID is required");
        }
    }
}
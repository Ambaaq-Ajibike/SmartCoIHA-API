using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class VerifyPatientIdentityValidator : AbstractValidator<VerifyPatientIdentityDto>
    {
        public VerifyPatientIdentityValidator()
        {
            RuleFor(x => x.InstitutePatientId)
                .NotEmpty().WithMessage("Institute Patient ID is required.");

            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("Institution ID is required.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}

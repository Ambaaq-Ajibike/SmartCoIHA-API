using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class PatientLoginValidator : AbstractValidator<PatientLoginDto>
    {
        public PatientLoginValidator()
        {
            RuleFor(x => x.InstitutePatientId)
                .NotEmpty().WithMessage("Institute Patient ID is required.");

            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("Institution ID is required.");

            RuleFor(x => x.FingerprintTemplate)
                .NotEmpty().WithMessage("Fingerprint template is required.")
                .MinimumLength(10).WithMessage("Fingerprint template is too short.");
        }
    }
}

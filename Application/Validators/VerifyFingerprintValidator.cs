using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class VerifyFingerprintValidator : AbstractValidator<VerifyFingerprintDto>
    {
        public VerifyFingerprintValidator()
        {
            RuleFor(x => x.FingerprintTemplate)
                .NotEmpty()
                .WithMessage("Fingerprint template is required.")
                .MinimumLength(10)
                .WithMessage("Fingerprint template appears to be invalid (too short).");
        }
    }
}
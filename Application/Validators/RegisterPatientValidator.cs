using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class RegisterPatientValidator : AbstractValidator<RegisterPatientDto>
    {
        public RegisterPatientValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Patient name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("Institution ID is required.");
        }
    }
}
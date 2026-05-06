namespace Application.Validators
{
    using Application.Dtos;
    using FluentValidation;

    public class RegisterInstitutionValidator : AbstractValidator<RegisterInstitutionDto>
    {
        public RegisterInstitutionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Institution name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Physical address is required.");

            RuleFor(x => x.RegistrationId)
                .NotEmpty().WithMessage("Physical address is required.");

        }
    }
}

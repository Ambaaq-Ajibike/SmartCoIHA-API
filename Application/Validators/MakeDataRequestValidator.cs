using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class MakeDataRequestValidator : AbstractValidator<MakeDataRequestDto>
    {
        public MakeDataRequestValidator()
        {
            RuleFor(x => x.RequestingInstitutionId)
                .NotEmpty()
                .WithMessage("Requesting institution ID is required.");

            RuleFor(x => x.InstitutePatientId)
                .NotEmpty()
                .WithMessage("Patient ID is required.")
                .WithMessage("Patient ID must be a valid GUID format.");

            RuleFor(x => x.ResourceType)
                .NotEmpty()
                .WithMessage("Resource type is required.")
                .MaximumLength(100)
                .WithMessage("Resource type cannot exceed 100 characters.")
                .Must(BeValidResourceType)
                .WithMessage("Resource type must be a valid FHIR resource type (e.g., Patient, Observation, Condition, MedicationRequest, etc.).");
        }

        private static bool BeValidResourceType(string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return false;

            // Common FHIR resource types
            var validResourceTypes = new[]
            {
                "Patient", "Observation", "Condition", "MedicationRequest",
                "DiagnosticReport", "Procedure", "Encounter", "AllergyIntolerance",
                "Immunization", "CarePlan", "Goal", "DocumentReference"
            };

            return validResourceTypes.Contains(resourceType, StringComparer.OrdinalIgnoreCase);
        }
    }
}
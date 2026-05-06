using Application.Dtos;
using FluentValidation;

namespace Application.Validators
{
    public class AddEndpointValidator : AbstractValidator<AddEndPointRequestDto>
    {
        public AddEndpointValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .WithMessage("FHIR endpoint URL is required.")
                .Must(BeValidUrl)
                .WithMessage("FHIR endpoint URL must be a valid HTTP/HTTPS URL.");

            RuleFor(x => x.SupportedResources)
                .NotEmpty()
                .WithMessage("At least one supported resource must be specified.")
                .Must(resources => resources != null && resources.Count > 0)
                .WithMessage("Supported resources list cannot be empty.");

            RuleForEach(x => x.SupportedResources)
                .NotEmpty()
                .WithMessage("Resource name cannot be empty.")
                .Must(BeValidFhirResource)
                .WithMessage("Invalid FHIR resource type. Must be a valid FHIR R4 resource.");

            RuleFor(x => x.TestingPatientId)
                .NotEmpty()
                .WithMessage("Testing patient ID is required for endpoint validation.");
        }

        private static bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static bool BeValidFhirResource(string resourceType)
        {
            var validResources = new[]
            {
                "Patient", "Observation", "Condition", "MedicationRequest",
                "DiagnosticReport", "Procedure", "Encounter", "AllergyIntolerance",
                "Immunization", "CarePlan", "Goal", "DocumentReference",
                "Practitioner", "Organization", "Location", "Device"
            };

            return validResources.Contains(resourceType, StringComparer.OrdinalIgnoreCase);
        }
    }
}
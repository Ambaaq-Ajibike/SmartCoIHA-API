using Application.Repositories.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class FhirValidationService(
        IGenericRepository<InstituteBaserUrl> _endpointRepository,
        IGenericRepository<FhirResourceStatus> _resourceStatusRepository,
        ILogger<FhirValidationService> _logger)
    {
        public async System.Threading.Tasks.Task ValidateEndpointAsync(
            Guid endpointId,
            string baseUrl,
            List<string> supportedResources,
            Guid testingPatientId)
        {
            _logger.LogInformation("Starting FHIR endpoint validation. EndpointId: {EndpointId}, BaseUrl: {BaseUrl}, PatientId: {TestingPatientId}",
                endpointId, baseUrl, testingPatientId);

            var endpoint = await _endpointRepository.GetByIdAsync(endpointId);
            if (endpoint == null)
            {
                _logger.LogWarning("Validation aborted. Endpoint {EndpointId} not found.", endpointId);
                return;
            }

            var allResourcesValid = true;

            foreach (var resourceName in supportedResources)
            {
                _logger.LogInformation("Validating resource: {ResourceName} against EndpointId: {EndpointId}", resourceName, endpointId);

                var isValid = await ValidateResourceAsync(
                    endpointId,
                    baseUrl,
                    resourceName,
                    testingPatientId.ToString());

                if (!isValid)
                {
                    _logger.LogWarning("Resource validation failed for {ResourceName}. EndpointId: {EndpointId}", resourceName, endpointId);
                    allResourcesValid = false;
                }
            }

            // Update overall endpoint verification status
            var finalStatus = allResourcesValid ? VerificationStatus.Verified : VerificationStatus.Failed;
            await endpoint.UpdateVerificationStatus(finalStatus);

            _endpointRepository.Update(endpoint);
            await _endpointRepository.SaveChangesAsync();

            _logger.LogInformation("Endpoint {EndpointId} validation completed. Final Status: {FinalStatus}", endpointId, finalStatus);
        }

        private async Task<bool> ValidateResourceAsync(
            Guid endpointId,
            string baseUrl,
            string resourceName,
            string patientId)
        {
            try
            {
                // Get the resource status record
                var resourceStatus = await _resourceStatusRepository.GetByExpressionAsync(
                    r => r.InstituteBaseUrlId == endpointId && r.ResourceName == resourceName);

                if (resourceStatus == null)
                {
                    _logger.LogWarning("Resource status tracking record not found for {ResourceName} on EndpointId: {EndpointId}", resourceName, endpointId);
                    return false;
                }

                // Create FHIR client with timeout settings
                var settings = new FhirClientSettings
                {
                    Timeout = 30000, // 30 seconds
                    PreferredFormat = ResourceFormat.Json,
                    VerifyFhirVersion = false
                };

                var client = new FhirClient(baseUrl, settings);

                // Validate based on resource type
                bool isValid = resourceName.ToLower() switch
                {
                    "patient" => await ValidatePatientResourceAsync(client, patientId),
                    "observation" => await ValidateObservationResourceAsync(client, patientId),
                    "condition" => await ValidateConditionResourceAsync(client, patientId),
                    "medicationrequest" => await ValidateMedicationRequestResourceAsync(client, patientId),
                    "diagnosticreport" => await ValidateDiagnosticReportResourceAsync(client, patientId),
                    "procedure" => await ValidateProcedureResourceAsync(client, patientId),
                    "encounter" => await ValidateEncounterResourceAsync(client, patientId),
                    "allergyintolerance" => await ValidateAllergyIntoleranceResourceAsync(client, patientId),
                    "immunization" => await ValidateImmunizationResourceAsync(client, patientId),
                    _ => await ValidateGenericResourceAsync(client, resourceName, patientId)
                };

                if (isValid)
                {
                    resourceStatus.MarkVerified();
                    _logger.LogInformation("Successfully validated resource type: {ResourceName} for EndpointId: {EndpointId}", resourceName, endpointId);
                }
                else
                {
                    resourceStatus.MarkFailed("Resource validation failed: No valid data returned");
                    _logger.LogWarning("Validation returned no valid data for resource type: {ResourceName} on EndpointId: {EndpointId}", resourceName, endpointId);
                }

                _resourceStatusRepository.Update(resourceStatus);
                await _resourceStatusRepository.SaveChangesAsync();

                return isValid;
            }
            catch (FhirOperationException ex)
            {
                _logger.LogError(ex, "FHIR operation error while validating {ResourceName} for EndpointId: {EndpointId}. Message: {ErrorMessage}",
                    resourceName, endpointId, ex.Message);
                await MarkResourceFailed(endpointId, resourceName, $"FHIR error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System error while validating {ResourceName} for EndpointId: {EndpointId}. Message: {ErrorMessage}",
                    resourceName, endpointId, ex.Message);
                await MarkResourceFailed(endpointId, resourceName, $"Error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ValidatePatientResourceAsync(FhirClient client, string patientId)
        {
            var patient = await client.ReadAsync<Patient>($"Patient/{patientId}");
            return patient != null && !string.IsNullOrEmpty(patient.Id);
        }

        private static async Task<bool> ValidateObservationResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Observation>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateConditionResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Condition>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateMedicationRequestResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<MedicationRequest>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateDiagnosticReportResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<DiagnosticReport>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateProcedureResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Procedure>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateEncounterResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Encounter>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateAllergyIntoleranceResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<AllergyIntolerance>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateImmunizationResourceAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Immunization>([$"patient={patientId}"]);
            return bundle?.Entry != null;
        }

        private static async Task<bool> ValidateGenericResourceAsync(FhirClient client, string resourceName, string patientId)
        {
            // Generic validation using raw REST API
            var url = $"{resourceName}?patient={patientId}&_count=1";
            var bundle = await client.GetAsync(url);
            return bundle != null;
        }

        private async System.Threading.Tasks.Task MarkResourceFailed(Guid endpointId, string resourceName, string errorMessage)
        {
            var resourceStatus = await _resourceStatusRepository.GetByExpressionAsync(
                r => r.InstituteBaseUrlId == endpointId && r.ResourceName == resourceName);

            if (resourceStatus != null)
            {
                resourceStatus.MarkFailed(errorMessage);
                _resourceStatusRepository.Update(resourceStatus);
                await _resourceStatusRepository.SaveChangesAsync();

                _logger.LogWarning("Marked resource {ResourceName} as Failed for EndpointId: {EndpointId}. Reason: {ErrorMessage}",
                    resourceName, endpointId, errorMessage);
            }
        }
    }
}
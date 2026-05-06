using Application.Dtos;
using Application.Messaging.Interfaces;
using Application.Messaging.Models;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Validators;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class FHIREndpointService(
        IGenericRepository<InstituteBaserUrl> _endpointRepository,
        IGenericRepository<Institution> _institutionRepository,
        IGenericRepository<FhirResourceStatus> _resourceStatusRepository,
        IMessagePublisher _messagePublisher,
        ILogger<FHIREndpointService> _logger) : IFHIREndpointService
    {
        public async Task<BaseResponse<Guid>> AddEndpointAsync(AddEndPointRequestDto request)
        {
            _logger.LogInformation("Attempting to Add/Update FHIR Endpoint. InstitutionId: {InstitutionId}, Url: {Url}", request.InstitutionId, request.Url);

            // Validate request
            var validator = new AddEndpointValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Endpoint validation failed for InstitutionId: {InstitutionId}. Errors: {ValidationErrors}", request.InstitutionId, errors);
                return new BaseResponse<Guid>(false, errors, Guid.Empty);
            }

            // Check if endpoint already exists for this institution
            var institution = await _institutionRepository.GetByIdAsync(request.InstitutionId);

            if (institution is null)
            {
                _logger.LogWarning("Endpoint upsert failed. InstitutionId: {InstitutionId} not found.", request.InstitutionId);
                return new BaseResponse<Guid>(
                    false,
                    "Institution not found.",
                    Guid.Empty);
            }

            var existingEndpoint = await _endpointRepository.GetByExpressionAsync(e => e.InstitutionID == request.InstitutionId);

            // Normalize URL (remove trailing slash)
            var normalizedUrl = request.Url.TrimEnd('/');
            var endpointId = Guid.NewGuid();

            if (existingEndpoint is not null)
            {
                _logger.LogInformation("Existing FHIR Endpoint found for InstitutionId: {InstitutionId}. Updating EndpointId: {EndpointId}", request.InstitutionId, existingEndpoint.Id);
                endpointId = existingEndpoint.Id;

                // Remove all existing resource status entries for this endpoint FIRST
                var existingResourceStatuses = await _resourceStatusRepository.GetAllAsync(r => r.InstituteBaseUrlId == endpointId);

                _logger.LogInformation("Deleting {ResourceStatusCount} existing FHIR Resource Status entries for EndpointId: {EndpointId}", existingResourceStatuses.Count, endpointId);

                foreach (var resourceStatus in existingResourceStatuses)
                {
                    _resourceStatusRepository.Delete(resourceStatus);
                }

                // Update endpoint URL
                await existingEndpoint.UpdateUrl(normalizedUrl);
                _endpointRepository.Update(existingEndpoint);

                // Save both deletions and endpoint update together
                await _endpointRepository.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("No existing FHIR Endpoint found for InstitutionId: {InstitutionId}. Creating new endpoint.", request.InstitutionId);

                // Create new endpoint
                var endpoint = new InstituteBaserUrl(normalizedUrl, request.InstitutionId);
                var createdEndpoint = await _endpointRepository.AddAsync(endpoint);
                endpointId = createdEndpoint.Id;

                // Save the new endpoint so it exists before adding resource statuses
                await _endpointRepository.SaveChangesAsync();

                _logger.LogInformation("Successfully created new FHIR Endpoint: {EndpointId} for InstitutionId: {InstitutionId}", endpointId, request.InstitutionId);
            }

            // Create new resource status entries
            var resourcesToAdd = request.SupportedResources.Distinct().ToList();
            _logger.LogInformation("Adding {ResourceCount} initial FHIR Resource Status entries for EndpointId: {EndpointId}", resourcesToAdd.Count, endpointId);

            foreach (var resourceName in resourcesToAdd)
            {
                var resourceStatus = new FhirResourceStatus(resourceName, endpointId);
                await _resourceStatusRepository.AddAsync(resourceStatus);
            }

            // Save the new resource statuses
            await _resourceStatusRepository.SaveChangesAsync();

            // Enqueue validation message to RabbitMQ
            var validationMessage = new FhirEndpointValidationMessage
            {
                EndpointId = endpointId,
                BaseUrl = normalizedUrl,
                SupportedResources = resourcesToAdd,
                TestingPatientId = request.TestingPatientId
            };

            _logger.LogInformation("Publishing FHIR Endpoint Validation Message to RabbitMQ for EndpointId: {EndpointId}. Queue: fhir-validation-queue", endpointId);
            await _messagePublisher.PublishAsync(validationMessage, "fhir-validation-queue");

            return new BaseResponse<Guid>(
                true,
                "FHIR endpoint upserted successfully. Validation is in progress.",
                endpointId);
        }

        public async Task<BaseResponse<FHIREndpointDto>> GetEndpointByInstitutionIdAsync(Guid institutionId)
        {
            _logger.LogInformation("Attempting to get FHIR Endpoint for InstitutionId: {InstitutionId}", institutionId);

            // Validate institution exists
            var institution = await _institutionRepository.GetByIdAsync(institutionId);
            if (institution == null)
            {
                _logger.LogWarning("Failed to retrieve FHIR Endpoint. InstitutionId: {InstitutionId} not found.", institutionId);
                return new BaseResponse<FHIREndpointDto>(
                    false,
                    "Institution not found.",
                    null!);
            }

            // Get endpoint for institution
            var endpoint = await _endpointRepository.GetByExpressionAsync(e => e.InstitutionID == institutionId);

            if (endpoint is null)
            {
                _logger.LogInformation("No FHIR endpoint found for InstitutionId: {InstitutionId}", institutionId);
                return new BaseResponse<FHIREndpointDto>(
                    false,
                    "No FHIR endpoint found for this institution.",
                    null!);
            }

            // Get resource statuses
            var resourceStatuses = await _resourceStatusRepository.GetAllAsync(r => r.InstituteBaseUrlId == endpoint.Id);

            _logger.LogInformation("Successfully retrieved FHIR Endpoint: {EndpointId} with {ResourceCount} resource statuses for InstitutionId: {InstitutionId}",
                endpoint.Id, resourceStatuses.Count, institutionId);

            var resourceDtos = resourceStatuses.Select(r => new ResourceStatusDto(
                r.ResourceName,
                r.IsVerified,
                r.ErrorMessage)).ToList();

            var endpointDto = new FHIREndpointDto(
                endpoint.Id,
                endpoint.Url,
                resourceDtos);

            return new BaseResponse<FHIREndpointDto>(
                true,
                "FHIR endpoint retrieved successfully.",
                endpointDto);
        }
    }
}

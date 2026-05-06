using Application.Dtos;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class InstitutionService(
        IGenericRepository<Institution> _institutionRepository,
        ILogger<InstitutionService> _logger) : IInstitutionService
    {

        public async Task<BaseResponse<InstitutionDto>> GetInstitutionByIdAsync(Guid id)
        {
            _logger.LogInformation("Attempting to retrieve institution with ID: {InstitutionId}", id);

            var institution = await _institutionRepository.GetByIdAsync(id);

            if (institution == null)
            {
                _logger.LogWarning("Institution retrieval failed. Institution with ID: {InstitutionId} was not found.", id);
                return new BaseResponse<InstitutionDto>(false, $"Institution with ID {id} not found.", null!);
            }

            var institutionDto = new InstitutionDto(
                institution.Id,
                institution.Name,
                institution.Address,
                institution.RegistrationId,
                institution.VerificationStatus.ToString());

            _logger.LogInformation("Successfully retrieved institution: {InstitutionName} ({InstitutionId})", institution.Name, institution.Id);
            return new BaseResponse<InstitutionDto>(true, "Institution retrieved successfully.", institutionDto);
        }

        public async Task<BaseResponse<IEnumerable<InstitutionDto>>> GetAllInstitutionsAsync()
        {
            _logger.LogInformation("Attempting to retrieve all institutions.");

            var institutions = await _institutionRepository.GetAllAsync(i => true);

            if (!institutions.Any())
            {
                _logger.LogInformation("No institutions found in the database.");
                return new BaseResponse<IEnumerable<InstitutionDto>>(
                    true,
                    "No institutions found.",
                    []);
            }

            var institutionDtos = institutions.Select(institution => new InstitutionDto(
                institution.Id,
                institution.Name,
                institution.Address,
                institution.RegistrationId,
                institution.VerificationStatus.ToString()));

            _logger.LogInformation("Successfully retrieved {InstitutionCount} institutions.", institutions.Count);
            return new BaseResponse<IEnumerable<InstitutionDto>>(
                true,
                "Institutions retrieved successfully.",
                institutionDtos);
        }
        public async Task<BaseResponse<IEnumerable<GetInstitutionDto>>> GetAllVerifiedInstitutionsAsync()
        {
            _logger.LogInformation("Attempting to retrieve all institutions.");

            var institutions = await _institutionRepository.GetAllAsync(i => i.VerificationStatus == VerificationStatus.Verified);

            if (!institutions.Any())
            {
                _logger.LogInformation("No institutions found in the database.");
                return new BaseResponse<IEnumerable<GetInstitutionDto>>(
                    true,
                    "No institutions found.",
                    []);
            }

            var institutionDtos = institutions.Select(institution => new GetInstitutionDto(
                institution.Id,
                institution.Name));

            _logger.LogInformation("Successfully retrieved {InstitutionCount} institutions.", institutions.Count);
            return new BaseResponse<IEnumerable<GetInstitutionDto>>(
                true,
                "Institutions retrieved successfully.",
                institutionDtos);
        }

        public async Task<BaseResponse<bool>> UpdateInstitutionStatusAsync(Guid id, VerificationStatus status)
        {
            _logger.LogInformation("Attempting to update status for Institution ID: {InstitutionId} to {VerificationStatus}", id, status);

            var institution = await _institutionRepository.GetByIdAsync(id);
            if (institution == null)
            {
                _logger.LogWarning("Failed to update status. Institution ID: {InstitutionId} was not found.", id);
                return new BaseResponse<bool>(false, $"Institution not found.", false);
            }

            var oldStatus = institution.VerificationStatus;
            institution.UpdateVerificationStatus(status);

            _institutionRepository.Update(institution);
            await _institutionRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully updated status for Institution ID: {InstitutionId} from {OldStatus} to {NewStatus}",
                id, oldStatus, status);

            return new BaseResponse<bool>(true, "Institution status updated successfully.", true);
        }
    }
}

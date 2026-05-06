using API.Attributes;
using Application.Dtos;
using Application.Services.Interfaces;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class DataRequestController(IDataRequestService _dataRequestService) : ControllerBase
    {
        [HttpPost]
        [RequireInstitutionManager]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MakeDataRequest([FromBody] MakeDataRequestDto dto)
        {
            var response = await _dataRequestService.MakeDataRequestAsync(dto);

            return response.Success
                ? Ok(response)
                 : BadRequest(response);
        }

        [HttpGet("institution/{institutionId}/outgoing")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<DataRequestDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<DataRequestDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOutgoingDataRequests(Guid institutionId)
        {
            var response = await _dataRequestService.GetOutgoingDataRequestsAsync(institutionId);

            return response.Success
                ? Ok(response)
                : NotFound(response);
        }

        [HttpGet("institution/{institutionId}/incoming")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<DataRequestDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<DataRequestDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetIncomingDataRequests(Guid institutionId)
        {
            var response = await _dataRequestService.GetIncomingDataRequestsAsync(institutionId);

            return response.Success
                ? Ok(response)
                : NotFound(response);
        }

        [HttpPut("{requestId}/approval-status")]
        [RequireInstitutionManager]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateApprovalStatus(
            Guid requestId,
            [FromBody] UpdateApprovalStatusDto dto)
        {
            var response = await _dataRequestService.UpdateInstitutionApprovalStatusAsync(requestId, dto.Status);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPost("{requestId}/verify-fingerprint/{institutePatientId}")]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyPatientFingerprint(
            Guid requestId,
            string institutePatientId,
            [FromBody] VerifyFingerprintDto dto)
        {
            var response = await _dataRequestService.VerifyPatientFingerprintAsync(
                requestId,
                institutePatientId,
                dto.FingerprintTemplate);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpGet("{requestId}/resource-data")]
        [ProducesResponseType(typeof(BaseResponse<Resource>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<Resource>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Resource>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPatientResourceData(Guid requestId)
        {
            var response = await _dataRequestService.GetPatientResourceDataAsync(requestId);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }
    }
}
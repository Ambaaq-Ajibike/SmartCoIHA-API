using API.Attributes;
using Application.Dtos;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/patient-mobile")]
    [Produces("application/json")]
    public class PatientMobileController(
        IPatientMobileService _patientMobileService,
        INotificationService _notificationService) : ControllerBase
    {
        [HttpPost("verify-identity")]
        [ProducesResponseType(typeof(BaseResponse<PatientAuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PatientAuthResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyIdentity([FromBody] VerifyPatientIdentityDto dto)
        {
            var response = await _patientMobileService.VerifyIdentityAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(BaseResponse<PatientAuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PatientAuthResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] PatientLoginDto dto)
        {
            var response = await _patientMobileService.PatientLoginAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("profile")]
        [RequirePatient]
        [ProducesResponseType(typeof(BaseResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PatientDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            var patientId = (Guid)HttpContext.Items["PatientId"]!;
            var institutionId = (Guid)HttpContext.Items["InstitutionId"]!;
            var response = await _patientMobileService.GetPatientProfileAsync(patientId, institutionId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        [HttpGet("data-requests/history")]
        [RequirePatient]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<PatientDataRequestHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDataRequestHistory()
        {
            var patientId = (Guid)HttpContext.Items["PatientId"]!;
            var institutionId = (Guid)HttpContext.Items["InstitutionId"]!;
            var response = await _patientMobileService.GetDataRequestHistoryAsync(patientId, institutionId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("device-token")]
        [RequirePatient]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto)
        {
            var patientId = (Guid)HttpContext.Items["PatientId"]!;
            var response = await _notificationService.RegisterDeviceTokenAsync(patientId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}

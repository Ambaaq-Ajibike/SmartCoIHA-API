using Application.Dtos;
using Application.Services.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class PatientsController(IPatientService _patientService) : ControllerBase
    {

        [HttpPost("register")]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterPatientDto dto)
        {
            var response = await _patientService.RegsiterPatientAsync(dto);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }


        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<PatientDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _patientService.GetPatientByIdAsync(id);

            return response.Success
                ? Ok(response)
                : NotFound(response);
        }
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPatients(
            [FromQuery] string? institutionId = null,
            [FromQuery] VerificationStatus? enrollmentStatus = null)
        {
            var response = await _patientService.GetPatientsAsync(institutionId, enrollmentStatus);
            return Ok(response);
        }

        [HttpPost("fingerprint")]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddFingerprint([FromBody] AddFingerprintDto dto)
        {
            var response = await _patientService.AddFingerprintAsync(dto.PatientId, dto.FingerprintTemplate);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPost("bulk-upload/{institutionId}")]
        [ProducesResponseType(typeof(BaseResponse<BulkUploadResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<BulkUploadResultDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
        public async Task<IActionResult> BulkUpload(IFormFile file, Guid institutionId)
        {
            var response = await _patientService.BulkUploadPatientsAsync(file, institutionId);
            return Ok(response);
        }

        [HttpGet("bulk-upload-template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("text/csv")]
        public IActionResult DownloadBulkUploadTemplate()
        {
            var csv = new StringBuilder();

            // Add headers
            csv.AppendLine("ID,Name,Email");

            // Add sample rows (optional - helps users understand the format)
            csv.AppendLine("1,John Doe,john.doe@example.com");
            csv.AppendLine("2,Jane Smith,jane.smith@example.com");
            csv.AppendLine("3,Michael Johnson,michael.johnson@example.com");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"patient_bulk_upload_template_{DateTime.UtcNow:yyyyMMdd}.csv";

            return File(bytes, "text/csv", fileName);
        }
    }
}

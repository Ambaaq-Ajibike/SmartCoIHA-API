using Application.Dtos;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FHIREndpointController(IFHIREndpointService _fhirEndpointService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddEndpoint([FromBody] AddEndPointRequestDto dto)
        {
            var response = await _fhirEndpointService.AddEndpointAsync(dto);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpGet("institution/{institutionId}")]
        [ProducesResponseType(typeof(BaseResponse<FHIREndpointDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<FHIREndpointDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEndpointByInstitution(Guid institutionId)
        {
            var response = await _fhirEndpointService.GetEndpointByInstitutionIdAsync(institutionId);

            return response.Success
                ? Ok(response)
                : NotFound(response);
        }
    }
}
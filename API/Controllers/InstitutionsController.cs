using Application.Dtos;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InstitutionsController(IInstitutionService _institutionService) : ControllerBase
    {
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<InstitutionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _institutionService.GetInstitutionByIdAsync(id);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<InstitutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            var response = await _institutionService.GetAllInstitutionsAsync();
            return Ok(response);
        }

        [HttpGet("verified")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<GetInstitutionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVerified()
        {
            var response = await _institutionService.GetAllVerifiedInstitutionsAsync();
            return Ok(response);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInstitutionStatusDto dto)
        {
            var response = await _institutionService.UpdateInstitutionStatusAsync(id, dto.Status);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}

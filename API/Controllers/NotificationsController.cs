using API.Attributes;
using Application.Dtos;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NotificationsController(INotificationService _notificationService) : ControllerBase
    {
        [HttpGet("patient")]
        [RequirePatient]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<PatientNotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPatientNotifications()
        {
            var patientId = (Guid)HttpContext.Items["PatientId"]!;
            var response = await _notificationService.GetPatientNotificationsAsync(patientId);
            return Ok(response);
        }

        [HttpPut("{id}/read")]
        [RequirePatient]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var patientId = (Guid)HttpContext.Items["PatientId"]!;
            var response = await _notificationService.MarkAsReadAsync(patientId, id);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}

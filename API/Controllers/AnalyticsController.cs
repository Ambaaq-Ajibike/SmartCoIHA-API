using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
    {
        /// <summary>
        /// Retrieves complete analytics for the Admin (KPIs + Graphs)
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var response = await analyticsService.GetAdminDashboardAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves complete analytics for a specific Institution (KPIs + Graphs)
        /// </summary>
        [HttpGet("institution/{institutionId}")]
        [Authorize(Roles = "InstitutionManager")]
        public async Task<IActionResult> GetInstitutionDashboard(Guid institutionId)
        {
            var response = await analyticsService.GetInstitutionDashboardAsync(institutionId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
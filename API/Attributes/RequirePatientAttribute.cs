using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace API.Attributes
{
    public class RequirePatientAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "You must be logged in to access this resource."
                });
                return;
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Patient")
            {
                context.Result = new ForbidResult();
                return;
            }

            var patientId = user.FindFirst("PatientId")?.Value;
            var institutionId = user.FindFirst("InstitutionId")?.Value;
            if (string.IsNullOrWhiteSpace(patientId) || string.IsNullOrWhiteSpace(institutionId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "Invalid patient token."
                });
                return;
            }

            if (!Guid.TryParse(patientId, out var parsedPatientId) ||
                !Guid.TryParse(institutionId, out var parsedInstitutionId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "Invalid patient token."
                });
                return;
            }

            context.HttpContext.Items["PatientId"] = parsedPatientId;
            context.HttpContext.Items["InstitutionId"] = parsedInstitutionId;
        }
    }
}

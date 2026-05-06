using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace API.Attributes
{
    public class RequireInstitutionManagerAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "You must be logged in to access this resource"
                });
                return;
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "InstitutionManager")
            {
                context.Result = new ForbidResult();
                return;
            }

            var isEmailVerified = bool.Parse(user.FindFirst("IsEmailVerified")?.Value ?? "false");
            var isInstitutionVerified = bool.Parse(user.FindFirst("IsInstitutionVerified")?.Value ?? "false");

            if (!isEmailVerified || !isInstitutionVerified)
            {
                context.Result = new ObjectResult(new
                {
                    Success = false,
                    Message = "Your email or institution is not verified. Please complete verification before accessing this resource."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
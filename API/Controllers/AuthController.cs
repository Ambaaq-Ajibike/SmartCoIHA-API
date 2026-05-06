using Application.Dtos;
using Application.Dtos.Auth;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("users")]
        public async Task<ActionResult<BaseResponse<AuthResponseDto>>> GetUsers()
        {

            var result = await authService.GetUsersAsync();
            return Ok(result);
        }
        [HttpPost("register-manager")]
        public async Task<ActionResult<BaseResponse<AuthResponseDto>>> RegisterManager([FromBody] RegisterInstitutionManagerDto dto)
        {

            var result = await authService.RegisterInstitutionManagerAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<BaseResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
        {
            var result = await authService.LoginAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult<BaseResponse<string>>> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            var result = await authService.VerifyEmailAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("resend-verification")]
        public async Task<ActionResult<BaseResponse<string>>> ResendVerification([FromQuery] string email)
        {
            var result = await authService.ResendVerificationEmailAsync(email);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
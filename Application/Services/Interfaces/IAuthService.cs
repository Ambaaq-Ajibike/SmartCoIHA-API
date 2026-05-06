using Application.Dtos;
using Application.Dtos.Auth;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse<AuthResponseDto>> RegisterInstitutionManagerAsync(RegisterInstitutionManagerDto dto);
        Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<BaseResponse<string>> VerifyEmailAsync(VerifyEmailDto dto);
        Task<BaseResponse<string>> ResendVerificationEmailAsync(string email);
        Task<List<User>> GetUsersAsync();
    }
}
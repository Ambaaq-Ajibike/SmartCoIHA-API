namespace Application.Dtos
{
    public record BaseResponse<T>(bool Success, string Message, T Data);
}

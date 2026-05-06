using Application.Dtos;
using Domain.Enums;

namespace Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(Guid patientId, string title, string message, NotificationType type, Guid? dataRequestId = null);
        Task<BaseResponse<IEnumerable<PatientNotificationDto>>> GetPatientNotificationsAsync(Guid patientId);
        Task<BaseResponse<bool>> MarkAsReadAsync(Guid patientId, Guid notificationId);
        Task<BaseResponse<bool>> RegisterDeviceTokenAsync(Guid patientId, RegisterDeviceTokenDto dto);
    }
}

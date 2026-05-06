using Application.Dtos;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class NotificationService(
        IGenericRepository<Notification> _notificationRepository,
        IGenericRepository<DeviceToken> _deviceTokenRepository,
        IPushNotificationService _pushService,
        ILogger<NotificationService> _logger) : INotificationService
    {
        public async Task CreateNotificationAsync(Guid patientId, string title, string message, NotificationType type, Guid? dataRequestId = null)
        {
            _logger.LogInformation("Creating notification for patient {PatientId}: {Title}", patientId, title);

            var notification = new Notification(title, message, type, patientId, dataRequestId);
            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            // Send FCM push notification
            var data = new Dictionary<string, string>
            {
                { "notificationId", notification.Id.ToString() },
                { "type", type.ToString() }
            };

            if (dataRequestId.HasValue)
            {
                data["dataRequestId"] = dataRequestId.Value.ToString();
            }

            var sentCount = await _pushService.SendPushToPatientAsync(patientId, title, message, data);
            _logger.LogInformation("Notification created and {SentCount} push notification(s) sent for patient {PatientId}", sentCount, patientId);
        }

        public async Task<BaseResponse<IEnumerable<PatientNotificationDto>>> GetPatientNotificationsAsync(Guid patientId)
        {
            _logger.LogInformation("Retrieving notifications for patient {PatientId}", patientId);

            var notifications = await _notificationRepository.GetAllAsync(n => n.PatientId == patientId);

            var dtos = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PatientNotificationDto(
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type.ToString(),
                    n.IsRead,
                    n.CreatedAt,
                    n.DataRequestId))
                .ToList();

            _logger.LogInformation("Retrieved {Count} notification(s) for patient {PatientId}", dtos.Count, patientId);

            return new BaseResponse<IEnumerable<PatientNotificationDto>>(
                true,
                $"{dtos.Count} notification(s) retrieved.",
                dtos);
        }

        public async Task<BaseResponse<bool>> MarkAsReadAsync(Guid patientId, Guid notificationId)
        {
            _logger.LogInformation("Marking notification {NotificationId} as read for patient {PatientId}", notificationId, patientId);

            var notification = await _notificationRepository.GetByExpressionAsync(
                n => n.Id == notificationId && n.PatientId == patientId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for patient {PatientId}", notificationId, patientId);
                return new BaseResponse<bool>(false, "Notification not found.", false);
            }

            notification.MarkAsRead();
            _notificationRepository.Update(notification);
            await _notificationRepository.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} marked as read for patient {PatientId}", notificationId, patientId);
            return new BaseResponse<bool>(true, "Notification marked as read.", true);
        }

        public async Task<BaseResponse<bool>> RegisterDeviceTokenAsync(Guid patientId, RegisterDeviceTokenDto dto)
        {
            _logger.LogInformation("Registering device token for patient {PatientId}, platform: {Platform}", patientId, dto.Platform);

            if (string.IsNullOrWhiteSpace(dto.DeviceToken))
            {
                return new BaseResponse<bool>(false, "Device token is required.", false);
            }

            if (string.IsNullOrWhiteSpace(dto.Platform))
            {
                return new BaseResponse<bool>(false, "Platform is required.", false);
            }

            var existingToken = await _deviceTokenRepository.GetByExpressionAsync(
                dt => dt.PatientId == patientId && dt.Platform == dto.Platform);

            if (existingToken != null)
            {
                existingToken.UpdateToken(dto.DeviceToken);
                _deviceTokenRepository.Update(existingToken);
            }
            else
            {
                var deviceToken = new DeviceToken(patientId, dto.DeviceToken, dto.Platform);
                await _deviceTokenRepository.AddAsync(deviceToken);
            }

            await _deviceTokenRepository.SaveChangesAsync();

            _logger.LogInformation("Device token registered successfully for patient {PatientId}", patientId);
            return new BaseResponse<bool>(true, "Device token registered successfully.", true);
        }
    }
}

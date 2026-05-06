using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence.PushNotifications
{
    public class FcmPushNotificationService : IPushNotificationService
    {
        private readonly IGenericRepository<DeviceToken> _deviceTokenRepository;
        private readonly ILogger<FcmPushNotificationService> _logger;
        private readonly bool _isConfigured;

        public FcmPushNotificationService(
            IGenericRepository<DeviceToken> deviceTokenRepository,
            IConfiguration configuration,
            ILogger<FcmPushNotificationService> logger)
        {
            _deviceTokenRepository = deviceTokenRepository;
            _logger = logger;

            // Initialize Firebase if not already initialized and credentials exist
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = configuration["Firebase:ServiceAccountKeyPath"];
                if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                    _isConfigured = true;
                    _logger.LogInformation("Firebase Admin SDK initialized from {CredentialPath}", credentialPath);
                }
                else
                {
                    _isConfigured = false;
                    _logger.LogWarning(
                        "Firebase service account key not found at '{CredentialPath}'. Push notifications will be logged but not sent. " +
                        "Set Firebase:ServiceAccountKeyPath in appsettings.json to enable push notifications.",
                        credentialPath);
                }
            }
            else
            {
                _isConfigured = true;
            }
        }

        public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (!_isConfigured)
            {
                _logger.LogInformation("[FCM Dry Run] Would send push to token {Token}: {Title} - {Body}",
                    deviceToken[..Math.Min(20, deviceToken.Length)] + "...", title, body);
                return true;
            }

            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "smartcoiha_notifications"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    }
                };

                if (data != null)
                {
                    message.Data = data;
                }

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("FCM push sent successfully. Message ID: {MessageId}", response);
                return true;
            }
            catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                _logger.LogWarning("Device token is unregistered/expired: {Token}. Removing from database.", deviceToken[..Math.Min(20, deviceToken.Length)]);
                // Token is invalid — clean it up
                var tokenEntity = await _deviceTokenRepository.GetByExpressionAsync(dt => dt.Token == deviceToken);
                if (tokenEntity != null)
                {
                    _deviceTokenRepository.Delete(tokenEntity);
                    await _deviceTokenRepository.SaveChangesAsync();
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM push notification to {Token}", deviceToken[..Math.Min(20, deviceToken.Length)]);
                return false;
            }
        }

        public async Task<int> SendPushToPatientAsync(Guid patientId, string title, string body, Dictionary<string, string>? data = null)
        {
            var deviceTokens = await _deviceTokenRepository.GetAllAsync(dt => dt.PatientId == patientId);

            if (deviceTokens.Count == 0)
            {
                _logger.LogInformation("No device tokens found for patient {PatientId}. Push notification skipped.", patientId);
                return 0;
            }

            var sentCount = 0;
            foreach (var token in deviceTokens)
            {
                var success = await SendPushNotificationAsync(token.Token, title, body, data);
                if (success) sentCount++;
            }

            _logger.LogInformation("Sent {SentCount}/{TotalCount} push notification(s) to patient {PatientId}", sentCount, deviceTokens.Count, patientId);
            return sentCount;
        }
    }
}

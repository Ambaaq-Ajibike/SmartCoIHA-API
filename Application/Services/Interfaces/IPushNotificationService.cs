namespace Application.Services.Interfaces
{
    public interface IPushNotificationService
    {
        Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
        Task<int> SendPushToPatientAsync(Guid patientId, string title, string body, Dictionary<string, string>? data = null);
    }
}

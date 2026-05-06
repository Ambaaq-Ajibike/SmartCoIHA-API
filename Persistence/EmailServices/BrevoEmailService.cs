using Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace Persistence.EmailServices
{
    public class BrevoEmailService(IConfiguration configuration) : IEmailService
    {
        private readonly string _apiKey = configuration["Brevo:ApiKey"] ?? throw new ArgumentNullException("Brevo API Key not configured");
        private readonly string _senderEmail = configuration["Brevo:SenderEmail"] ?? throw new ArgumentNullException("Brevo Sender Email not configured");
        private readonly string _senderName = configuration["Brevo:SenderName"] ?? "SmartCoIHA";

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            try
            {
                // Configure API key authorization
                Configuration.Default.ApiKey.Clear();
                Configuration.Default.ApiKey.Add("api-key", _apiKey);

                var apiInstance = new TransactionalEmailsApi();

                // Create sender
                var sender = new SendSmtpEmailSender(name: _senderName, email: _senderEmail);

                // Create recipient
                var to = new List<SendSmtpEmailTo>
                {
                    new(email: toEmail, name: toName)
                };

                // Create email object
                var sendSmtpEmail = new SendSmtpEmail(
                    sender: sender,
                    to: to,
                    subject: subject,
                    htmlContent: htmlContent
                );

                // Send email
                var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);

                Console.WriteLine($"Email sent successfully to {toEmail}. Message ID: {result.MessageId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }
    }
}

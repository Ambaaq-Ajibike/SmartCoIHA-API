namespace API.BackgroundServices
{
    using Application.Messaging.Models;
    using Application.Services.Interfaces;
    using Microsoft.Extensions.Hosting;
    using Persistence.Messaging;

    public class RabbitMqConsumerService(
        RabbitMqConsumer _consumer,
        IServiceScopeFactory _scopeFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.StartAsync<EmailNotificationMessage>(
                "email-queue",
                async message =>
                {
                    Console.WriteLine($"Processing email notification for {message.To}");

                    // Convert plain text body to HTML if needed
                    var htmlContent = message.Body.Contains("<html>")
                        ? message.Body
                        : $"<html><body><pre>{message.Body}</pre></body></html>";

                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider
                   .GetRequiredService<IEmailService>();
                    // Send email using Brevo
                    var success = await emailService.SendEmailAsync(
                        toEmail: message.To,
                        toName: message.To,
                        subject: message.Subject,
                        htmlContent: htmlContent
                    );

                    if (success)
                    {
                        Console.WriteLine($"Email sent successfully to {message.To}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send email to {message.To}");
                    }
                });
        }
    }
}

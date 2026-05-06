using Application.Messaging.Models;
using Application.Services.Implementations;
using Persistence.Messaging;

namespace API.BackgroundServices
{
    public class FhirValidationConsumerService(
        IServiceProvider serviceProvider,
        RabbitMqConsumer _consumer,
        ILogger<FhirValidationConsumerService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("FHIR Validation Consumer Service starting...");

            try
            {
                using var scope = serviceProvider.CreateScope();
                var fhirValidationService = scope.ServiceProvider.GetRequiredService<FhirValidationService>();

                logger.LogInformation("Attempting to connect to RabbitMQ...");

                await _consumer.StartAsync<FhirEndpointValidationMessage>(
                    "fhir-validation-queue",
                    async (message) =>
                    {
                        try
                        {
                            using var messageScope = serviceProvider.CreateScope();
                            var scopedFhirService = messageScope.ServiceProvider.GetRequiredService<FhirValidationService>();

                            logger.LogInformation(
                                "Processing FHIR validation for endpoint {EndpointId}",
                                message.EndpointId);

                            await scopedFhirService.ValidateEndpointAsync(
                                message.EndpointId,
                                message.BaseUrl,
                                message.SupportedResources,
                                message.TestingPatientId);

                            logger.LogInformation(
                                "Completed FHIR validation for endpoint {EndpointId}",
                                message.EndpointId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex,
                                "Error processing FHIR validation for endpoint {EndpointId}",
                                message.EndpointId);
                        }
                    });

                logger.LogInformation("Successfully connected to RabbitMQ. FHIR Validation Consumer Service is running.");

                // Keep the service running until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Fatal error in FHIR Validation Consumer Service. " +
                    "Please ensure RabbitMQ is running at localhost:5672");
                throw; // Let the host handle the failure
            }
        }
    }
}
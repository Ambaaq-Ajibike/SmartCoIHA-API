using Application.Services.Implementations;
using Application.Services.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ServiceConfigurationExtension
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IInstitutionService, InstitutionService>();
            services.AddScoped<IPatientService, PatientService>();
            services.AddScoped<IFHIREndpointService, FHIREndpointService>();
            services.AddScoped<IDataRequestService, DataRequestService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<FhirValidationService>();

            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IPatientMobileService, PatientMobileService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        }
    }
}
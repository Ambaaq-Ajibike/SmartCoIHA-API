using Application.Services.Implementations;
using Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ServiceConfigurationExtension
    {
        public static void AddApplicationServices(this IServiceCollection services)
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
        }
    }
}
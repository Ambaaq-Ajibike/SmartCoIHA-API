using Application.Messaging.Interfaces;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Data;
using Persistence.Data.Repositories;
using Persistence.EmailServices;
using Persistence.Interceptors; // Make sure to import your interceptor
using Persistence.Messaging;
using Persistence.PushNotifications;
using Persistence.Services;
using StackExchange.Redis;

namespace Persistence
{
    public static class ServiceConfigurationExtension
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 1. Register the generic IHttpContextAccessor
            services.AddHttpContextAccessor();

            // 2. Register the Interceptor into DI
            services.AddScoped<AuditLogInterceptor>();

            // 3. Update DbContext registration to use the Service Provider (sp)
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                // Resolve the interceptor from the DI container
                var auditInterceptor = sp.GetRequiredService<AuditLogInterceptor>();

                options.UseNpgsql(connectionString)
                       .AddInterceptors(auditInterceptor); // Attach it to EF Core
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddSingleton<IMessagePublisher, RabbitMqProducer>();
            services.AddSingleton<RabbitMqConsumer>();


            services.AddScoped<IEmailService, BrevoEmailService>();

            var redisConnection = configuration.GetConnectionString("Redis");

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection)
            );

            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
        }
    }
}
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Infrastructure.Cache;
using CourseProject.Events.Infrastructure.DataAccess;
using CourseProject.Events.Infrastructure.Messaging.Consumers;
using CourseProject.Events.Infrastructure.Messaging.Producers;
using CourseProject.Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CourseProject.Events.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IEventRepository, EventRepository>();

            services.AddHostedService<KafkaTopicInitializer>();

            services.AddHostedService<BookingCreatedConsumer>();
            services.AddHostedService<BookingCancelledConsumer>();


            services.AddSingleton<IEventSeatsReservedProducer, EventSeatsReservedProducer>();
            services.AddSingleton<IEventSeatsReleasedProducer, EventSeatsReleasedProducer>();
            services.AddSingleton<IEventSeatsUnavailableProducer, EventSeatsUnavailableProducer>();

            var redisConnectionString = configuration["Redis:RedisConnection"].ToString();
            var options = new ConfigurationOptions
            {
                EndPoints = { redisConnectionString },
                ConnectTimeout = 5000,
                SyncTimeout = 3000,
                AbortOnConnectFail = false,
                ConnectRetry = 3,
            };

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(options)
            );

            services.AddScoped<ICacheService, CacheService>();

            return services;
        }
    }
}

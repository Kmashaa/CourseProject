using CourseProject.Events.Infrastructure.Messaging.Producers;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Infrastructure.DataAccess;
using CourseProject.Events.Infrastructure.Messaging.Consumers;
using CourseProject.Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IEventSeatsUnavailableProducer, EventSeatsUnavailableProducer>();

            return services;
        }
    }
}

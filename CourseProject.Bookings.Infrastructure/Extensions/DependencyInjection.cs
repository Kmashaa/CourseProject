using Confluent.Kafka;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Infrastructure.DataAccess;
using CourseProject.Bookings.Infrastructure.Messaging.Consumers;
using CourseProject.Bookings.Infrastructure.Messaging.Producers;
using CourseProject.Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Bookings.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IBookingRepository, BookingRepository>();


            services.AddHostedService<KafkaTopicInitializer>();

            services.AddHostedService<EventAvailablenessConsumer>();

            services.AddSingleton<IBookingCreatedProducer, BookingCreatedProducer>();
            services.AddSingleton<IBookingConfirmedProducer, BookingConfirmedProducer>();
            services.AddSingleton<IBookingRejectedProducer, BookingRejectedProducer>();
            services.AddSingleton<IBookingCancelledProducer, BookingCancelledProducer>();

            return services;
        }
    }
}

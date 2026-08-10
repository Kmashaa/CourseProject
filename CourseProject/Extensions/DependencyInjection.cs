using CourseProject.Data;
using CourseProject.DataAccess;
using CourseProject.Interfaces;
using CourseProject.Services;

namespace CourseProject.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<AppDbContext>();

            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();
            services.AddSingleton<IBookingDtoMapperService, BookingDtoMapperService>();
            services.AddHostedService<BookingProcessingService>();
            return services;

        }
    }
}

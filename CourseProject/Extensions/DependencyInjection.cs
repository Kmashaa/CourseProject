using CourseProject.Data;
using CourseProject.Interfaces;
using CourseProject.Services;

namespace CourseProject.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IEventRepository, EventRepository>(); // Added as Singleton for testing. Normally added as Scoped
            services.AddSingleton<IBookingRepository, BookingRepository>(); // Added as Singleton for testing. Normally added as Scoped

            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();
            services.AddSingleton<IBookingDtoMapperService, BookingDtoMapperService>();
            
            return services;

        }
    }
}

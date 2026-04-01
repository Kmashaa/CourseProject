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

            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();

            return services;

        }
    }
}

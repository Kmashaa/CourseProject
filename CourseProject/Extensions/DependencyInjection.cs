using CourseProject.Data;
using CourseProject.Interfaces;
using CourseProject.Services;

namespace CourseProject.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IEventRepository, EventRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            return services;

        }
    }
}

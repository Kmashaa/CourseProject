using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Events.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();

            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();
            services.AddSingleton<IEventFilterDtoMapperService, EventFilterDtoMapperService>();
            services.AddSingleton<IPaginatedResultDtoMapperService, PaginatedResultDtoMapperService>();
            services.AddSingleton<ITopEventDtoMapperService, TopEventDtoMapperService>();

            return services;

        }
    }
}

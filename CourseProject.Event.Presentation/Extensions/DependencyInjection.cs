using CourseProject.Events.Presentation.Interfaces;
using CourseProject.Events.Presentation.Services;

namespace CourseProject.Events.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddSingleton<IEventModelDtoMapperService, EventModelDtoMapperService>();
            services.AddSingleton<IEventFilterModelDtoMapperService, EventFilterModelDtoMapperService>();

            
            return services;

        }
    }
}

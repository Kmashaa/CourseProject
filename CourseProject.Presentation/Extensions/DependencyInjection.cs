using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Services;

namespace CourseProject.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddSingleton<IEventModelDtoMapperService, EventModelDtoMapperService>();
            services.AddSingleton<IBookingModelDtoMapperService, BookingModelDtoMapperService>();
            services.AddSingleton<IEventFilterModelDtoMapperService, EventFilterModelDtoMapperService>();

            
            return services;

        }
    }
}

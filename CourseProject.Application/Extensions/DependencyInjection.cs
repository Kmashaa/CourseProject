using CourseProject.Application.Interfaces;
using CourseProject.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CourseProject.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();
            services.AddSingleton<IBookingDtoMapperService, BookingDtoMapperService>();
            services.AddHostedService<BookingProcessingService>();
            services.AddSingleton<IEventFilterDtoMapperService, EventFilterDtoMapperService>();
            services.AddSingleton<IPaginatedResultDtoMapperService, PaginatedResultDtoMapperService>();

            return services;

        }
    }
}

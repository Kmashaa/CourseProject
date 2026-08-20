using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Bookings.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IBookingService, BookingService>();

            services.AddSingleton<IBookingDtoMapperService, BookingDtoMapperService>();
            services.AddHostedService<BookingProcessingService>();

            return services;

        }
    }
}

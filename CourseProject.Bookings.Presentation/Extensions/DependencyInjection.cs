using CourseProject.Bookings.Presentation.Interfaces;
using CourseProject.Bookings.Presentation.Services;

namespace CourseProject.Bookings.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddSingleton<IBookingModelDtoMapperService, BookingModelDtoMapperService>();

            return services;

        }
    }
}

using CourseProject.Presentation.DataAccess;
using CourseProject.Presentation.Interfaces;
using CourseProject.Presentation.Repositories;
using CourseProject.Presentation.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddSingleton<IEventDtoMapperService, EventDtoMapperService>();
            services.AddSingleton<IBookingDtoMapperService, BookingDtoMapperService>();
            services.AddHostedService<BookingProcessingService>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();

            return services;

        }
    }
}

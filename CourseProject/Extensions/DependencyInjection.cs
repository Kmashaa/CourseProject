using CourseProject.DataAccess;
using CourseProject.Interfaces;
using CourseProject.Repositories;
using CourseProject.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Extensions
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

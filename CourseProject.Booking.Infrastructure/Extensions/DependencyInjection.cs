using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Infrastructure.DataAccess;
using CourseProject.Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Bookings.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IBookingRepository, BookingRepository>();

            return services;
        }
    }
}

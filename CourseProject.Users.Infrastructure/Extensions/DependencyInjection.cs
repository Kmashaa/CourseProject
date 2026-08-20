using CourseProject.Users.Application.Interfaces;
using CourseProject.Users.Infrastructure.DataAccess;
using CourseProject.Users.Infrastructure.Repositories;
using CourseProject.Users.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Users.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IPasswordHasher, PasswordHasher>();

            services.Configure<JwtOptions>(options =>
            {
                configuration.GetSection(JwtOptions.SectionName).Bind(options);
            });

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}

using CourseProject.Users.Application.Interfaces;
using CourseProject.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CourseProject.Users.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();

            return services;

        }
    }
}

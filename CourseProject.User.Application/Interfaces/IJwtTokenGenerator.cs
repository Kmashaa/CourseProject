using CourseProject.Users.Domain.Entities;

namespace CourseProject.Users.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwt(Guid userId, string login, Roles role);
    }
}

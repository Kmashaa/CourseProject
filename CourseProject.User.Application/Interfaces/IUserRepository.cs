using CourseProject.Users.Domain.Entities;

namespace CourseProject.Users.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);

        Task<User> CreateAsync(User user);

        Task<User?> GetByLoginAsync(string login);


    }
}

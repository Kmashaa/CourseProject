namespace CourseProject.Users.Application.Interfaces
{
    public interface IUserService
    {
        Task<Guid> RegisterUserAsync(string login, string password, string role);
        Task<string> LoginUserAsync(string login, string password);

    }
}

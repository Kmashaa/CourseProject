namespace CourseProject.Users.Application.Models
{
    public class UserDto
    {
        public required Guid Id { get; init; }

        public required string Login { get; init; }

        public required string PasswordHash { get; init; }

        public required Roles Role { get; set; }
    }

    public enum Roles
    {
        User = 1,
        Admin = 2
    }
}

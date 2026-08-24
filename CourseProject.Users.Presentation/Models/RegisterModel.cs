using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace CourseProject.Users.Presentation.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Login is required")]
        [SwaggerSchema("Login of usser", Format = "string")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [SwaggerSchema("Password of user", Format = "string")]
        public string? Password { get; set; }

        [SwaggerSchema("Role of user (Admin, User)", Format = "string")]
        public string? Role { get; set; } = "User";
    }
}

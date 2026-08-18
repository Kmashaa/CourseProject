using CourseProject.Application.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Application.Interfaces
{
    public interface IUserService
    {
        Task<Guid> RegisterUserAsync(string login, string password, string role);
        Task<string> LoginUserAsync(string login, string password);

    }
}

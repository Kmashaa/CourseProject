using CourseProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);

        Task<User> CreateAsync(User user);

        Task<User?> GetByLoginAsync(string login);


    }
}

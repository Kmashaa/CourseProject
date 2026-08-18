using CourseProject.Application.Interfaces;
using CourseProject.Domain.Entities;
using CourseProject.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Infrastructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(e => e.Id == id);
            return user;

        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(e => e.Login == login);
            return user;

        }

        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;

        }
    }
}

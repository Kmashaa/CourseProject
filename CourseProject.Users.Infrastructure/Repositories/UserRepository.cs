using CourseProject.Users.Domain.Entities;
using CourseProject.Users.Application.Interfaces;
using CourseProject.Users.Domain.Exceptions;
using CourseProject.Users.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Users.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
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
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidUsersData();
            }

        }
    }
}

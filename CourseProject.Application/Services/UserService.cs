using CourseProject.Application.Interfaces;
using CourseProject.Domain.Entities;
using CourseProject.Domain.Exceptions;

namespace CourseProject.Application.Services
{
    public class UserService : IUserService
    {

        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public UserService(IPasswordHasher passwordHasher, IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<Guid> RegisterUserAsync(string login, string password, string role)
        {
            if (Enum.TryParse<Domain.Entities.Roles>(role, true, out Domain.Entities.Roles enumRole))
            {
                var passwordHash = _passwordHasher.Hash(password);
                var user = User.Create(login, passwordHash, enumRole);
                await _userRepository.CreateAsync(user);
                return user.Id;
            }
            else
            {
                throw new InvalidUsersData();
            }
        }

        public async Task<string> LoginUserAsync(string login, string password)
        {
            var user = await _userRepository.GetByLoginAsync(login);
            if (user == null)
            {
                throw new InvalidUsersData();
            }

            var isPasswordValid = _passwordHasher.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new InvalidUsersData();
            }

            var token = _jwtTokenGenerator.GenerateJwt(user.Id, user.Login, user.Role);
            return token;
        }

    }
}

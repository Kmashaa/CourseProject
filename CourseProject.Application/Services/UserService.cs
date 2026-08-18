using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Domain.Entities;
using CourseProject.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Application.Services
{
    public class UserService: IUserService
    {

        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        //private readonly IBookingDtoMapperService _bookingDtoMapperService;


        public UserService(IPasswordHasher passwordHasher, IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            //_bookingRepository = bookingRepository;
            //_eventRepository = eventRepository;
            //_eventDtoMapperService = eventDtoMapperService;
            //_bookingDtoMapperService = bookingDtoMapperService;
        }

        public async Task<Guid> RegisterUserAsync(string login, string password, string role)
        {
            if (Enum.TryParse<Domain.Entities.Roles>(role, true, out Domain.Entities.Roles enumRole)) 
            {
                var passwordHash = _passwordHasher.Hash(password);
                var user = User.Create(login, passwordHash, enumRole);
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

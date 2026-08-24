//using CourseProject.Application.Interfaces;
//using CourseProject.Application.Services;
//using CourseProject.Domain.Entities;
//using CourseProject.Domain.Exceptions;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace CourseProject.Tests
//{
//    public class UserServiceTests : IDisposable
//    {
//        private readonly Mock<IPasswordHasher> _passwordHasherMock;
//        private readonly Mock<IUserRepository> _userRepositoryMock;
//        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
//        private readonly IUserService _userService;

//        public UserServiceTests()
//        {
//            _passwordHasherMock = new Mock<IPasswordHasher>();
//            _userRepositoryMock = new Mock<IUserRepository>();
//            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

//            _userService = new UserService(
//                _passwordHasherMock.Object,
//                _userRepositoryMock.Object,
//                _jwtTokenGeneratorMock.Object);
//        }

//        public void Dispose()
//        {
//        }


//        [Fact]
//        public async Task RegisterUserAsync_WithValidData_ShouldCreateUserAndReturnId()
//        {
//            // Arrange
//            var login = "testuser";
//            var password = "password123";
//            var role = "User";
//            var expectedUserId = Guid.NewGuid();
//            var passwordHash = "hashed_password";

//            _passwordHasherMock
//                .Setup(hasher => hasher.Hash(password))
//                .Returns(passwordHash);

//            _userRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
//                .ReturnsAsync((User u) => u);

//            // Act
//            var result = await _userService.RegisterUserAsync(login, password, role);

//            // Assert
//            Assert.NotEqual(Guid.Empty, result);

//            _passwordHasherMock.Verify(hasher => hasher.Hash(password), Times.Once);
//            _userRepositoryMock.Verify(repo => repo.CreateAsync(
//                It.Is<User>(u =>
//                    u.Login == login &&
//                    u.PasswordHash == passwordHash &&
//                    u.Role == Domain.Entities.Roles.User
//                )
//            ), Times.Once);
//        }

//        [Fact]
//        public async Task RegisterUserAsync_WithAdminRole_ShouldCreateAdminUser()
//        {
//            // Arrange
//            var login = "adminuser";
//            var password = "adminpass";
//            var role = "Admin";
//            var passwordHash = "hashed_admin_password";

//            _passwordHasherMock
//                .Setup(hasher => hasher.Hash(password))
//                .Returns(passwordHash);

//            _userRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
//                .ReturnsAsync((User u) => u);

//            // Act
//            var result = await _userService.RegisterUserAsync(login, password, role);

//            // Assert
//            Assert.NotEqual(Guid.Empty, result);

//            _userRepositoryMock.Verify(repo => repo.CreateAsync(
//                It.Is<User>(u =>
//                    u.Login == login &&
//                    u.Role == Domain.Entities.Roles.Admin
//                )
//            ), Times.Once);
//        }

//        [Theory]
//        [InlineData("User")]
//        [InlineData("user")]
//        [InlineData("USER")]
//        [InlineData("Admin")]
//        [InlineData("admin")]
//        [InlineData("ADMIN")]
//        public async Task RegisterUserAsync_WithValidRoleCaseInsensitive_ShouldCreateUser(string role)
//        {
//            // Arrange
//            var login = "testuser";
//            var password = "password123";
//            var passwordHash = "hashed_password";

//            _passwordHasherMock
//                .Setup(hasher => hasher.Hash(password))
//                .Returns(passwordHash);

//            _userRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
//                .ReturnsAsync((User u) => u);

//            // Act
//            var result = await _userService.RegisterUserAsync(login, password, role);

//            // Assert
//            Assert.NotEqual(Guid.Empty, result);
//            _userRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
//        }

//        [Fact]
//        public async Task RegisterUserAsync_WithInvalidRole_ShouldThrowInvalidUsersData()
//        {
//            // Arrange
//            var login = "testuser";
//            var password = "password123";
//            var invalidRole = "InvalidRole";

//            // Act & Assert
//            await Assert.ThrowsAsync<InvalidUsersData>(async () =>
//                await _userService.RegisterUserAsync(login, password, invalidRole)
//            );

//            _passwordHasherMock.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
//            _userRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Never);
//        }

//        [Fact]
//        public async Task RegisterUserAsync_WithEmptyLogin_ShouldCreateUserWithEmptyLogin()
//        {
//            // Arrange
//            var login = "";
//            var password = "password123";
//            var role = "User";
//            var passwordHash = "hashed_password";

//            _passwordHasherMock
//                .Setup(hasher => hasher.Hash(password))
//                .Returns(passwordHash);

//            _userRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
//                .ReturnsAsync((User u) => u);

//            // Act
//            var result = await _userService.RegisterUserAsync(login, password, role);

//            // Assert
//            Assert.NotEqual(Guid.Empty, result);
//            _userRepositoryMock.Verify(repo => repo.CreateAsync(
//                It.Is<User>(u => u.Login == login)
//            ), Times.Once);
//        }



//        [Fact]
//        public async Task LoginUserAsync_WithValidCredentials_ShouldReturnJwtToken()
//        {
//            // Arrange
//            var login = "testuser";
//            var password = "password123";
//            var userId = Guid.NewGuid();
//            var expectedToken = "jwt_token_123";

//            var user = new User(
//                id: userId,
//                login: login,
//                passwordHash: "hashed_password",
//                role: Domain.Entities.Roles.User
//            );

//            _userRepositoryMock
//                .Setup(repo => repo.GetByLoginAsync(login))
//                .ReturnsAsync(user);

//            _passwordHasherMock
//                .Setup(hasher => hasher.Verify(password, user.PasswordHash))
//                .Returns(true);

//            _jwtTokenGeneratorMock
//                .Setup(generator => generator.GenerateJwt(user.Id, user.Login, user.Role))
//                .Returns(expectedToken);

//            // Act
//            var result = await _userService.LoginUserAsync(login, password);

//            // Assert
//            Assert.Equal(expectedToken, result);

//            _userRepositoryMock.Verify(repo => repo.GetByLoginAsync(login), Times.Once);
//            _passwordHasherMock.Verify(hasher => hasher.Verify(password, user.PasswordHash), Times.Once);
//            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwt(user.Id, user.Login, user.Role), Times.Once);
//        }

//        [Fact]
//        public async Task LoginUserAsync_WithNonExistentUser_ShouldThrowInvalidUsersData()
//        {
//            // Arrange
//            var login = "nonexistent";
//            var password = "password123";
//            User? nullUser = null;

//            _userRepositoryMock
//                .Setup(repo => repo.GetByLoginAsync(login))
//                .ReturnsAsync(nullUser);

//            // Act & Assert
//            await Assert.ThrowsAsync<InvalidUsersData>(async () =>
//                await _userService.LoginUserAsync(login, password)
//            );

//            _passwordHasherMock.Verify(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
//            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwt(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Domain.Entities.Roles>()), Times.Never);
//        }

//        [Fact]
//        public async Task LoginUserAsync_WithInvalidPassword_ShouldThrowInvalidUsersData()
//        {
//            // Arrange
//            var login = "testuser";
//            var wrongPassword = "wrong_password";
//            var userId = Guid.NewGuid();

//            var user = new User(
//                id: userId,
//                login: login,
//                passwordHash: "correct_hash",
//                role: Domain.Entities.Roles.User
//                );

//            _userRepositoryMock
//                .Setup(repo => repo.GetByLoginAsync(login))
//                .ReturnsAsync(user);

//            _passwordHasherMock
//                .Setup(hasher => hasher.Verify(wrongPassword, user.PasswordHash))
//                .Returns(false);

//            // Act & Assert
//            await Assert.ThrowsAsync<InvalidUsersData>(async () =>
//                await _userService.LoginUserAsync(login, wrongPassword)
//            );

//            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwt(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Domain.Entities.Roles>()), Times.Never);
//        }

//        [Fact]
//        public async Task LoginUserAsync_WithAdminUser_ShouldGenerateTokenWithAdminRole()
//        {
//            // Arrange
//            var login = "adminuser";
//            var password = "adminpass";
//            var userId = Guid.NewGuid();
//            var expectedToken = "admin_jwt_token";

//            var adminUser = new User(
//                id: userId,
//                login: login,
//                passwordHash: "admin_hash",
//                role: Domain.Entities.Roles.Admin
//                );

//            _userRepositoryMock
//                .Setup(repo => repo.GetByLoginAsync(login))
//                .ReturnsAsync(adminUser);

//            _passwordHasherMock
//                .Setup(hasher => hasher.Verify(password, adminUser.PasswordHash))
//                .Returns(true);

//            _jwtTokenGeneratorMock
//                .Setup(generator => generator.GenerateJwt(adminUser.Id, adminUser.Login, adminUser.Role))
//                .Returns(expectedToken);

//            // Act
//            var result = await _userService.LoginUserAsync(login, password);

//            // Assert
//            Assert.Equal(expectedToken, result);

//            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwt(
//                adminUser.Id,
//                adminUser.Login,
//                Domain.Entities.Roles.Admin
//            ), Times.Once);
//        }

//        [Fact]
//        public async Task LoginUserAsync_WithEmptyLogin_ShouldThrowInvalidUsersData()
//        {
//            // Arrange
//            var login = "";
//            var password = "password123";
//            User? nullUser = null;

//            _userRepositoryMock
//                .Setup(repo => repo.GetByLoginAsync(login))
//                .ReturnsAsync(nullUser);

//            // Act & Assert
//            await Assert.ThrowsAsync<InvalidUsersData>(async () =>
//                await _userService.LoginUserAsync(login, password)
//            );

//            _userRepositoryMock.Verify(repo => repo.GetByLoginAsync(login), Times.Once);
//        }
//    }
//}

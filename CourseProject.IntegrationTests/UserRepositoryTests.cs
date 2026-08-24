//using CourseProject.Domain.Entities;
//using CourseProject.Domain.Exceptions;
//using CourseProject.Infrastructure.DataAccess;
//using CourseProject.Infrastructure.Repositories;
//using Microsoft.EntityFrameworkCore;
//using Testcontainers.PostgreSql;

//namespace CourseProject.IntegrationTests
//{
//    public class UserRepositoryTests : IAsyncLifetime
//    {
//        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
//           .WithImage("postgres:16-alpine")
//           .Build();

//        public async Task InitializeAsync()
//        {
//            await _postgres.StartAsync();

//            await using var context = CreateContext();
//            await context.Database.MigrateAsync();
//        }

//        public async Task DisposeAsync()
//        {
//            await _postgres.DisposeAsync();
//        }

//        private AppDbContext CreateContext()
//        {
//            var options = new DbContextOptionsBuilder<AppDbContext>()
//                .UseNpgsql(_postgres.GetConnectionString())
//                .Options;

//            var context = new AppDbContext(options);
//            return context;
//        }

//        private async Task ResetDatabaseAsync()
//        {
//            await using var context = CreateContext();

//            await context.Database.ExecuteSqlRawAsync(
//                "TRUNCATE TABLE events, bookings, users RESTART IDENTITY CASCADE");
//        }

//        [Fact]
//        public async Task GetByIdAsync_ReturnsUser_WhenUserExists()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user = User.Create(
//                "testuser",
//                "hashedpassword123",
//                Roles.User
//            );

//            await context.Users.AddAsync(user);
//            await context.SaveChangesAsync();

//            var userRepository = new UserRepository(context);

//            // Act
//            var result = await userRepository.GetByIdAsync(user.Id);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(user.Id, result.Id);
//            Assert.Equal("testuser", result.Login);
//            Assert.Equal("hashedpassword123", result.PasswordHash);
//            Assert.Equal(Roles.User, result.Role);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotExist()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var userRepository = new UserRepository(context);
//            var nonExistentId = Guid.NewGuid();

//            // Act
//            var result = await userRepository.GetByIdAsync(nonExistentId);

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task GetByLoginAsync_ReturnsUser_WhenLoginExists()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user = User.Create(
//                "testuser",
//                "hashedpassword123",
//                Roles.User
//            );

//            await context.Users.AddAsync(user);
//            await context.SaveChangesAsync();

//            var userRepository = new UserRepository(context);

//            // Act
//            var result = await userRepository.GetByLoginAsync("testuser");

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(user.Id, result.Id);
//            Assert.Equal("testuser", result.Login);
//        }

//        [Fact]
//        public async Task GetByLoginAsync_ReturnsNull_WhenLoginDoesNotExist()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var userRepository = new UserRepository(context);

//            // Act
//            var result = await userRepository.GetByLoginAsync("nonexistentuser");

//            // Assert
//            Assert.Null(result);
//        }

//        [Fact]
//        public async Task CreateAsync_SavesUserToDatabase()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user = User.Create(
//                "newuser",
//                "hashedpassword123",
//                Roles.User
//            );

//            var userRepository = new UserRepository(context);

//            // Act
//            var result = await userRepository.CreateAsync(user);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(user.Id, result.Id);

//            await using var verifyContext = CreateContext();
//            var savedUser = await verifyContext.Users
//                .FirstOrDefaultAsync(u => u.Id == user.Id);

//            Assert.NotNull(savedUser);
//            Assert.Equal("newuser", savedUser.Login);
//            Assert.Equal("hashedpassword123", savedUser.PasswordHash);
//            Assert.Equal(Roles.User, savedUser.Role);
//        }

//        [Fact]
//        public async Task CreateAsync_ThrowsInvalidUsersData_WhenDuplicateLogin()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user1 = User.Create(
//                "duplicateuser",
//                "hashedpassword1",
//                Roles.User
//            );

//            var user2 = User.Create(
//                "duplicateuser",
//                "hashedpassword2",
//                Roles.Admin
//            );

//            var userRepository = new UserRepository(context);

//            // Act
//            await userRepository.CreateAsync(user1);

//            // Assert
//            var exception = await Assert.ThrowsAsync<InvalidUsersData>(
//                async () => await userRepository.CreateAsync(user2));

//            await using var verifyContext = CreateContext();
//            var count = await verifyContext.Users
//                .CountAsync(u => u.Login == "duplicateuser");

//            Assert.Equal(1, count);
//        }

//        [Fact]
//        public async Task CreateAsync_CreatesMultipleUsers_WithDifferentLogins()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user1 = User.Create(
//                "user1",
//                "hashedpassword1",
//                Roles.User
//            );

//            var user2 = User.Create(
//                "user2",
//                "hashedpassword2",
//                Roles.Admin
//            );

//            var userRepository = new UserRepository(context);

//            // Act
//            await userRepository.CreateAsync(user1);
//            await userRepository.CreateAsync(user2);

//            // Assert
//            await using var verifyContext = CreateContext();
//            var users = await verifyContext.Users.ToListAsync();

//            Assert.Equal(2, users.Count);
//            Assert.Contains(users, u => u.Login == "user1" && u.Role == Roles.User);
//            Assert.Contains(users, u => u.Login == "user2" && u.Role == Roles.Admin);
//        }

//        [Fact]
//        public async Task GetByLoginAsync_IsCaseSensitive()
//        {
//            // Arrange
//            await ResetDatabaseAsync();

//            await using var context = CreateContext();

//            var user = User.Create(
//                "TestCaseUser",
//                "hashedpassword123",
//                Roles.User
//            );

//            await context.Users.AddAsync(user);
//            await context.SaveChangesAsync();

//            var userRepository = new UserRepository(context);

//            // Act
//            var exactMatch = await userRepository.GetByLoginAsync("TestCaseUser");
//            var differentCase = await userRepository.GetByLoginAsync("testcaseuser");

//            // Assert
//            Assert.NotNull(exactMatch);
//            Assert.Equal("TestCaseUser", exactMatch.Login);
//            Assert.Null(differentCase);
//        }


//    }
//}


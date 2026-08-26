using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Infrastructure.DataAccess;
using CourseProject.Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CourseProject.Bookings.IntegrationTests
{
    public class BookingRepositoryTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
          .WithImage("postgres:16-alpine")
          .Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();

            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE bookings RESTART IDENTITY CASCADE");
        }

        private Booking CreateTestBooking(
    Guid? id = null,
    Guid? eventId = null,
    Guid? userId = null,
    BookingStatus status = BookingStatus.Pending,
    DateTime? createdAt = null)
        {
            return new Booking(
                id ?? Guid.NewGuid(),
                eventId ?? Guid.NewGuid(),
                userId ?? Guid.NewGuid(),
                status,
                createdAt ?? DateTime.UtcNow
            );
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingBooking_ReturnsBooking()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking = CreateTestBooking();

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetByIdAsync(booking.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
            Assert.Equal(booking.EventId, result.EventId);
            Assert.Equal(booking.UserId, result.UserId);
            Assert.Equal(booking.Status, result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentBooking_ReturnsNull()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_WithBookings_ReturnsAllBookings()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking1 = CreateTestBooking(status: BookingStatus.Pending);
            var booking2 = CreateTestBooking(status: BookingStatus.Confirmed);
            var booking3 = CreateTestBooking(status: BookingStatus.Cancelled);

            await context.Bookings.AddRangeAsync(new[] { booking1, booking2, booking3 });
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, b => b.Id == booking1.Id);
            Assert.Contains(result, b => b.Id == booking2.Id);
            Assert.Contains(result, b => b.Id == booking3.Id);
        }

        [Fact]
        public async Task UpdateAsync_WithExistingBooking_UpdatesBooking()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking = CreateTestBooking(status: BookingStatus.Pending);

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            context.Entry(booking).State = EntityState.Detached;

            var bookingRepository = new BookingRepository(context);

            var updatedBooking = new Booking(
                booking.Id,
                booking.EventId,
                booking.UserId,
                BookingStatus.Confirmed,
                booking.CreatedAt
            );

            // Act
            var result = await bookingRepository.UpdateAsync(updatedBooking);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Confirmed, result.Status);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(saved);
            Assert.Equal(BookingStatus.Confirmed, saved.Status);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingBooking_ReturnsTrueAndDeletes()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking = CreateTestBooking();

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.DeleteAsync(booking.Id);

            // Assert
            Assert.True(result);

            await using var verifyContext = CreateContext();
            var deleted = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentBooking_ReturnsFalse()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.DeleteAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetPendingsAsync_WithPendingBookings_ReturnsPendingIds()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var pendingBooking1 = CreateTestBooking(status: BookingStatus.Pending);
            var pendingBooking2 = CreateTestBooking(status: BookingStatus.Pending);
            var confirmedBooking = CreateTestBooking(status: BookingStatus.Confirmed);
            var cancelledBooking = CreateTestBooking(status: BookingStatus.Cancelled);

            await context.Bookings.AddRangeAsync(new[] { pendingBooking1, pendingBooking2, confirmedBooking, cancelledBooking });
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetPendingsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(pendingBooking1.Id, result);
            Assert.Contains(pendingBooking2.Id, result);
            Assert.DoesNotContain(confirmedBooking.Id, result);
            Assert.DoesNotContain(cancelledBooking.Id, result);
        }

        [Fact]
        public async Task GetPendingsAsync_WithNoPendingBookings_ReturnsEmptyList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var confirmedBooking = CreateTestBooking(status: BookingStatus.Confirmed);
            var cancelledBooking = CreateTestBooking(status: BookingStatus.Cancelled);

            await context.Bookings.AddRangeAsync(new[] { confirmedBooking, cancelledBooking });
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetPendingsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActiveBookingsCountByUserIdAsync_WithActiveBookings_ReturnsCount()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var userId = Guid.NewGuid();

            var pendingBooking = CreateTestBooking(userId: userId, status: BookingStatus.Pending);
            var confirmedBooking = CreateTestBooking(userId: userId, status: BookingStatus.Confirmed);
            var cancelledBooking = CreateTestBooking(userId: userId, status: BookingStatus.Cancelled);

            await context.Bookings.AddRangeAsync(new[] { pendingBooking, confirmedBooking, cancelledBooking });
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result); 
        }

        [Fact]
        public async Task GetActiveBookingsCountByUserIdAsync_WithNoActiveBookings_ReturnsZero()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var userId = Guid.NewGuid();

            var cancelledBooking = CreateTestBooking(userId: userId, status: BookingStatus.Cancelled);

            await context.Bookings.AddAsync(cancelledBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetActiveBookingsCountByUserIdAsync_WithDifferentUsers_ReturnsCorrectCount()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();

            var booking1 = CreateTestBooking(userId: user1Id, status: BookingStatus.Pending);
            var booking2 = CreateTestBooking(userId: user1Id, status: BookingStatus.Confirmed);
            var booking3 = CreateTestBooking(userId: user2Id, status: BookingStatus.Pending);
            var booking4 = CreateTestBooking(userId: user2Id, status: BookingStatus.Cancelled);

            await context.Bookings.AddRangeAsync(new[] { booking1, booking2, booking3, booking4 });
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result1 = await bookingRepository.GetActiveBookingsCountByUserIdAsync(user1Id);
            var result2 = await bookingRepository.GetActiveBookingsCountByUserIdAsync(user2Id);

            // Assert
            Assert.Equal(2, result1); 
            Assert.Equal(1, result2); 
        }

        [Fact]
        public async Task CreateBooking_WithDuplicateId_ThrowsException()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking = CreateTestBooking();

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var duplicateBooking = new Booking(
                booking.Id, 
                Guid.NewGuid(),
                Guid.NewGuid(),
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await bookingRepository.CreateAsync(duplicateBooking)
            );

            await using var verifyContext = CreateContext();
            var count = await verifyContext.Bookings
                .CountAsync(b => b.Id == booking.Id);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateBooking_WithValidBooking_ReturnsSavedBooking()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();
            var booking = CreateTestBooking();

            var bookingRepository = new BookingRepository(context);

            // Act
            var result = await bookingRepository.CreateAsync(booking);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
            Assert.Equal(booking.EventId, result.EventId);
            Assert.Equal(booking.UserId, result.UserId);
            Assert.Equal(booking.Status, result.Status);
            Assert.Equal(booking.CreatedAt, result.CreatedAt);

            await using var verifyContext = CreateContext();
            var savedBooking = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(savedBooking);
            Assert.Equal(booking.Id, savedBooking.Id);
            Assert.Equal(booking.Status, savedBooking.Status);
        }
    }

}


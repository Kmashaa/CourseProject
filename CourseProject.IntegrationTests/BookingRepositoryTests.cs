using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CourseProject.IntegrationTests
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
                "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
        }

        [Fact]
        public async Task Createbooking_SavesBookingToDatabase()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await bookingRepository.CreateAsync(booking);

            // Assert 
            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(saved);
            Assert.Equal(booking.EventId, saved.EventId);
            Assert.Equal(booking.Id, saved.Id);
            Assert.Equal(booking.Status, saved.Status);
            Assert.Equal(booking.CreatedAt, saved.CreatedAt);
        }

        [Fact]
        public async Task Createbooking_DoesntSaveDuplicateBooking()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var booking2 = new Booking(
                booking.Id,
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            // Act assert

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bookingRepository.CreateAsync(booking2));
            await using var verifyContext = CreateContext();
            var count = verifyContext.Bookings
                .Count(b => b.Id == booking.Id);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Createbooking_DoesntSaveBookingWithoutExistingEvent()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var booking = new Booking(
                Guid.NewGuid(),
                Guid.NewGuid(),
                BookingStatus.Pending,
                DateTime.UtcNow
            );


            var bookingRepository = new BookingRepository(context);


            // Act assert

            var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await bookingRepository.CreateAsync(booking));
            await using var verifyContext = CreateContext();
            var count = verifyContext.Bookings
                .Count(b => b.Id == booking.Id);

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetAll_SuccessfullyReturnsList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var booking2 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddRangeAsync(booking, booking2);
            await context.SaveChangesAsync();



            // Act assert
            var bookings = await bookingRepository.GetAllAsync();

            await using var verifyContext = CreateContext();
            var verifyBookings = verifyContext.Bookings.ToList();

            Assert.Equal(bookings.Count, verifyBookings.Count);
            Assert.NotNull(bookings);
            Assert.NotEmpty(bookings);

        }

        [Fact]
        public async Task GetAll_SuccessfullyReturnsEmptyList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var bookingRepository = new BookingRepository(context);

            // Act assert
            var events = await bookingRepository.GetAllAsync();

            await using var verifyContext = CreateContext();
            var verifyBookings = verifyContext.Bookings.ToList();

            Assert.Equal(events.Count, verifyBookings.Count);
            Assert.NotNull(events);
            Assert.Empty(events);

        }

        [Fact]
        public async Task GetById_SuccessfullyReturnsBooking()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act assert
            var bookingFromDb = await bookingRepository.GetByIdAsync(booking.Id);

            await using var verifyContext = CreateContext();
            var verifyBooking = verifyContext.Bookings.FirstOrDefault(o => o.Id == booking.Id);

            Assert.NotNull(verifyBooking);
            Assert.Equal(booking.EventId, verifyBooking.EventId);
            Assert.Equal(booking.Id, verifyBooking.Id);
            Assert.Equal(booking.Status, verifyBooking.Status);

        }

        [Fact]
        public async Task GetById_InvalidId_ReturnsNull()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var booking2 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddRangeAsync(booking, booking2);
            await context.SaveChangesAsync();

            // Act assert
            var testGuid = Guid.NewGuid();
            var eventFromDb = await bookingRepository.GetByIdAsync(testGuid);

            await using var verifyContext = CreateContext();
            var verifyBooking = verifyContext.Bookings.FirstOrDefault(o => o.Id == testGuid);

            Assert.Null(eventFromDb);
            Assert.Equal(eventFromDb, verifyBooking);
        }

        [Fact]
        public async Task UpdateBooking_SuccessfullyUpdates()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            booking.Status = BookingStatus.Confirmed;
            booking.CreatedAt = new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc);


            // Act assert
            var updated = await bookingRepository.UpdateAsync(booking);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.NotNull(updated);
            Assert.Equal(BookingStatus.Confirmed, updated.Status);
            Assert.Equal(new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), updated.CreatedAt);
        }

        [Fact]
        public async Task UpdateBooking_NotExistingBooking_Exception()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var booking = new Booking(
                Guid.NewGuid(),
                Guid.NewGuid(),
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);


            // Act assert

            var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await bookingRepository.UpdateAsync(booking));
            await using var verifyContext = CreateContext();
            var count = verifyContext.Bookings
                .Count(b => b.Id == booking.Id);

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task DeleteBooking_SuccessfullyDeletes()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);

            // Act Assert

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();


            var deleted = await bookingRepository.DeleteAsync(booking.Id);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.True(deleted);
            Assert.Null(saved);
        }

        [Fact]
        public async Task DeleteBooking_NotExistingBooking_False()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);

            // Act Assert

            var deleted = await bookingRepository.DeleteAsync(booking.Id);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.False(deleted);
            Assert.Null(saved);
        }

        [Fact]
        public async Task DeleteBooking_SuccessfullyDeletedWhenEventIsDeleted()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            );

            var bookingRepository = new BookingRepository(context);

            // Act Assert

            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();

            await using var verifyContext1 = CreateContext();
            var saved = await verifyContext1.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);


            context.Events.Remove(@event);
            await context.SaveChangesAsync();



            await using var verifyContext2 = CreateContext();
            var saved2 = await verifyContext2.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            Assert.Equal(booking.Id, saved.Id);
            Assert.Null(saved2);
        }


        [Fact]
        public async Task GetPendings_SuccessfullyReturnsList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var booking = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Pending,
                DateTime.UtcNow
            );

            var booking2 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Confirmed,
                DateTime.UtcNow
            );

            var booking3 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Rejected,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddRangeAsync(booking, booking2, booking3);
            await context.SaveChangesAsync();



            // Act assert
            var bookings = await bookingRepository.GetPendingsAsync();

            await using var verifyContext = CreateContext();
            var verifyPendings = verifyContext.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .Select(o => o.Id)
                .ToList();

            Assert.Equal(verifyPendings.Count, bookings.Count);
            Assert.NotNull(bookings);
            Assert.NotEmpty(bookings);
        }

        [Fact]
        public async Task GetPendings_SuccessfullyReturnsEmptyList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var @event = new Event(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();


            var booking2 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Confirmed,
                DateTime.UtcNow
            );

            var booking3 = new Booking(
                Guid.NewGuid(),
                @event.Id,
                BookingStatus.Rejected,
                DateTime.UtcNow
            );

            var bookingRepository = new BookingRepository(context);

            // Act
            await context.Bookings.AddRangeAsync(booking2, booking3);
            await context.SaveChangesAsync();



            // Act assert
            var bookings = await bookingRepository.GetPendingsAsync();

            await using var verifyContext = CreateContext();
            var verifyPendings = verifyContext.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .Select(o => o.Id)
                .ToList();

            Assert.Equal(verifyPendings.Count, bookings.Count);
            Assert.NotNull(bookings);
            Assert.Empty(bookings);
        }
    }
}

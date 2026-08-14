using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Models;
using CourseProject.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace CourseProject.IntegrationTests
{
    public class EventRepositoryTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
           .WithImage("postgres:16-alpine")
           .Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
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
            context.Database.EnsureCreated();
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
        }

        [Fact]
        public async Task CreateEvent_SavesEventToDatabase()
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

            var eventRepository = new EventRepository(context);

            // Act
            await eventRepository.CreateAsync(@event);

            // Assert 
            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Events
                .FirstOrDefaultAsync(b => b.Id == @event.Id);

            Assert.NotNull(saved);
            Assert.Equal("Test Event 1", saved.Title);
            Assert.Equal(50, saved.AvailableSeats);
        }

        [Fact]
        public async Task CreateEvent_DoesntSaveDuplicateEvent()
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

            var @event2 = new Event(
                @event.Id,
                "Test Event 2",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            var eventRepository = new EventRepository(context);

            // Act assert
            await eventRepository.CreateAsync(@event);

            var exception = Assert.ThrowsAsync<DbUpdateException>(async () => await eventRepository.CreateAsync(@event2));
            await using var verifyContext = CreateContext();
            var count = verifyContext.Events
                .Count(b => b.Id == @event.Id);

            Assert.Equal(1, count);
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

            var @event2 = new Event(
                Guid.NewGuid(),
                "Test Event 2",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddRangeAsync(@event, event2);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
           var events = await eventRepository.GetAllAsync();

            await using var verifyContext = CreateContext();
            var verifyEvents = verifyContext.Events.ToList();

            Assert.Equal(events.Count, verifyEvents.Count);
            Assert.NotNull(events);
            Assert.NotEmpty(events);

        }

        [Fact]
        public async Task GetAll_SuccessfullyReturnsEmptyList()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var eventRepository = new EventRepository(context);

            // Act assert
            var events = await eventRepository.GetAllAsync();

            await using var verifyContext = CreateContext();
            var verifyEvents = verifyContext.Events.ToList();

            Assert.Equal(events.Count, verifyEvents.Count);
            Assert.NotNull(events);
            Assert.Empty(events);

        }

        [Fact]
        public async Task GetById_SuccessfullyReturnsEvent()
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

            var eventRepository = new EventRepository(context);

            // Act assert
            var eventFromDb = await eventRepository.GetByIdAsync(@event.Id);

            await using var verifyContext = CreateContext();
            var verifyEvent = verifyContext.Events.FirstOrDefault(o=>o.Id==@event.Id);

            Assert.Equal(eventFromDb.Id, verifyEvent.Id);
            Assert.NotNull(eventFromDb);
            Assert.Equal("Test Event 1", eventFromDb.Title);
            Assert.Equal(50, eventFromDb.AvailableSeats);


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

            var @event2 = new Event(
                Guid.NewGuid(),
                "Test Event 2",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            await context.Events.AddRangeAsync(@event, event2);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var testGuid = Guid.NewGuid();
            var eventFromDb = await eventRepository.GetByIdAsync(testGuid);

            await using var verifyContext = CreateContext();
            var verifyEvent = verifyContext.Events.FirstOrDefault(o => o.Id == testGuid);

            Assert.Null(eventFromDb);
        }

        [Fact]
        public async Task UpdateEvent_SuccessfullyUpdates()
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

            @event.Title = "Test Event 2";
            @event.AvailableSeats = 60;

            var eventRepository = new EventRepository(context);

            // Act assert
            var updated = await eventRepository.UpdateAsync(@event);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Events
                .FirstOrDefaultAsync(b => b.Id == @event.Id);

            Assert.NotNull(updated);
            Assert.Equal("Test Event 2", updated.Title);
            Assert.Equal(60, updated.AvailableSeats);
        }

        [Fact]
        public async Task UpdateEvent_NotExistingEvent_Exception()
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

            var eventRepository = new EventRepository(context);

            // Act assert

            var exception = Assert.ThrowsAsync<DbUpdateException>(async () => await eventRepository.UpdateAsync(@event));
            await using var verifyContext = CreateContext();
            var count = verifyContext.Events
                .Count(b => b.Id == @event.Id);

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task DeleteEvent_SuccessfullyDeletes()
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

            var eventRepository = new EventRepository(context);

            // Act assert
            var deleted = await eventRepository.DeleteAsync(@event.Id);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Events
                .FirstOrDefaultAsync(b => b.Id == @event.Id);

            Assert.Equal(true, deleted);
            Assert.Null(saved);
        }

        [Fact]
        public async Task DeleteEvent_NotExistingEvent_False()
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

            var eventRepository = new EventRepository(context);

            // Act assert
            var deleted = await eventRepository.DeleteAsync(@event.Id);

            await using var verifyContext = CreateContext();
            var saved = await verifyContext.Events
                .FirstOrDefaultAsync(b => b.Id == @event.Id);

            Assert.Equal(false, deleted);
            Assert.Null(saved);
        }











        /////////////////////////////////////////////////////////////////////
        [Fact]
        public async Task GetEvents_FilterByTitle_ReturnsFilteredResults()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                Title = "Test"
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 )

                };
            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            
            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.Events.Count);
            Assert.All(result.Events, dto => Assert.Contains(filter.Title, dto.Title, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetEvents_FilterByStartDate_ReturnsFilteredResults()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                From = new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc)
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 10",new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 11",new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 12",new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 13",new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 14",new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 15",new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 16",new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)
            };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            // Act
            var eventRepository = new EventRepository(context);

            var result = await eventRepository.GetEventsWithFilterAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.TotalItems);
            Assert.Equal(7, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 1")?.Id);
            Assert.All(result.Events, dto =>
            {
                // Находим исходное событие по Id, чтобы сверить дату
                var originalEvent = allEvents.First(e => e.Id == dto.Id);
                Assert.True(originalEvent.StartAt >= filter.From);
            });
        }

        [Fact]
        public async Task GetEvents_FilterByEndDate_ReturnsFilteredResults()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                To = new DateTime(2026, 4, 13, 0, 30, 0, DateTimeKind.Utc)
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ("Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)
                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(8, result.TotalItems);
            Assert.Equal(8, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
            Assert.All(result.Events, dto =>
            {
                // Находим исходное событие по Id, чтобы сверить дату
                var originalEvent = allEvents.First(e => e.Id == dto.Id);
                Assert.True(originalEvent.EndAt <= filter.To);
            });
        }

        [Fact]
        public async Task GetEvents_DefaultPagination_ReturnsFirst10Results()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
            };

            var allEvents = new List<Event>
                {
                    Event.Create( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)

                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public async Task GetEvents_PaginationPage2_ReturnsTheSecondPage()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                Page = 2
            };


            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)

                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(6, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public async Task GetEvents_PaginationPageSize2_Returns2Items()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)

                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 2")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 3")?.Id);
        }

        [Fact]
        public async Task GetEvents_PaginationPage2PageSize2_Returns2ItemsFromSecondPage()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                Page = 2,
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)

                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 3")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 2")?.Id);
        }

        [Fact]
        public async Task GetEvents_CombinedFiltration_ReturnsFirst10Results()
        {
            // Arrange
            await ResetDatabaseAsync();

            await using var context = CreateContext();

            var filter = new EventFilter
            {
                Title = "4",
                Page = 2,
                PageSize = 1
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 12, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 13, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 18, 1, 0, 0, DateTimeKind.Utc), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 19, 1, 0, 0, DateTimeKind.Utc), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc), 50)

                };

            context.Events.AddRange(allEvents);
            await context.SaveChangesAsync();

            var eventRepository = new EventRepository(context);

            // Act assert
            var result = await eventRepository.GetEventsWithFilterAsync(filter);
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
            Assert.Equal(1, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 14")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 4")?.Id);
        }

    }
}

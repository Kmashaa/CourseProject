using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Models;
using CourseProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CourseProject.Tests
{
    public class EventServiceTests : IDisposable
    {
        //private readonly Mock<IEventRepository> _repositoryMock;
        //private readonly Mock<IEventDtoMapperService> _mapperMock;
        //private readonly Services.EventService _service;

        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;
        private readonly AppDbContext _context;

        public EventServiceTests()
        {

            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<IEventService, EventService>();

            var mapperMock = new Mock<IEventDtoMapperService>();
            services.AddSingleton(mapperMock.Object);

            _serviceProvider = services.BuildServiceProvider();

            _scope = _serviceProvider.CreateScope();

            _eventService = _serviceProvider.GetRequiredService<IEventService>();

            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        }

        // Реализация IDisposable для очистки ресурсов после каждого прогона теста
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            _scope.Dispose();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task GetAllEvents_ReturnsAllEvents()
        {
            // Arrange 

            var event1 = Event.Create("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50);
            var event2 = Event.Create("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc), 50);


            _context.Events.AddRange(event1, event2);
            await _context.SaveChangesAsync();

            // 2. Act 
            var result = await _eventService.GetAllEventsAsync();

            // 3. Assert (Проверка результатов)
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Test Event 1", result[0].Title);


        }

        [Fact]
        public async Task GetEventById_ExistedId_ReturnsEventsWithThisId()
        {
            // Arrange
            var expectedEvent = Event.Create(
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            _context.Events.Add(expectedEvent);
            await _context.SaveChangesAsync();


            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);
        }

        [Fact]
        public async Task GetEventById_NotExistedId_ReturnsNull()
        {
            // Arrange
            var notExistedGuid = Guid.NewGuid();

            // Act
            var result = await _eventService.GetEventByIdAsync(notExistedGuid);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateEvent_WithCorrectData_ShouldCallRepositoryCreate()
        {
            // Arrange
            var newEvent = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            // Act
            var result = await _eventService.CreateEventAsync(newEvent);

            // Assert
            Assert.Equal(newEvent, result);
            Assert.NotNull(result);

            var eventInDb = await _context.Events.FirstOrDefaultAsync(o => o.Id == newEvent.Id);
            Assert.Equal(newEvent.Title, eventInDb.Title);
            Assert.NotNull(eventInDb);
        }

        [Fact]
        public async Task CreateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var newEvent = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                50
            );

            // Act Assert
            var exception = Assert.ThrowsAsync<InvalidEventDataException>(async () => await _eventService.CreateEventAsync(newEvent));
        }

        [Fact]
        public async Task UpdateEvent_WithCorrectData_ReturnsUpdatedEvent()
        {
            // Arrange
            var eventToUpdate = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            var newEvent = Event.Create
            (
                "Test Event 2",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 2, 0, 0, DateTimeKind.Utc),
                60
            );

            newEvent.Id = eventToUpdate.Id;

            _context.Events.Add(eventToUpdate);
            await _context.SaveChangesAsync();
            // Act
            var result = await _eventService.UpdateEventAsync(newEvent);

            // Assert
            Assert.Equal(result, newEvent);

            _context.ChangeTracker.Clear();
            var eventInDb = await _context.Events.FirstOrDefaultAsync(o => o.Id == newEvent.Id);

            Assert.Equal(newEvent.Title, eventInDb.Title);
            Assert.Equal(newEvent.StartAt, eventInDb.StartAt);
            Assert.Equal(newEvent.EndAt, eventInDb.EndAt);
            Assert.Equal(newEvent.AvailableSeats, eventInDb.AvailableSeats);

            Assert.NotNull(eventInDb);
        }

        [Fact]
        public async Task UpdateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var eventToUpdate = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 8, 0, 0, 0),
                new DateTime(2026, 4, 5, 1, 0, 0),
                50
            );


            // Act Assert
            var exception = Assert.ThrowsAsync<InvalidEventDataException>(async () => await _eventService.UpdateEventAsync(eventToUpdate));
        }

        [Fact]
        public async Task DeleteEvent_WithCorrectId_DoesntThrowException()
        {
            // Arrange
            var eventToDelete = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            _context.Events.Add(eventToDelete);
            await _context.SaveChangesAsync();

            // Act
            var exception = await Record.ExceptionAsync(async () =>
                await _eventService.DeleteEventAsync(eventToDelete.Id));

            // Assert
            Assert.Null(exception);
            _context.ChangeTracker.Clear();

            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventToDelete.Id);
            Assert.Null(eventInDb);
        }

        [Fact]
        public async Task DeleteEvent_WithIncorrectId_ThrowsException()
        {
            // Arrange
            Guid? eventId = null;

            // Act Assert
            var exception = Assert.ThrowsAsync<InvalidEventDataException>(async () => await _eventService.DeleteEventAsync(eventId));
        }

        
    }
}

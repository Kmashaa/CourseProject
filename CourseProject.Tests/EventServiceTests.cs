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

        [Fact]
        public async Task GetEvents_FilterByTitle_ReturnsFilteredResults()
        {
            // Arrange
            var filter = new EventFilter
            {
                Title = "Test"
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 )

                };
            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsAsync(filter);

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
            var filter = new EventFilter
            {
                From = new DateTime(2026, 4, 13, 1, 0, 0)
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ("Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50),
                    Event.Create ("Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ("Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50),
                    Event.Create ("Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ("Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50),
                    Event.Create ("Test Event 10",new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ("Test Event 11",new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50),
                    Event.Create ("Test Event 12",new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ("Test Event 13",new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50),
                    Event.Create ("Test Event 14",new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ("Test Event 15",new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50),
                    Event.Create ("Test Event 16",new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)
            };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsAsync(filter);

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
            var filter = new EventFilter
            {
                To = new DateTime(2026, 4, 13, 0, 30, 0)
            };

            var allEvents = new List<Event>
                {
                    Event.Create ("Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50),
                    Event.Create ("Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ("Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50),
                    Event.Create ("Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ("Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50),
                    Event.Create ("Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ("Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50),
                    Event.Create ("Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ("Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50),
                    Event.Create ("Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ("Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50),
                    Event.Create ("Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ("Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50),
                    Event.Create ("Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ("Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50),
                    Event.Create ("Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)
                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
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
            var filter = new EventFilter
            {
            };

            var allEvents = new List<Event>
                {
                    Event.Create( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50),
                    Event.Create( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50),
                    Event.Create( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50),
                    Event.Create( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50),
                    Event.Create( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50),
                    Event.Create( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50),
                    Event.Create( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50),
                    Event.Create( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50),
                    Event.Create( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)

                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public async Task GetEvents_PaginationPage2_ReturnsTheSecondPage()
        {
            // Arrange
            var filter = new EventFilter
            {
                Page = 2
            };


            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)

                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(6, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public async Task GetEvents_PaginationPageSize2_Returns2Items()
        {
            // Arrange
            var filter = new EventFilter
            {
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)

                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            //Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
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
            var filter = new EventFilter
            {
                Page = 2,
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)

                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            //Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
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
            var filter = new EventFilter
            {
                Title = "4",
                Page = 2,
                PageSize = 1
            };

            var allEvents = new List<Event>
                {
                    Event.Create ( "Test Event 1", new DateTime(2026, 4, 5, 0, 0, 0), new DateTime(2026, 4, 5, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 2", new DateTime(2026, 4, 6, 0, 0, 0), new DateTime(2026, 4, 6, 1, 0, 0), 50),
                    Event.Create ( "Test Event 3", new DateTime(2026, 4, 7, 0, 0, 0), new DateTime(2026, 4, 7, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 4", new DateTime(2026, 4, 8, 0, 0, 0), new DateTime(2026, 4, 8, 1, 0, 0), 50),
                    Event.Create ( "Test Event 5", new DateTime(2026, 4, 9, 0, 0, 0), new DateTime(2026, 4, 9, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 6", new DateTime(2026, 4, 10, 0, 0, 0), new DateTime(2026, 4, 10, 1, 0, 0), 50),
                    Event.Create ( "Test Event 7", new DateTime(2026, 4, 11, 0, 0, 0), new DateTime(2026, 4, 11, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 8", new DateTime(2026, 4, 12, 0, 0, 0), new DateTime(2026, 4, 12, 1, 0, 0), 50),
                    Event.Create ( "Test Event 9", new DateTime(2026, 4, 13, 0, 0, 0), new DateTime(2026, 4, 13, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 10", new DateTime(2026, 4, 14, 0, 0, 0), new DateTime(2026, 4, 14, 1, 0, 0), 50),
                    Event.Create ( "Test Event 11", new DateTime(2026, 4, 15, 0, 0, 0), new DateTime(2026, 4, 15, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 12", new DateTime(2026, 4, 16, 0, 0, 0), new DateTime(2026, 4, 16, 1, 0, 0), 50),
                    Event.Create ( "Test Event 13", new DateTime(2026, 4, 17, 0, 0, 0), new DateTime(2026, 4, 17, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 14", new DateTime(2026, 4, 18, 0, 0, 0), new DateTime(2026, 4, 18, 1, 0, 0), 50),
                    Event.Create ( "Test Event 15", new DateTime(2026, 4, 19, 0, 0, 0), new DateTime(2026, 4, 19, 1, 0, 0), 50 ),
                    Event.Create ( "Test Event 16", new DateTime(2026, 4, 20, 0, 0, 0), new DateTime(2026, 4, 20, 1, 0, 0), 50)

                };

            _context.Events.AddRange(allEvents);
            await _context.SaveChangesAsync();

            //Act
            var result = await _eventService.GetEventsAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
            Assert.Equal(1, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 14")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 4")?.Id);
        }
    }
}

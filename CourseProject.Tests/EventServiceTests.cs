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
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<IEventDtoMapperService> _mapperMock;
        private readonly IEventService _eventService;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _mapperMock = new Mock<IEventDtoMapperService>();

            _eventService = new EventService(_eventRepositoryMock.Object, _mapperMock.Object);

        }

        // Реализация IDisposable для очистки ресурсов после каждого прогона теста
        public void Dispose()
        {
        }

        [Fact]
        public async Task GetAllEvents_ReturnsAllEvents()
        {
            // Arrange
            var event1 = Event.Create("Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50);

            var event2 = Event.Create("Test Event 2",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50);

            var eventsList = new List<Event> { event1, event2 };

            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(eventsList);

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Test Event 1", result[0].Title);

            _eventRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);

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

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(expectedEvent.Id))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedEvent.Id), Times.Once);
        }

        [Fact]
        public async Task GetEventById_NonExistedId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _eventService.GetEventByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
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

            _eventRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync(newEvent); // Теперь возвращаем Event

            // Act
            var result = await _eventService.CreateEventAsync(newEvent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEvent, result);
            Assert.Equal(newEvent.TotalSeats, result.AvailableSeats);
            Assert.NotEqual(Guid.Empty, result.Id);

            _eventRepositoryMock.Verify(repo => repo.CreateAsync(newEvent), Times.Once);
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

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.CreateEventAsync(newEvent)
            );

            _eventRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Event>()), Times.Never);
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
            eventToUpdate.AvailableSeats = 50;

            var newEvent = Event.Create
            (
                "Test Event 2",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 2, 0, 0, DateTimeKind.Utc),
                60
            );
            newEvent.Id = eventToUpdate.Id;

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(eventToUpdate.Id))
                .ReturnsAsync(eventToUpdate);

            _eventRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) => e);

            // Act
            var result = await _eventService.UpdateEventAsync(newEvent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEvent.Title, result.Title);
            Assert.Equal(newEvent.StartAt, result.StartAt);
            Assert.Equal(newEvent.EndAt, result.EndAt);
            Assert.Equal(60, result.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventToUpdate.Id), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var eventToUpdate = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.UpdateEventAsync(eventToUpdate)
            );

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
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


            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventToDelete.Id))
                .ReturnsAsync(true);

            // Act
            var exception = await Record.ExceptionAsync(async () =>
                await _eventService.DeleteEventAsync(eventToDelete.Id));

            // Assert
            Assert.Null(exception);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(eventToDelete.Id), Times.Once);
        }

        [Fact]
        public async Task DeleteEvent_WithIncorrectId_ThrowsException()
        {
            // Arrange
            Guid? eventId = null;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.DeleteEventAsync(eventId)
            );

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }


    }
}

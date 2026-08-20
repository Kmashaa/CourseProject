using CourseProject.Domain.Entities;
using CourseProject.Domain.Exceptions;
using CourseProject.Application.Exceptions;
using CourseProject.Application.Interfaces;
using CourseProject.Application.Models;
using CourseProject.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CourseProject.Tests
{
    public class EventServiceTests : IDisposable
    {
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<IEventDtoMapperService> _mapperMock;
        private readonly Mock<IEventFilterDtoMapperService> _filtermapperMock;
        private readonly Mock<IPaginatedResultDtoMapperService> _paginatedResultmapperMock;

        private readonly IEventService _eventService;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _mapperMock = new Mock<IEventDtoMapperService>();
            _filtermapperMock = new Mock<IEventFilterDtoMapperService>();
            _paginatedResultmapperMock = new Mock<IPaginatedResultDtoMapperService>();

            _eventService = new EventService(_eventRepositoryMock.Object, _mapperMock.Object, _filtermapperMock.Object, _paginatedResultmapperMock.Object);

        }

        // Реализация IDisposable для очистки ресурсов после каждого прогона теста
        public void Dispose()
        {
        }

        [Fact]
        public async Task GetAllEvents_ReturnsAllEvents()
        {
            //Arrange
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

            var expectedDto1 = new EventDto(event1.Id, event1.Title, event1.StartAt, event1.EndAt, event1.TotalSeats, event1.AvailableSeats, event1.Description);
            var expectedDto2 = new EventDto(event2.Id, event2.Title, event2.StartAt, event2.EndAt, event2.TotalSeats, event2.AvailableSeats, event2.Description);

            _mapperMock
                .Setup(mapper => mapper.EntityToDto(event1))
                .Returns(expectedDto1);

            _mapperMock
                .Setup(mapper => mapper.EntityToDto(event2))
                .Returns(expectedDto2);

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal(expectedDto1.Id, result[0].Id);
            Assert.Equal("Test Event 1", result[0].Title);
            Assert.Equal(expectedDto1.TotalSeats, result[0].TotalSeats);

            Assert.Equal(expectedDto2.Id, result[1].Id);
            Assert.Equal("Test Event 2", result[1].Title);
            Assert.Equal(expectedDto2.TotalSeats, result[1].TotalSeats);

            _eventRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
            _mapperMock.Verify(mapper => mapper.EntityToDto(event1), Times.Once);
            _mapperMock.Verify(mapper => mapper.EntityToDto(event2), Times.Once);

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

            var expectedDto = new EventDto(
                expectedEvent.Id,
                expectedEvent.Title,
                expectedEvent.StartAt,
                expectedEvent.EndAt,
                expectedEvent.TotalSeats,
                expectedEvent.AvailableSeats,
                expectedEvent.Description
            );

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(expectedEvent.Id))
                .ReturnsAsync(expectedEvent);

            _mapperMock
                .Setup(mapper => mapper.EntityToDto(expectedEvent))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);
            Assert.Equal(expectedEvent.TotalSeats, result.TotalSeats);
            Assert.Equal(expectedEvent.AvailableSeats, result.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedEvent.Id), Times.Once);
            _mapperMock.Verify(mapper => mapper.EntityToDto(expectedEvent), Times.Once);
        }

        [Fact]
        public async Task GetEventById_NonExistedId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<EventNotFoundException>(
                () => _eventService.GetEventByIdAsync(nonExistentId)
            );

            Assert.Null(exception.Event);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
            _mapperMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task CreateEvent_WithCorrectData_ShouldCallRepositoryCreate()
        {
            // Arrange
            Event capturedEvent = null!;

            var newEventDto = new EventDto(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50,
                0,
                null
            );

            _mapperMock
                .Setup(mapper => mapper.DtoToEntity(It.IsAny<EventDto>()))
                .Returns((EventDto dto) => new Event(
                    dto.Id,
                    dto.Title,
                    dto.StartAt,
                    dto.EndAt,
                    (int)dto.TotalSeats,
                    dto.Description
                ));

            _eventRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) =>
                {
                    capturedEvent = e;
                    return e;
                });

            // Act
            var result = await _eventService.CreateEventAsync(newEventDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Test Event 1", result.Title);
            Assert.Equal(50, result.TotalSeats);
            Assert.Equal(50, result.AvailableSeats);
            Assert.Equal(result.TotalSeats, result.AvailableSeats);

            Assert.NotNull(capturedEvent);
            Assert.Equal(result.Id, capturedEvent.Id);
            Assert.Equal("Test Event 1", capturedEvent.Title);
            Assert.Equal(50, capturedEvent.TotalSeats);
            Assert.Equal(50, capturedEvent.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Event>()), Times.Once);
            _mapperMock.Verify(mapper => mapper.DtoToEntity(It.IsAny<EventDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var newEventDto = new EventDto
            (
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                50
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.CreateEventAsync(newEventDto)
            );

            _eventRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEvent_WithCorrectData_ReturnsUpdatedEvent()
        {
            // Arrange
            var existingEventId = Guid.NewGuid();

            var existingEventEntity = new Event
            (
                existingEventId,
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );
            existingEventEntity.AvailableSeats = 50;

            var updateEventDto = new EventDto
            (
                existingEventId,
                "Test Event 2",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 2, 0, 0, DateTimeKind.Utc),
                60
            );

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEventId))
                .ReturnsAsync(existingEventEntity);

            _eventRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) => e);

            // Act
            var result = await _eventService.UpdateEventAsync(updateEventDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateEventDto.Title, result.Title);
            Assert.Equal(updateEventDto.StartAt, result.StartAt);
            Assert.Equal(updateEventDto.EndAt, result.EndAt);
            Assert.Equal(60, result.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEventId), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var invalidEventDto = new EventDto
            (
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.UpdateEventAsync(invalidEventDto)
            );

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEvent_WithCorrectId_DoesntThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var exception = await Record.ExceptionAsync(async () =>
                await _eventService.DeleteEventAsync(eventId));

            // Assert
            Assert.Null(exception);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(eventId), Times.Once);
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

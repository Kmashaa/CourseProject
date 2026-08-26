using CourseProject.Events.Application.Exceptions;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Models;
using CourseProject.Events.Application.Services;
using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Timers;

namespace CourseProject.Events.Tests
{
    public class EventServiceTests : IDisposable
    {
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<IEventDtoMapperService> _eventDtoMapperServiceMock;
        private readonly Mock<IEventFilterDtoMapperService> _eventFilterDtoMapperServiceMock;
        private readonly Mock<IPaginatedResultDtoMapperService> _paginatedResultDtoMapperServiceMock;
        private readonly Mock<ITopEventDtoMapperService> _topEventDtoMapperServiceMock;
        private readonly Mock<ICacheService> _cacheServiceMock;

        private readonly IEventService _eventService;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _eventDtoMapperServiceMock = new Mock<IEventDtoMapperService>();
            _eventFilterDtoMapperServiceMock = new Mock<IEventFilterDtoMapperService>();
            _paginatedResultDtoMapperServiceMock = new Mock<IPaginatedResultDtoMapperService>();
            _topEventDtoMapperServiceMock = new Mock<ITopEventDtoMapperService>();
            _cacheServiceMock = new Mock<ICacheService>();

            _eventService = new EventService(_eventRepositoryMock.Object, _eventDtoMapperServiceMock.Object, _eventFilterDtoMapperServiceMock.Object, _paginatedResultDtoMapperServiceMock.Object, _topEventDtoMapperServiceMock.Object, _cacheServiceMock.Object);

        }

        public void Dispose()
        {
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

            _eventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(expectedEvent))
                .Returns(expectedDto);

            _cacheServiceMock
                .Setup(cache => cache.GetById(expectedEvent.Id))
                .ReturnsAsync((Event?)null);

            _cacheServiceMock
                .Setup(cache => cache.SetById(expectedEvent.Id, expectedEvent))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);
            Assert.Equal(expectedEvent.TotalSeats, result.TotalSeats);
            Assert.Equal(expectedEvent.AvailableSeats, result.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedEvent.Id), Times.Once);
            _eventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(expectedEvent), Times.Once);
        }

        [Fact]
        public async Task GetEventById_WhenCached_ReturnsFromCache()
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

            _cacheServiceMock
                .Setup(cache => cache.GetById(expectedEvent.Id))
                .ReturnsAsync(expectedEvent);

            _eventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(expectedEvent))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto, result);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedEvent.Id), Times.Never);
            _cacheServiceMock.Verify(cache => cache.SetById(expectedEvent.Id, expectedEvent), Times.Never);
        }

        [Fact]
        public async Task GetEventById_WhenNotCached_ReturnsEventFromRepository()
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

            _cacheServiceMock
                .Setup(cache => cache.GetById(expectedEvent.Id))
                .ReturnsAsync((Event?)null);

            _cacheServiceMock
                .Setup(cache => cache.SetById(expectedEvent.Id, expectedEvent))
                .ReturnsAsync(expectedEvent);

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(expectedEvent.Id))
                .ReturnsAsync(expectedEvent);

            _eventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(expectedEvent))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            Assert.Equal(expectedDto.Title, result.Title);
            Assert.Equal(expectedDto.StartAt, result.StartAt);
            Assert.Equal(expectedDto.EndAt, result.EndAt);
            Assert.Equal(expectedDto.TotalSeats, result.TotalSeats);
            Assert.Equal(expectedDto.AvailableSeats, result.AvailableSeats);
            Assert.Equal(expectedDto.Description, result.Description);

            _cacheServiceMock.Verify(cache => cache.GetById(expectedEvent.Id), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetById(expectedEvent.Id, expectedEvent), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedEvent.Id), Times.Once);
            _eventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(expectedEvent), Times.Once);
        }

        [Fact]
        public async Task GetTopEvents_WhenCached_ReturnsFromCache()
        {
            // Arrange
            var number = 5;

            var topEvent1 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 1",
                Description = "Popular event",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                TotalSeats = 50,
                AvailableSeats = 10,
                SalesPercentage = 80m
            };

            var topEvent2 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 2",
                Description = "Very popular event",
                StartAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc),
                TotalSeats = 100,
                AvailableSeats = 5,
                SalesPercentage = 95m
            };

            var cachedEvents = new List<TopEvent> { topEvent1, topEvent2 };

            var expectedDtos = new List<TopEventDto>
    {
        new TopEventDto(
            topEvent1.Id,
            topEvent1.Title,
            topEvent1.Description,
            topEvent1.StartAt,
            topEvent1.EndAt,
            topEvent1.TotalSeats,
            topEvent1.AvailableSeats,
            topEvent1.SalesPercentage
        ),
        new TopEventDto(
            topEvent2.Id,
            topEvent2.Title,
            topEvent2.Description,
            topEvent2.StartAt,
            topEvent2.EndAt,
            topEvent2.TotalSeats,
            topEvent2.AvailableSeats,
            topEvent2.SalesPercentage
        )
    };

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync(cachedEvents);

            _topEventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(topEvent1))
                .Returns(expectedDtos[0]);

            _topEventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(topEvent2))
                .Returns(expectedDtos[1]);

            // Act
            var result = await _eventService.GetTopEvents(number);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal(expectedDtos[0].Id, result[0].Id);
            Assert.Equal(expectedDtos[0].Title, result[0].Title);
            Assert.Equal(expectedDtos[0].Description, result[0].Description);
            Assert.Equal(expectedDtos[0].StartAt, result[0].StartAt);
            Assert.Equal(expectedDtos[0].EndAt, result[0].EndAt);
            Assert.Equal(expectedDtos[0].TotalSeats, result[0].TotalSeats);
            Assert.Equal(expectedDtos[0].AvailableSeats, result[0].AvailableSeats);
            Assert.Equal(expectedDtos[0].SalesPercentage, result[0].SalesPercentage);

            Assert.Equal(expectedDtos[1].Id, result[1].Id);
            Assert.Equal(expectedDtos[1].Title, result[1].Title);
            Assert.Equal(expectedDtos[1].SalesPercentage, result[1].SalesPercentage);

            _eventRepositoryMock.Verify(repo => repo.GetTopEventsAsync(number), Times.Never);
            _cacheServiceMock.Verify(cache => cache.SetTop(number, It.IsAny<List<TopEvent>>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.GetTop(number), Times.Once);
            _topEventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(topEvent1), Times.Once);
            _topEventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(topEvent2), Times.Once);
        }

        [Fact]
        public async Task GetTopEvents_WhenNotCached_ReturnsFromRepositoryAndCaches()
        {
            // Arrange
            var number = 3;

            var topEvent1 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 1",
                Description = null,
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                TotalSeats = 50,
                AvailableSeats = 20,
                SalesPercentage = 60m
            };

            var topEvent2 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 2",
                Description = "Top rated",
                StartAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc),
                TotalSeats = 100,
                AvailableSeats = 10,
                SalesPercentage = 90m
            };

            var topEvent3 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 3",
                Description = "Best seller",
                StartAt = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 15, 1, 30, 0, DateTimeKind.Utc),
                TotalSeats = 75,
                AvailableSeats = 5,
                SalesPercentage = 93m
            };

            var eventsFromRepo = new List<TopEvent> { topEvent1, topEvent2, topEvent3 };

            var expectedDtos = new List<TopEventDto>
    {
        new TopEventDto(
            topEvent1.Id,
            topEvent1.Title,
            topEvent1.Description,
            topEvent1.StartAt,
            topEvent1.EndAt,
            topEvent1.TotalSeats,
            topEvent1.AvailableSeats,
            topEvent1.SalesPercentage
        ),
        new TopEventDto(
            topEvent2.Id,
            topEvent2.Title,
            topEvent2.Description,
            topEvent2.StartAt,
            topEvent2.EndAt,
            topEvent2.TotalSeats,
            topEvent2.AvailableSeats,
            topEvent2.SalesPercentage
        ),
        new TopEventDto(
            topEvent3.Id,
            topEvent3.Title,
            topEvent3.Description,
            topEvent3.StartAt,
            topEvent3.EndAt,
            topEvent3.TotalSeats,
            topEvent3.AvailableSeats,
            topEvent3.SalesPercentage
        )
    };

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync((List<TopEvent>?)null);

            _eventRepositoryMock
                .Setup(repo => repo.GetTopEventsAsync(number))
                .ReturnsAsync(eventsFromRepo);

            _cacheServiceMock
                .Setup(cache => cache.SetTop(number, eventsFromRepo))
                .ReturnsAsync(eventsFromRepo);

            for (int i = 0; i < eventsFromRepo.Count; i++)
            {
                _topEventDtoMapperServiceMock
                    .Setup(mapper => mapper.EntityToDto(eventsFromRepo[i]))
                    .Returns(expectedDtos[i]);
            }

            // Act
            var result = await _eventService.GetTopEvents(number);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            for (int i = 0; i < expectedDtos.Count; i++)
            {
                Assert.Equal(expectedDtos[i].Id, result[i].Id);
                Assert.Equal(expectedDtos[i].Title, result[i].Title);
                Assert.Equal(expectedDtos[i].Description, result[i].Description);
                Assert.Equal(expectedDtos[i].StartAt, result[i].StartAt);
                Assert.Equal(expectedDtos[i].EndAt, result[i].EndAt);
                Assert.Equal(expectedDtos[i].TotalSeats, result[i].TotalSeats);
                Assert.Equal(expectedDtos[i].AvailableSeats, result[i].AvailableSeats);
                Assert.Equal(expectedDtos[i].SalesPercentage, result[i].SalesPercentage);
            }

            _cacheServiceMock.Verify(cache => cache.GetTop(number), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetTopEventsAsync(number), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetTop(number, eventsFromRepo), Times.Once);
            _topEventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<TopEvent>()), Times.Exactly(3));
        }

        [Fact]
        public async Task GetTopEvents_WhenRepositoryReturnsNull_ThrowsEventNotFoundException()
        {
            // Arrange
            var number = 5;

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync((List<TopEvent>?)null);

            _eventRepositoryMock
                .Setup(repo => repo.GetTopEventsAsync(number))
                .ReturnsAsync((List<TopEvent>?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(
                () => _eventService.GetTopEvents(number)
            );

            _cacheServiceMock.Verify(cache => cache.GetTop(number), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetTopEventsAsync(number), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetTop(number, It.IsAny<List<TopEvent>>()), Times.Never);
            _topEventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<TopEvent>()), Times.Never);
        }

        [Fact]
        public async Task GetTopEvents_WhenCachedWithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var number = 5;
            var cachedEvents = new List<TopEvent>(); 

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync(cachedEvents);

            // Act
            var result = await _eventService.GetTopEvents(number);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _eventRepositoryMock.Verify(repo => repo.GetTopEventsAsync(number), Times.Never);
            _cacheServiceMock.Verify(cache => cache.GetTop(number), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetTop(number, It.IsAny<List<TopEvent>>()), Times.Never);
            _topEventDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<TopEvent>()), Times.Never);
        }

        [Fact]
        public async Task GetTopEvents_WithDifferentNumbers_UsesCorrectCacheKey()
        {
            // Arrange
            var number = 10;

            var topEvent1 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event",
                Description = "Test",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                TotalSeats = 50,
                AvailableSeats = 15,
                SalesPercentage = 70m
            };

            var eventsFromRepo = new List<TopEvent> { topEvent1 };

            var expectedDto = new TopEventDto(
                topEvent1.Id,
                topEvent1.Title,
                topEvent1.Description,
                topEvent1.StartAt,
                topEvent1.EndAt,
                topEvent1.TotalSeats,
                topEvent1.AvailableSeats,
                topEvent1.SalesPercentage
            );

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync((List<TopEvent>?)null);

            _eventRepositoryMock
                .Setup(repo => repo.GetTopEventsAsync(number))
                .ReturnsAsync(eventsFromRepo);

            _cacheServiceMock
                .Setup(cache => cache.SetTop(number, eventsFromRepo))
                .ReturnsAsync(eventsFromRepo);

            _topEventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(topEvent1))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetTopEvents(number);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedDto.Id, result[0].Id);
            Assert.Equal(expectedDto.Title, result[0].Title);
            Assert.Equal(expectedDto.SalesPercentage, result[0].SalesPercentage);

            _cacheServiceMock.Verify(cache => cache.GetTop(10), Times.Once);
            _cacheServiceMock.Verify(cache => cache.GetTop(It.Is<int>(n => n != 10)), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.GetTopEventsAsync(10), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetTop(10, eventsFromRepo), Times.Once);
        }

        [Fact]
        public async Task GetTopEvents_AfterMutation_InvalidatesCache()
        {
            // Arrange
            var number = 5;

            var topEvent1 = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Top Event 1",
                Description = "Test",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                TotalSeats = 50,
                AvailableSeats = 20,
                SalesPercentage = 60m
            };

            var eventsFromRepo = new List<TopEvent> { topEvent1 };

            var expectedDto = new TopEventDto(
                topEvent1.Id,
                topEvent1.Title,
                topEvent1.Description,
                topEvent1.StartAt,
                topEvent1.EndAt,
                topEvent1.TotalSeats,
                topEvent1.AvailableSeats,
                topEvent1.SalesPercentage
            );

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync((List<TopEvent>?)null);

            _eventRepositoryMock
                .Setup(repo => repo.GetTopEventsAsync(number))
                .ReturnsAsync(eventsFromRepo);

            _cacheServiceMock
                .Setup(cache => cache.SetTop(number, eventsFromRepo))
                .ReturnsAsync(eventsFromRepo);

            _topEventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(topEvent1))
                .Returns(expectedDto);

            // Act 
            var firstResult = await _eventService.GetTopEvents(number);

            // Assert 
            Assert.NotNull(firstResult);
            Assert.Single(firstResult);
            _cacheServiceMock.Verify(cache => cache.SetTop(number, eventsFromRepo), Times.Once);

            topEvent1.AvailableSeats = 10; 
            topEvent1.SalesPercentage = 80m;

            var updatedExpectedDto = new TopEventDto(
                topEvent1.Id,
                topEvent1.Title,
                topEvent1.Description,
                topEvent1.StartAt,
                topEvent1.EndAt,
                topEvent1.TotalSeats,
                topEvent1.AvailableSeats,
                topEvent1.SalesPercentage
            );

            _cacheServiceMock
                .Setup(cache => cache.GetTop(number))
                .ReturnsAsync(new List<TopEvent> { topEvent1 }); 

            _topEventDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(topEvent1))
                .Returns(updatedExpectedDto);

            // Act 
            var secondResult = await _eventService.GetTopEvents(number);

            // Assert 
            Assert.Single(secondResult);
            Assert.Equal(10, secondResult[0].AvailableSeats);
            Assert.Equal(80m, secondResult[0].SalesPercentage);
        }

        [Fact]
        public async Task GetEvents_WithValidFilter_ReturnsPaginatedEvents()
        {
            // Arrange
            var filterDto = new EventFilterDto
            {
                Page = 1,
                PageSize = 10,
                Title = "Test",
                From = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc)
            };

            var filterEntity = new EventFilter
            {
                Page = filterDto.Page,
                PageSize = filterDto.PageSize,
                Title = filterDto.Title,
                From = filterDto.From,
                To = filterDto.To
            };

            var event1 = Event.Create(
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50,
                "Description 1"
            );

            var event2 = Event.Create(
                "Test Event 2",
                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc),
                30,
                "Description 2"
            );

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event> { event1, event2 },
                TotalItems = 2,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 2
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>
        {
            new EventDto(
                event1.Id,
                event1.Title,
                event1.StartAt,
                event1.EndAt,
                event1.TotalSeats,
                event1.AvailableSeats,
                event1.Description
            ),
            new EventDto(
                event2.Id,
                event2.Title,
                event2.StartAt,
                event2.EndAt,
                event2.TotalSeats,
                event2.AvailableSeats,
                event2.Description
            )
        },
                TotalItems = 2,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 2
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.TotalItems, result.TotalItems);
            Assert.Equal(expectedDto.CurrentPage, result.CurrentPage);
            Assert.Equal(expectedDto.NumOfItemsOnCurrentPage, result.NumOfItemsOnCurrentPage);
            Assert.Equal(expectedDto.EventsDto.Count, result.EventsDto.Count);

            for (int i = 0; i < expectedDto.EventsDto.Count; i++)
            {
                Assert.Equal(expectedDto.EventsDto[i].Id, result.EventsDto[i].Id);
                Assert.Equal(expectedDto.EventsDto[i].Title, result.EventsDto[i].Title);
                Assert.Equal(expectedDto.EventsDto[i].StartAt, result.EventsDto[i].StartAt);
                Assert.Equal(expectedDto.EventsDto[i].EndAt, result.EventsDto[i].EndAt);
                Assert.Equal(expectedDto.EventsDto[i].TotalSeats, result.EventsDto[i].TotalSeats);
                Assert.Equal(expectedDto.EventsDto[i].AvailableSeats, result.EventsDto[i].AvailableSeats);
                Assert.Equal(expectedDto.EventsDto[i].Description, result.EventsDto[i].Description);
            }

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task GetEvents_WithDefaultFilter_ReturnsFirstPage()
        {
            // Arrange
            var filterDto = new EventFilterDto(); 

            var filterEntity = new EventFilter
            {
                Page = 1,
                PageSize = 10
            };

            var event1 = Event.Create(
                "Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event> { event1 },
                TotalItems = 1,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 1
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>
        {
            new EventDto(
                event1.Id,
                event1.Title,
                event1.StartAt,
                event1.EndAt,
                event1.TotalSeats,
                event1.AvailableSeats,
                event1.Description
            )
        },
                TotalItems = 1,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 1
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(1, result.NumOfItemsOnCurrentPage);
            Assert.Single(result.EventsDto);

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task GetEvents_WithEmptyResult_ReturnsEmptyPaginatedResult()
        {
            // Arrange
            var filterDto = new EventFilterDto
            {
                Page = 1,
                PageSize = 10,
                Title = "NonExistentEvent"
            };

            var filterEntity = new EventFilter
            {
                Page = filterDto.Page,
                PageSize = filterDto.PageSize,
                Title = filterDto.Title
            };

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event>(),
                TotalItems = 0,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 0
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>(),
                TotalItems = 0,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 0
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalItems);
            Assert.Empty(result.EventsDto);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(0, result.NumOfItemsOnCurrentPage);

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task GetEvents_WithNullFilter_ReturnsAllEvents()
        {
            // Arrange
            EventFilterDto filterDto = null;
            EventFilter filterEntity = null;

            var event1 = Event.Create(
                "Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );

            var event2 = Event.Create(
                "Event 2",
                new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 15, 2, 0, 0, DateTimeKind.Utc),
                100,
                "Special Event"
            );

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event> { event1, event2 },
                TotalItems = 2,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 2
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>
        {
            new EventDto(
                event1.Id,
                event1.Title,
                event1.StartAt,
                event1.EndAt,
                event1.TotalSeats,
                event1.AvailableSeats,
                event1.Description
            ),
            new EventDto(
                event2.Id,
                event2.Title,
                event2.StartAt,
                event2.EndAt,
                event2.TotalSeats,
                event2.AvailableSeats,
                event2.Description
            )
        },
                TotalItems = 2,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 2
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.TotalItems, result.TotalItems);
            Assert.Equal(expectedDto.CurrentPage, result.CurrentPage);
            Assert.Equal(expectedDto.NumOfItemsOnCurrentPage, result.NumOfItemsOnCurrentPage);
            Assert.Equal(expectedDto.EventsDto.Count, result.EventsDto.Count);
            Assert.NotNull(result.EventsDto);

            for (int i = 0; i < expectedDto.EventsDto.Count; i++)
            {
                Assert.Equal(expectedDto.EventsDto[i].Id, result.EventsDto[i].Id);
                Assert.Equal(expectedDto.EventsDto[i].Title, result.EventsDto[i].Title);
                Assert.Equal(expectedDto.EventsDto[i].StartAt, result.EventsDto[i].StartAt);
                Assert.Equal(expectedDto.EventsDto[i].EndAt, result.EventsDto[i].EndAt);
                Assert.Equal(expectedDto.EventsDto[i].TotalSeats, result.EventsDto[i].TotalSeats);
                Assert.Equal(expectedDto.EventsDto[i].AvailableSeats, result.EventsDto[i].AvailableSeats);
                Assert.Equal(expectedDto.EventsDto[i].Description, result.EventsDto[i].Description);
            }

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task GetEvents_WithDateRangeFilter_ReturnsFilteredEvents()
        {
            // Arrange
            var filterDto = new EventFilterDto
            {
                Page = 1,
                PageSize = 10,
                From = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc)
            };

            var filterEntity = new EventFilter
            {
                Page = filterDto.Page,
                PageSize = filterDto.PageSize,
                From = filterDto.From,
                To = filterDto.To
            };

            var event1 = Event.Create(
                "April Event",
                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc),
                60,
                "In April"
            );

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event> { event1 },
                TotalItems = 1,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 1
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>
        {
            new EventDto(
                event1.Id,
                event1.Title,
                event1.StartAt,
                event1.EndAt,
                event1.TotalSeats,
                event1.AvailableSeats,
                event1.Description
            )
        },
                TotalItems = 1,
                CurrentPage = 1,
                NumOfItemsOnCurrentPage = 1
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.EventsDto);
            Assert.Equal("April Event", result.EventsDto[0].Title);
            Assert.Equal(new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), result.EventsDto[0].StartAt);

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task GetEvents_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var filterDto = new EventFilterDto
            {
                Page = 2,
                PageSize = 2
            };

            var filterEntity = new EventFilter
            {
                Page = filterDto.Page,
                PageSize = filterDto.PageSize
            };

            var event3 = Event.Create(
                "Event 3",
                new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc),
                75
            );

            var event4 = Event.Create(
                "Event 4",
                new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 5, 2, 0, 0, DateTimeKind.Utc),
                40,
                "Page 2 Event"
            );

            var paginatedResult = new PaginatedResult
            {
                Events = new List<Event> { event3, event4 },
                TotalItems = 10, 
                CurrentPage = 2,
                NumOfItemsOnCurrentPage = 2
            };

            var expectedDto = new PaginatedResultDto
            {
                EventsDto = new List<EventDto>
        {
            new EventDto(
                event3.Id,
                event3.Title,
                event3.StartAt,
                event3.EndAt,
                event3.TotalSeats,
                event3.AvailableSeats,
                event3.Description
            ),
            new EventDto(
                event4.Id,
                event4.Title,
                event4.StartAt,
                event4.EndAt,
                event4.TotalSeats,
                event4.AvailableSeats,
                event4.Description
            )
        },
                TotalItems = 10,
                CurrentPage = 2,
                NumOfItemsOnCurrentPage = 2
            };

            _eventFilterDtoMapperServiceMock
                .Setup(mapper => mapper.DtoToEntity(filterDto))
                .Returns(filterEntity);

            _eventRepositoryMock
                .Setup(repo => repo.GetEventsWithFilterAsync(filterEntity))
                .ReturnsAsync(paginatedResult);

            _paginatedResultDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(paginatedResult))
                .Returns(expectedDto);

            // Act
            var result = await _eventService.GetEventsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalItems);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(2, result.NumOfItemsOnCurrentPage);
            Assert.Equal(2, result.EventsDto.Count);
            Assert.Equal("Event 3", result.EventsDto[0].Title);
            Assert.Equal("Event 4", result.EventsDto[1].Title);

            _eventFilterDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(filterDto), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.GetEventsWithFilterAsync(filterEntity), Times.Once);
            _paginatedResultDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(paginatedResult), Times.Once);
        }

        [Fact]
        public async Task CreateEvent_WithCorrectData_ShouldCreateAndCacheEvent()
        {
            // Arrange
            Event? capturedEvent = null;
            Event? cachedEvent = null;

            var newEventDto = new EventDto(
                Guid.NewGuid(), 
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50,
                0,
                null
            );

            _eventDtoMapperServiceMock
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
                .Callback<Event>(e => capturedEvent = e)
                .ReturnsAsync((Event e) => e);

            _cacheServiceMock
                .Setup(cache => cache.SetById(It.IsAny<Guid>(), It.IsAny<Event>()))
                .Callback<Guid, Event>((id, evt) => cachedEvent = evt)
                .ReturnsAsync((Guid id, Event e) => e);

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

            Assert.NotNull(cachedEvent);
            Assert.Equal(result.Id, cachedEvent.Id);
            Assert.Equal("Test Event 1", cachedEvent.Title);

            _eventRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Event>()), Times.Once);
            _eventDtoMapperServiceMock.Verify(mapper => mapper.DtoToEntity(It.IsAny<EventDto>()), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetById(result.Id, It.IsAny<Event>()), Times.Once);
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
        public async Task CreateEvent_WithCorrectData_ShouldCacheCreatedEvent()
        {
            // Arrange
            var newEventDto = new EventDto(
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50,
                0,
                null
            );

            Event? cachedEvent = null;

            _eventDtoMapperServiceMock
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
                .ReturnsAsync((Event e) => e);

            _cacheServiceMock
                .Setup(cache => cache.SetById(It.IsAny<Guid>(), It.IsAny<Event>()))
                .Callback<Guid, Event>((id, evt) => cachedEvent = evt)
                .ReturnsAsync((Guid id, Event e) => e);

            // Act
            var result = await _eventService.CreateEventAsync(newEventDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(cachedEvent);
            Assert.Equal(result.Id, cachedEvent.Id);
            Assert.Equal(result.Title, cachedEvent.Title);
            Assert.Equal(result.TotalSeats, cachedEvent.TotalSeats);
            Assert.Equal(result.AvailableSeats, cachedEvent.AvailableSeats);

            _cacheServiceMock.Verify(cache => cache.SetById(
                result.Id,
                It.Is<Event>(e =>
                    e.Id == result.Id &&
                    e.Title == result.Title &&
                    e.TotalSeats == result.TotalSeats &&
                    e.AvailableSeats == result.AvailableSeats
                )), Times.Once);
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
        public async Task UpdateEvent_WithCorrectData_ReturnsUpdatedEventAndUpdatesCache()
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
                60,
                60,  
                null
            );

            Event? updatedEntity = null;

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEventId))
                .ReturnsAsync(existingEventEntity);

            _eventRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
                .Callback<Event>(e => updatedEntity = e)
                .ReturnsAsync((Event e) => e);

            _cacheServiceMock
                .Setup(cache => cache.SetById(existingEventId, It.IsAny<Event>()))
                .ReturnsAsync((Guid id, Event e) => e);

            // Act
            var result = await _eventService.UpdateEventAsync(updateEventDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateEventDto.Title, result.Title);
            Assert.Equal(updateEventDto.StartAt, result.StartAt);
            Assert.Equal(updateEventDto.EndAt, result.EndAt);
            Assert.Equal(60, result.TotalSeats);
            Assert.Equal(60, result.AvailableSeats);

            Assert.NotNull(updatedEntity);
            Assert.Equal("Test Event 2", updatedEntity.Title);
            Assert.Equal(60, updatedEntity.TotalSeats);
            Assert.Equal(60, updatedEntity.AvailableSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEventId), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
            _cacheServiceMock.Verify(cache => cache.SetById(existingEventId, It.Is<Event>(e =>
                e.Title == "Test Event 2" &&
                e.TotalSeats == 60 &&
                e.AvailableSeats == 60
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_WithCorrectData_ShouldInvalidateOrUpdateCache()
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
            existingEventEntity.AvailableSeats = 30; 

            var updateEventDto = new EventDto
            (
                existingEventId,
                "Test Event 2",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 2, 0, 0, DateTimeKind.Utc),
                70,
                0, 
                null
            );

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEventId))
                .ReturnsAsync(existingEventEntity);

            _eventRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) => e);

            _cacheServiceMock
                .Setup(cache => cache.SetById(existingEventId, It.IsAny<Event>()))
                .ReturnsAsync((Guid id, Event e) => e);

            // Act
            var result = await _eventService.UpdateEventAsync(updateEventDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(70, result.TotalSeats);
            Assert.Equal(50, result.AvailableSeats);

            _cacheServiceMock.Verify(cache => cache.SetById(existingEventId, It.Is<Event>(e =>
                e.TotalSeats == 70 &&
                e.AvailableSeats == 50
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_WithInvalidData_ThrowsException()
        {
            // Arrange
            var invalidEventDto = new EventDto
            (
                Guid.NewGuid(),
                "Test Event 1",
                new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc), 
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50,
                50,
                null
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.UpdateEventAsync(invalidEventDto)
            );

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.SetById(It.IsAny<Guid>(), It.IsAny<Event>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.DeleteById(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEvent_WithNonExistentId_ThrowsEventNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            var updateEventDto = new EventDto
            (
                nonExistentId,
                "Test Event 2",
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 2, 0, 0, DateTimeKind.Utc),
                60,
                60,
                null
            );

            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(
                () => _eventService.UpdateEventAsync(updateEventDto)
            );

            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.SetById(It.IsAny<Guid>(), It.IsAny<Event>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.DeleteById(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEvent_WithCorrectId_ShouldDeleteAndInvalidateCache()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(true);

            _cacheServiceMock
                .Setup(cache => cache.DeleteById(eventId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId);

            // Assert
            Assert.True(result);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(eventId), Times.Once);

            _cacheServiceMock.Verify(cache => cache.DeleteById(eventId), Times.Once);
        }

        [Fact]
        public async Task DeleteEvent_WithCorrectId_DoesntThrowException()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(true);

            _cacheServiceMock
                .Setup(cache => cache.DeleteById(eventId))
                .Returns(Task.CompletedTask);

            // Act
            var exception = await Record.ExceptionAsync(async () =>
                await _eventService.DeleteEventAsync(eventId));

            // Assert
            Assert.Null(exception);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(eventId), Times.Once);
            _cacheServiceMock.Verify(cache => cache.DeleteById(eventId), Times.Once);
        }

        [Fact]
        public async Task DeleteEvent_WithNullId_ThrowsException()
        {
            // Arrange
            Guid? eventId = null;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(
                async () => await _eventService.DeleteEventAsync(eventId)
            );

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<Guid>()), Times.Never);
            _cacheServiceMock.Verify(cache => cache.DeleteById(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEvent_WithNonExistentId_ShouldReturnFalseButStillInvalidateCache()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(nonExistentId))
                .ReturnsAsync(false);

            _cacheServiceMock
                .Setup(cache => cache.DeleteById(nonExistentId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _eventService.DeleteEventAsync(nonExistentId);

            // Assert
            Assert.False(result);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(nonExistentId), Times.Once);

            _cacheServiceMock.Verify(cache => cache.DeleteById(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task DeleteEvent_ShouldDeleteFromRepositoryBeforeInvalidatingCache()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var operations = new List<string>();

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventId))
                .Callback(() => operations.Add("repository"))
                .ReturnsAsync(true);

            _cacheServiceMock
                .Setup(cache => cache.DeleteById(eventId))
                .Callback(() => operations.Add("cache"))
                .Returns(Task.CompletedTask);

            // Act
            await _eventService.DeleteEventAsync(eventId);

            // Assert
            Assert.Equal(2, operations.Count);
            Assert.Equal("repository", operations[0]);
            Assert.Equal("cache", operations[1]);
        }

        [Fact]
        public async Task DeleteEvent_WithCorrectId_ShouldInvalidateCacheOnly()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var cachedEvent = new Event(
                eventId,
                "Test Event",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                50
            );

            _cacheServiceMock
                .Setup(cache => cache.GetById(eventId))
                .ReturnsAsync(cachedEvent);

            _eventRepositoryMock
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(true);

            _cacheServiceMock
                .Setup(cache => cache.DeleteById(eventId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId);

            // Assert
            Assert.True(result);

            _cacheServiceMock.Verify(cache => cache.DeleteById(eventId), Times.Once);

            _eventRepositoryMock.Verify(repo => repo.DeleteAsync(eventId), Times.Once);
        }
    }
}

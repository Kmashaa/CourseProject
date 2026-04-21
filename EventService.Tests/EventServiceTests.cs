using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Models;
using Moq;

namespace EventService.Tests
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _repositoryMock;
        private readonly Mock<IEventDtoMapperService> _mapperMock;
        private readonly CourseProject.Services.EventService _service;

        public EventServiceTests()
        {
            _repositoryMock = new Mock<IEventRepository>();
            _mapperMock = new Mock<IEventDtoMapperService>();

            _service = new CourseProject.Services.EventService(_repositoryMock.Object, _mapperMock.Object);
        }

        [Fact]
        public void GetAllEvents_ReturnsAllEvents()
        {
            // Arrange
            var expectedEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)}
                };

            _repositoryMock.Setup(repo => repo.GetAll()).Returns(expectedEvents);

            // Act
            var result = _service.GetAllEvents();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Test Event 1", result[0].Title);
        }

        [Fact]
        public void GetEventById_ExistedId_ReturnsEventsWithThisId()
        {
            // Arrange
            var expectedEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            _repositoryMock.Setup(repo => repo.GetById(expectedEvent.Id)).Returns(expectedEvent);

            // Act
            var result = _service.GetEventById(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);
        }

        [Fact]
        public void GetEventById_NotExistedId_ReturnsNull()
        {
            // Arrange
            var notExistedGuid = Guid.NewGuid();
            _repositoryMock.Setup(repo => repo.GetById(notExistedGuid)).Returns((Event?)null);

            // Act
            var result = _service.GetEventById(notExistedGuid);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateEvent_WithCorrectData_ShouldCallRepositoryCreate()
        {
            // Arrange
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };
            _repositoryMock.Setup(repo => repo.Create(newEvent)).Returns(newEvent);

            // Act
            var result = _service.CreateEvent(newEvent);

            // Assert
            Assert.Equal(newEvent, result);
            _repositoryMock.Verify(repo => repo.Create(It.IsAny<Event>()), Times.Once);
            _repositoryMock.Verify(repo => repo.Create(It.Is<Event>(e => e.Title == "Test Event 1")), Times.Once);

        }

        [Fact]
        public void CreateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 1, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 0, 0, 0)
            };

            // Act Assert
            var exception = Assert.Throws<InvalidEventDataException>(() => _service.CreateEvent(newEvent));
        }

        [Fact]
        public void UpdateEvent_WithCorrectData_ReturnsUpdatedEvent()
        {
            // Arrange
            var eventToUpdate = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };
            _repositoryMock.Setup(repo => repo.Update(eventToUpdate)).Returns(eventToUpdate);

            // Act
            var result = _service.UpdateEvent(eventToUpdate);

            // Assert
            Assert.Equal(result, eventToUpdate);
            _repositoryMock.Verify(repo => repo.Update(It.Is<Event>(e => e.Title == "Test Event 1")), Times.Once);
            _repositoryMock.Verify(repo => repo.Update(eventToUpdate), Times.Once);
        }

        [Fact]
        public void UpdateEvent_WithIncorrectData_ThrowsException()
        {
            // Arrange
            var eventToUpdate = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 8, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };


            // Act Assert
            var exception = Assert.Throws<InvalidEventDataException>(() => _service.UpdateEvent(eventToUpdate));
        }

        [Fact]
        public void DeleteEvent_WithCorrectId_ReturnsId()
        {
            // Arrange
            Guid eventIdToDelete = Guid.NewGuid();
            _repositoryMock.Setup(repo => repo.Delete(eventIdToDelete)).Returns(eventIdToDelete);

            // Act
            var result = _service.DeleteEvent(eventIdToDelete);

            // Assert
            Assert.Equal(result, eventIdToDelete);
            _repositoryMock.Verify(repo => repo.Delete(eventIdToDelete), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithIncorrectId_ThrowsException()
        {
            // Arrange
            Guid? eventId = null;

            // Act Assert
            var exception = Assert.Throws<InvalidEventDataException>(() => _service.DeleteEvent(eventId));
        }

        [Fact]
        public void GetEvents_FilterByTitle_ReturnsFilteredResults()
        {
            // Arrange
            var filter = new EventFilter
            {
                Title = "Test"
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.Events.Count);
            Assert.All(result.Events, dto => Assert.Contains(filter.Title, dto.Title, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetEvents_FilterByStartDate_ReturnsFilteredResults()
        {
            // Arrange
            var filter = new EventFilter
            {
                From = new DateTime(2026, 4, 13, 1, 0, 0)
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.TotalItems);
            Assert.Equal(7, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o=>o.Title== "Test Event 1")?.Id);
            Assert.All(result.Events, dto =>
            {
                // Находим исходное событие по Id, чтобы сверить дату
                var originalEvent = allEvents.First(e => e.Id == dto.Id);
                Assert.True(originalEvent.StartAt >= filter.From);
            });
        }

        [Fact]
        public void GetEvents_FilterByEndDate_ReturnsFilteredResults()
        {
            // Arrange
            var filter = new EventFilter
            {
                To = new DateTime(2026, 4, 13, 0, 30, 0)
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

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
        public void GetEvents_DefaultPagination_ReturnsFirst10Results()
        {
            // Arrange
            var filter = new EventFilter
            {
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.Events.Count);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public void GetEvents_PaginationPage2_ReturnsTheSecondPage()
        {
            // Arrange
            var filter = new EventFilter
            {
                Page = 2
            };


            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(6, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 16")?.Id);
        }

        [Fact]
        public void GetEvents_PaginationPageSize2_Returns2Items()
        {
            // Arrange
            var filter = new EventFilter
            {
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 2")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 3")?.Id);
        }

        [Fact]
        public void GetEvents_PaginationPage2PageSize2_Returns2ItemsFromSecondPage()
        {
            // Arrange
            var filter = new EventFilter
            {
                Page = 2,
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 3")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 2")?.Id);
        }

        [Fact]
        public void GetEvents_CombinedFiltration_ReturnsFirst10Results()
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
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 5", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 6", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 7", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 8", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 9", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 10", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 11", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 12", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 13", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 14", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 15", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = Guid.NewGuid(), Title = "Test Event 16", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems);
            Assert.Equal(1, result.Events.Count);
            Assert.Contains(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 14")?.Id);
            Assert.DoesNotContain(result.Events, x => x.Id == allEvents.FirstOrDefault(o => o.Title == "Test Event 4")?.Id);
        }
    }
}

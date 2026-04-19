using CourseProject.Data;
using CourseProject.Entities;
using CourseProject.Interfaces;
using CourseProject.Models;
using CourseProject.Services;
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)}
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
                Id = 1,
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            _repositoryMock.Setup(repo => repo.GetById(1)).Returns(expectedEvent);

            // Act
            var result = _service.GetEventById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Id, result.Id);
        }

        [Fact]
        public void GetEventById_NotExistedId_ReturnsNull()
        {
            // Arrange
            _repositoryMock.Setup(repo => repo.GetById(999)).Returns((Event?)null);

            // Act
            var result = _service.GetEventById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateEvent_WithCorrectData_ShouldCallRepositoryCreate()
        {
            // Arrange
            var newEvent = new Event
            {
                Id = 1,
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            // Act
            _service.CreateEvent(newEvent);

            // Assert
            _repositoryMock.Verify(repo => repo.Create(It.IsAny<Event>()), Times.Once);
            _repositoryMock.Verify(repo => repo.Create(It.Is<Event>(e => e.Title == "Test Event 1")), Times.Once);

        }

        [Fact]
        public void UpdateEvent_WithCorrectData_ShouldCallRepositoryUpdate()
        {
            // Arrange
            var eventToUpdate = new Event
            {
                Id = 1,
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            // Act
            _service.UpdateEvent(eventToUpdate);

            // Assert
            _repositoryMock.Verify(repo => repo.Update(It.Is<Event>(e => e.Title == "Test Event 1")), Times.Once);
            _repositoryMock.Verify(repo => repo.Update(eventToUpdate), Times.Once);
        }

        [Fact]
        public void UpdateEvent_WithIncorrectData_ShouldCallRepositoryUpdat()
        {
            // Arrange
            var eventToUpdate = new Event
            {
                Id = -1,
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 8, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            // Act
            _service.UpdateEvent(eventToUpdate);

            // Assert
            _repositoryMock.Verify(repo => repo.Update(It.Is<Event>(e => e.Title == "Test Event 1")), Times.Once);
            _repositoryMock.Verify(repo => repo.Update(eventToUpdate), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithCorrectId_ShouldCallRepositoryDelete()
        {
            // Arrange
            int eventIdToDelete = 5;

            // Act
            _service.DeleteEvent(eventIdToDelete);

            // Assert
            _repositoryMock.Verify(repo => repo.Delete(eventIdToDelete), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithIncorrectId_ShouldStillCallRepository()
        {
            // Arrange
            int invalidId = -1;

            // Act
            _service.DeleteEvent(invalidId);

            // Assert
            _repositoryMock.Verify(repo => repo.Delete(invalidId), Times.Once);
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description=src.Description, StartAt=src.StartAt, EndAt=src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.EventsDto.Count);
            Assert.All(result.EventsDto, dto => Assert.Contains(filter.Title, dto.Title, StringComparison.OrdinalIgnoreCase));
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.TotalItems);
            Assert.Equal(7, result.EventsDto.Count);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 1);
            Assert.All(result.EventsDto, dto =>
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.TotalItems);
            Assert.Equal(8, result.EventsDto.Count);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 16);
            Assert.All(result.EventsDto, dto =>
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(10, result.EventsDto.Count);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 16);
        }

        [Fact]
        public void GetEvents_PaginationPage2_ReturnsTheSecondPage()
        {
            // Arrange
            var filter = new EventFilter
            {
                Page=2
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(6, result.EventsDto.Count);
            Assert.Contains(result.EventsDto, x => x.Id == 16);
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
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.EventsDto.Count);
            Assert.Contains(result.EventsDto, x => x.Id == 2);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 3);
        }

        [Fact]
        public void GetEvents_PaginationPage2PageSize2_Returns2ItemsFromSecondPage()
        {
            // Arrange
            var filter = new EventFilter
            {
                Page=2,
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(16, result.TotalItems);
            Assert.Equal(2, result.EventsDto.Count);
            Assert.Contains(result.EventsDto, x => x.Id == 3);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 2);
        }

        [Fact]
        public void GetEvents_CombinedFiltration_ReturnsFirst10Results()
        {
            // Arrange
            var filter = new EventFilter
            {
                Title="4",
                Page = 2,
                PageSize = 2
            };

            var allEvents = new List<Event>
                {
                    new Event { Id = 1, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 5, 0, 0, 0), EndAt=new DateTime(2026, 4, 5, 1, 0, 0) },
                    new Event { Id = 2, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 6, 0, 0, 0), EndAt=new DateTime(2026, 4, 6, 1, 0, 0)},
                    new Event { Id = 3, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 7, 0, 0, 0), EndAt=new DateTime(2026, 4, 7, 1, 0, 0) },
                    new Event { Id = 4, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 8, 0, 0, 0), EndAt=new DateTime(2026, 4, 8, 1, 0, 0)},
                    new Event { Id = 5, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 9, 0, 0, 0), EndAt=new DateTime(2026, 4, 9, 1, 0, 0) },
                    new Event { Id = 6, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 10, 0, 0, 0), EndAt=new DateTime(2026, 4, 10, 1, 0, 0)},
                    new Event { Id = 7, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 11, 0, 0, 0), EndAt=new DateTime(2026, 4, 11, 1, 0, 0) },
                    new Event { Id = 8, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 12, 0, 0, 0), EndAt=new DateTime(2026, 4, 12, 1, 0, 0)},
                    new Event { Id = 9, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 13, 0, 0, 0), EndAt=new DateTime(2026, 4, 13, 1, 0, 0) },
                    new Event { Id = 10, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 14, 0, 0, 0), EndAt=new DateTime(2026, 4, 14, 1, 0, 0)},
                    new Event { Id = 11, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 15, 0, 0, 0), EndAt=new DateTime(2026, 4, 15, 1, 0, 0) },
                    new Event { Id = 12, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 16, 0, 0, 0), EndAt=new DateTime(2026, 4, 16, 1, 0, 0)},
                    new Event { Id = 13, Title = "Test Event 1", StartAt=new DateTime(2026, 4, 17, 0, 0, 0), EndAt=new DateTime(2026, 4, 17, 1, 0, 0) },
                    new Event { Id = 14, Title = "Test Event 2", StartAt=new DateTime(2026, 4, 18, 0, 0, 0), EndAt=new DateTime(2026, 4, 18, 1, 0, 0)},
                    new Event { Id = 15, Title = "Test Event 3", StartAt=new DateTime(2026, 4, 19, 0, 0, 0), EndAt=new DateTime(2026, 4, 19, 1, 0, 0) },
                    new Event { Id = 16, Title = "Test Event 4", StartAt=new DateTime(2026, 4, 20, 0, 0, 0), EndAt=new DateTime(2026, 4, 20, 1, 0, 0)}

                };

            _repositoryMock.Setup(r => r.GetAll()).Returns(allEvents);

            _mapperMock.Setup(m => m.EntityToDto(It.IsAny<Event>()))
                       .Returns((Event src) => new EventDto { Id = src.Id, Title = src.Title, Description = src.Description, StartAt = src.StartAt, EndAt = src.EndAt });

            // Act
            var result = _service.GetEvents(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalItems);
            Assert.Equal(2, result.EventsDto.Count);
            Assert.Contains(result.EventsDto, x => x.Id == 12);
            Assert.Contains(result.EventsDto, x => x.Id == 16);
            Assert.DoesNotContain(result.EventsDto, x => x.Id == 4);
        }
    }
}

using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Services;
using Moq;

namespace CourseProject.Tests
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingsRepositoryMock;
        private readonly Mock<IEventService> _eventsServiceMock;


        private readonly BookingService _service;

        public BookingServiceTests()
        {
            _bookingsRepositoryMock = new Mock<IBookingRepository>();
            _eventsServiceMock = new Mock<IEventService>();

            _service = new BookingService(_bookingsRepositoryMock.Object, _eventsServiceMock.Object);
        }

        [Fact]
        public async Task CreateNewBooking_SuccessfullyCreatedWithPendingStatus()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();
            var existingEvent = new Event
            {
                Id = eventGuid,
                Title = "Test Event 1",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
            };

            _eventsServiceMock.Setup(repo => repo.GetEventById(eventGuid)).Returns(existingEvent);

            var bookingGuid = Guid.NewGuid();
            var newBooking = new Booking
            {
                Id = bookingGuid,
                EventId = eventGuid,
                CreatedAt = new DateTime(2026, 4, 5, 0, 0, 0),
                Status = BookingStatus.Pending
            };

            _bookingsRepositoryMock.Setup(repo => repo.CreateAsync(eventGuid)).ReturnsAsync(newBooking);

            // Act
            var result = await _service.CreateBookingAsync(eventGuid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newBooking, result);
            Assert.Equal(BookingStatus.Pending, result.Status);

        }

        [Fact]
        public async Task CreateBooking_WhenEventIdIsNull_ThrowsInvalidEventDataException()
        {
            // Arrange
            Guid? nullEventId = null;

            // Act Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(async () =>
            {
                await _service.CreateBookingAsync(nullEventId);
            });
        }

        [Fact]
        public async Task CreateBooking_WhenEventDoesNotExist_ThrowsEventNotFoundException()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();

            _eventsServiceMock.Setup(service => service.GetEventById(nonExistentEventId)).Returns((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _service.CreateBookingAsync(nonExistentEventId);
            });

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Guid>()), Times.Never);
        }


        [Fact]
        public async Task CreateBooking_WhenEventWasCreatedAndThenDeleted_ThrowsEventNotFoundException()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();

            var localEvents = new List<Event>
            {
                new Event
                {
                    Id = eventGuid,
                    Title = "Test Event 1",
                    StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                    EndAt = new DateTime(2026, 4, 5, 1, 0, 0)
                }
            };

            _eventsServiceMock.Setup(service => service.GetEventById(eventGuid)).Returns(() => localEvents.FirstOrDefault(e => e.Id == eventGuid));


            var eventToDelete = localEvents.FirstOrDefault(e => e.Id == eventGuid);
            if (eventToDelete != null)
            {
                localEvents.Remove(eventToDelete);
            }


            // Act assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _service.CreateBookingAsync(eventGuid);
            });

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingDoesNotExist_ReturnsNull()
        {
            // Arrange
            var nonExistingBookingId = Guid.NewGuid();

            _bookingsRepositoryMock.Setup(repo => repo.GetByIdAsync(nonExistingBookingId)).ReturnsAsync((Booking?)null);

            // Act
            var result = await _service.GetBookingByIdAsync(nonExistingBookingId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingExists_ReturnsCorrectBookingData()
        {
            // Arrange
            var targetBookingId = Guid.NewGuid();
            var associatedEventId = Guid.NewGuid();
            var bookingCreationTime = new DateTime(2026, 4, 5, 12, 0, 0);

            var expectedBooking = new Booking
            {
                Id = targetBookingId,
                EventId = associatedEventId,
                CreatedAt = bookingCreationTime,
                Status = BookingStatus.Confirmed
            };

            _bookingsRepositoryMock.Setup(repo => repo.GetByIdAsync(targetBookingId)).ReturnsAsync(expectedBooking);

            // Act
            var result = await _service.GetBookingByIdAsync(targetBookingId);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(expectedBooking.Id, result.Id);
            Assert.Equal(expectedBooking.EventId, result.EventId);
            Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);
            Assert.Equal(expectedBooking.Status, result.Status);
        }

        [Fact]
        public async Task GetBookingById_ReflectsStatusChange_AfterBackgroundProcessing()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                EventId = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                Status = BookingStatus.Pending 
            };

            var localBookings = new List<Booking> { booking };

            _bookingsRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(() => localBookings);
            _bookingsRepositoryMock.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(() => localBookings.FirstOrDefault(b => b.Id == bookingId));
            _bookingsRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);

            var processingService = new BookingProcessingService(_bookingsRepositoryMock.Object);

            using var cts = new CancellationTokenSource();

            // Act
            var processingTask = processingService.StartAsync(cts.Token);

            await Task.Delay(TimeSpan.FromSeconds(10));

            cts.Cancel();

            try { await processingTask; } catch (OperationCanceledException) { }

            var result = await _service.GetBookingByIdAsync(bookingId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt);
        }

        [Fact]
        public async Task CreateMultipleBookings_ForSameEvent_AllHaveUniqueIds()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();
            var existingEvent = new Event
            {
                Id = eventGuid,
                Title = "Популярное событие",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(2)
            };

            _eventsServiceMock.Setup(service => service.GetEventById(eventGuid)).Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventGuid))
                .ReturnsAsync((Guid id) => new Booking
                {
                    Id = Guid.NewGuid(),
                    EventId = id,
                    CreatedAt = DateTime.UtcNow,
                    Status = BookingStatus.Pending
                });

            // Act
            var booking1 = await _service.CreateBookingAsync(eventGuid);
            var booking2 = await _service.CreateBookingAsync(eventGuid);
            var booking3 = await _service.CreateBookingAsync(eventGuid);

            // Assert
            Assert.NotNull(booking1);
            Assert.NotNull(booking2);
            Assert.NotNull(booking3);

            Assert.Equal(eventGuid, booking1.EventId);
            Assert.Equal(eventGuid, booking2.EventId);
            Assert.Equal(eventGuid, booking3.EventId);

            Assert.NotEqual(booking1.Id, booking2.Id);
            Assert.NotEqual(booking1.Id, booking3.Id);
            Assert.NotEqual(booking2.Id, booking3.Id);
        }
    }

}

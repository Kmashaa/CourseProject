using CourseProject.DataAccess;
using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace CourseProject.Tests
{
    public class BookingServiceTests : IDisposable
    {
        private readonly Mock<IEventService> _eventServiceMock;
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
        private readonly IBookingService _bookingService;

        public BookingServiceTests()
        {
            _eventServiceMock = new Mock<IEventService>();
            _bookingRepositoryMock = new Mock<IBookingRepository>();

            _bookingService = new BookingService(_eventServiceMock.Object, _bookingRepositoryMock.Object);
        }



        public void Dispose()
        {
        }

        [Fact]
        public async Task CreateNewBooking_SuccessfullyCreatedWithPendingStatus()
        {
            // Arrange
            var existingEvent = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                50
            );
            var eventGuid = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventGuid))
                .ReturnsAsync(existingEvent);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) => b);

            // Act
            var result = await _bookingService.CreateBookingAsync(eventGuid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(eventGuid, result.EventId);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventGuid), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_WhenEventIdIsNull_ThrowsInvalidEventDataException()
        {
            // Arrange
            Guid? nullEventId = null;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(async () =>
            {
                await _bookingService.CreateBookingAsync(nullEventId);
            });

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(It.IsAny<Guid>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WhenEventDoesNotExist_ThrowsEventNotFoundException()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(nonExistentEventId))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(nonExistentEventId);
            });

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WhenEventWasCreatedAndThenDeleted_ThrowsEventNotFoundException()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventGuid))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventGuid);
            });

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingDoesNotExist_ReturnsNull()
        {
            // Arrange
            var nonExistingBookingId = Guid.NewGuid();

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistingBookingId))
                .ReturnsAsync((Booking?)null);

            // Act
            var result = await _bookingService.GetBookingByIdAsync(nonExistingBookingId);

            // Assert
            Assert.Null(result);

            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistingBookingId), Times.Once);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingExists_ReturnsCorrectBookingData()
        {
            // Arrange
            var targetBookingId = Guid.NewGuid();
            var associatedEventId = Guid.NewGuid();
            var bookingCreationTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

            var expectedBooking = new Booking(
                id: targetBookingId,
                eventId: associatedEventId,
                status: BookingStatus.Confirmed,
                createdAt: bookingCreationTime
            );

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(targetBookingId))
                .ReturnsAsync(expectedBooking);

            // Act
            var result = await _bookingService.GetBookingByIdAsync(targetBookingId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedBooking.Id, result.Id);
            Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);

            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(targetBookingId), Times.Once);
        }

        [Fact]
        public async Task GetBookingById_ReflectsStatusChange_AfterBackgroundProcessing()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var targetBookingId = Guid.NewGuid();

            var existingEvent = Event.Create(
                "Test Event",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.Id = eventId;

            var booking = new Booking(
                id: targetBookingId,
                eventId: eventId,
                status: BookingStatus.Pending,
                createdAt: DateTime.UtcNow
            );

            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var loggerMock = new Mock<ILogger<BookingProcessingService>>();

            scopeFactoryMock
                .Setup(factory => factory.CreateScope())
                .Returns(scopeMock.Object);

            scopeMock
                .Setup(scope => scope.ServiceProvider)
                .Returns(serviceProviderMock.Object);

            serviceProviderMock
                .Setup(provider => provider.GetService(typeof(IBookingRepository)))
                .Returns(bookingRepositoryMock.Object);

            serviceProviderMock
                .Setup(provider => provider.GetService(typeof(IEventRepository)))
                .Returns(eventRepositoryMock.Object);

            bookingRepositoryMock
                .Setup(repo => repo.GetPendingsAsync())
                .ReturnsAsync(new List<Guid> { targetBookingId });

            bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(targetBookingId))
                .ReturnsAsync(booking);

            eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            bookingRepositoryMock
                .Setup(repo => repo.UpdateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) => b);

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(targetBookingId))
                .ReturnsAsync(booking);

            var processingService = new BookingProcessingService(
                scopeFactoryMock.Object,
                loggerMock.Object
            );

            using var cts = new CancellationTokenSource();

            // Act
            var processingTask = processingService.StartAsync(cts.Token);

            await Task.Delay(TimeSpan.FromSeconds(6));

            cts.Cancel();

            try
            {
                await processingTask;
            }
            catch (OperationCanceledException)
            {
            }

            booking.Confirm();

            var result = await _bookingService.GetBookingByIdAsync(targetBookingId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetBookingId, result.Id);
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt);

            bookingRepositoryMock.Verify(repo => repo.GetPendingsAsync(), Times.AtLeastOnce);
            bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(targetBookingId), Times.AtLeastOnce);
            eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.AtLeastOnce);
            bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateMultipleBookings_ForSameEvent_AllHaveUniqueIds()
        {
            // Arrange
            var existingEvent = Event.Create(
                "Популярное событие",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(2),
                50
            );
            var eventGuid = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventGuid))
                .ReturnsAsync(existingEvent);

            var bookingCounter = 0;
            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) =>
                {
                    var newBooking = new Booking(
                        id: Guid.NewGuid(),
                        eventId: b.EventId,
                        status: b.Status,
                        createdAt: b.CreatedAt
                    );
                    return newBooking;
                });

            // Act
            var booking1 = await _bookingService.CreateBookingAsync(eventGuid);
            var booking2 = await _bookingService.CreateBookingAsync(eventGuid);
            var booking3 = await _bookingService.CreateBookingAsync(eventGuid);

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

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(3));

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventGuid), Times.Exactly(3));
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreAvailable_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange 
            const int initialSeats = 50;

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                initialSeats
            );
            var eventId = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) => b);

            // Act 
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);

            var expectedAvailableSeats = initialSeats - 1;
            Assert.Equal(expectedAvailableSeats, existingEvent.AvailableSeats);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(
                It.Is<Booking>(b =>
                    b.EventId == eventId &&
                    b.Status == BookingStatus.Pending
                )
            ), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllShouldBeSuccessfulWithUniqueIds()
        {
            // Arrange 
            const int totalSeatsLimit = 3;

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 14, 19, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 14, 21, 30, 0, DateTimeKind.Utc),
                totalSeatsLimit
            );
            var eventId = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) => b);

            var createdBookings = new List<Booking>();

            // Act 
            for (int i = 0; i < totalSeatsLimit; i++)
            {
                var booking = await _bookingService.CreateBookingAsync(eventId);
                Assert.NotNull(booking);
                createdBookings.Add(booking);
            }

            // Assert 
            Assert.Equal(totalSeatsLimit, createdBookings.Count);

            var uniqueIdsCount = createdBookings.Select(b => b.Id).Distinct().Count();
            Assert.Equal(totalSeatsLimit, uniqueIdsCount);

            Assert.Equal(0, existingEvent.AvailableSeats);

            Assert.All(createdBookings, b => Assert.Equal(eventId, b.EventId));

            Assert.All(createdBookings, b => Assert.Equal(BookingStatus.Pending, b.Status));

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(totalSeatsLimit));
            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Exactly(totalSeatsLimit));
        }

        [Fact]
        public async Task CreateBookingAsync_FirstBookingSucceeds_SecondBookingThrowsNoAvailableSeatsException()
        {
            // Arrange
            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.AvailableSeats = 1;
            var eventId = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .ReturnsAsync((Booking b) => b);

            // Act & Assert
            var firstBooking = await _bookingService.CreateBookingAsync(eventId);
            Assert.NotNull(firstBooking);
            Assert.Equal(0, existingEvent.AvailableSeats);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _bookingService.CreateBookingAsync(eventId)
            );

            Assert.Equal(0, existingEvent.AvailableSeats);

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                50
            );
            existingEvent.AvailableSeats = 0;
            var eventId = existingEvent.Id;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _bookingService.CreateBookingAsync(eventId)
            );

            Assert.Equal(0, existingEvent.AvailableSeats);

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventDoesNotExist_ShouldThrowEventNotFoundException()
        {
            // Arrange
            var nonExistingEventId = Guid.NewGuid();

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(nonExistingEventId))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(nonExistingEventId)
            );

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(nonExistingEventId), Times.Once);
        }

        public void Confirm_WhenCalled_ShouldSetStatusToConfirmedAndPopulateProcessedAt()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            var createdAt = DateTime.UtcNow.AddMinutes(-5);

            var booking = new Booking(
                id: bookingId,
                eventId: eventId,
                status: BookingStatus.Pending,
                createdAt: createdAt
            );

            var testStartTime = DateTime.UtcNow;

            // Act
            booking.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);

            Assert.NotNull(booking.ProcessedAt);

            Assert.True(booking.ProcessedAt >= testStartTime);
            Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task Reject_WhenCalled_ShouldSetStatusToRejectedAndPopulateProcessedAt()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var booking = Booking.CreatePending(eventId);

            var testStartTime = DateTime.Now;

            await Task.Delay(TimeSpan.FromSeconds(2));

            // Act 
            booking.Reject();

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);

            Assert.NotNull(booking.ProcessedAt);

            Assert.True(booking.ProcessedAt >= testStartTime, "ProcessedAt should be set to current time 1 (UTC)");
            Assert.True(booking.ProcessedAt <= DateTime.Now, "ProcessedAt should be set to current time 2 (UTC)");
        }

        [Fact]
        public void RejectAndReleaseSeats_ShouldSetStatusToRejectedAndRestoreAvailableSeats()
        {
            // Arrange 
            var eventId = Guid.NewGuid();
            const int totalSeats = 10;

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                totalSeats
            );
            existingEvent.Id = eventId;

            existingEvent.AvailableSeats = 9;

            var booking = new Booking(
                id: Guid.NewGuid(),
                eventId: eventId,
                status: BookingStatus.Pending,
                createdAt: DateTime.UtcNow
            );

            // Act 
            booking.Reject();
            existingEvent.ReleaseSeats();

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);

            Assert.Equal(totalSeats, existingEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_AfterRejectAndReleaseSeats_ShouldAllowToBookTheReleasedSeatSuccessfully()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.Id = eventId;
            existingEvent.AvailableSeats = 1;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            var createdBookings = new List<Booking>();
            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => createdBookings.Add(b))
                .ReturnsAsync((Booking b) => b);

            // Act & Assert
            var result1 = await _bookingService.CreateBookingAsync(eventId);
            Assert.NotNull(result1);
            Assert.Equal(0, existingEvent.AvailableSeats);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _bookingService.CreateBookingAsync(eventId)
            );

            result1.Reject();
            existingEvent.ReleaseSeats(1);
            Assert.Equal(1, existingEvent.AvailableSeats);

            var result2 = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result2);
            Assert.NotEqual(result1.Id, result2.Id);
            Assert.Equal(0, existingEvent.AvailableSeats);

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(2));

            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(
                It.Is<Booking>(b => b.EventId == eventId)
            ), Times.Exactly(2));

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Exactly(3));
        }

        [Fact]
        public async Task CreateBookingAsync_ConcurrentRequests_ShouldAllowExactlyMaxSeatsAndThrowForRest()
        {
            // Arrange 
            var eventId = Guid.NewGuid();
            const int availableSeatsCount = 5;
            const int totalRequestsCount = 20;

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.Id = eventId;
            existingEvent.AvailableSeats = availableSeatsCount;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(() => existingEvent);

            var createdBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => createdBookings.Add(b))
                .ReturnsAsync((Booking b) => b);

            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = new Task[totalRequestsCount];

            // Act 
            for (int i = 0; i < totalRequestsCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        var booking = await _bookingService.CreateBookingAsync(eventId);
                        if (booking != null)
                        {
                            successfulBookings.Add(booking);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(availableSeatsCount, successfulBookings.Count);

            var expectedExceptionsCount = totalRequestsCount - availableSeatsCount;
            Assert.Equal(expectedExceptionsCount, exceptions.Count);

            Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));

            Assert.Equal(0, existingEvent.AvailableSeats);

            Assert.Equal(availableSeatsCount, createdBookings.Count);

            Assert.All(createdBookings, b => Assert.Equal(eventId, b.EventId));

            var uniqueIds = createdBookings.Select(b => b.Id).Distinct().Count();
            Assert.Equal(availableSeatsCount, uniqueIds);

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Exactly(totalRequestsCount));
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(availableSeatsCount));
        }

        [Fact]
        public async Task CreateBookingAsync_ConcurrentRequests_ShouldGenerateUniqueBookingIds()
        {
            // Arrange 
            var eventId = Guid.NewGuid();
            const int availableSeatsCount = 5;
            const int totalRequestsCount = 5;

            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.Id = eventId;
            existingEvent.AvailableSeats = availableSeatsCount;

            _eventServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            var createdBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => createdBookings.Add(b))
                .ReturnsAsync((Booking b) => b);

            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = new Task[totalRequestsCount];

            // Act 
            for (int i = 0; i < totalRequestsCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        var booking = await _bookingService.CreateBookingAsync(eventId);
                        if (booking != null)
                        {
                            successfulBookings.Add(booking);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(availableSeatsCount, successfulBookings.Count);

            Assert.Empty(exceptions);

            Assert.Equal(0, existingEvent.AvailableSeats);

            Assert.Equal(availableSeatsCount, createdBookings.Count);

            var bookingIds = createdBookings.Select(b => b.Id).ToList();
            var uniqueIdsCount = bookingIds.Distinct().Count();
            Assert.Equal(availableSeatsCount, uniqueIdsCount);

            Assert.All(createdBookings, b => Assert.Equal(eventId, b.EventId));

            _eventServiceMock.Verify(service => service.GetEventByIdAsync(eventId), Times.Exactly(totalRequestsCount));
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(availableSeatsCount));
        }


    }

}

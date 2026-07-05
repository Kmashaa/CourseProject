using CourseProject.Entities;
using CourseProject.Exceptions;
using CourseProject.Interfaces;
using CourseProject.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CourseProject.Tests
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingsRepositoryMock;
        private readonly Mock<IEventRepository> _eventsRepositoryMock;
        private readonly Mock<ILogger<BookingProcessingService>> _logger;

        private readonly Mock<IEventService> _eventsServiceMock;


        private readonly BookingService _service;

        public BookingServiceTests()
        {
            _bookingsRepositoryMock = new Mock<IBookingRepository>();
            _eventsRepositoryMock = new Mock<IEventRepository>();
            _logger = new Mock<ILogger<BookingProcessingService>>();

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
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0),
                TotalSeats = 50,
                AvailableSeats=50
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
                    EndAt = new DateTime(2026, 4, 5, 1, 0, 0),
                    TotalSeats = 50,
                    AvailableSeats=50
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
            var eventId= Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                EventId = eventId,
                CreatedAt = DateTime.Now,
                Status = BookingStatus.Pending
            };
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test Event",
                StartAt = new DateTime(2026, 4, 5, 0, 0, 0),
                EndAt = new DateTime(2026, 4, 5, 1, 0, 0),
                TotalSeats = 10,
                AvailableSeats = 10
            };
            var localBookings = new List<Booking> { booking };

            _bookingsRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(() => localBookings);
            _bookingsRepositoryMock.Setup(repo => repo.GetByIdAsync(bookingId)).ReturnsAsync(() => localBookings.FirstOrDefault(b => b.Id == bookingId));
            _bookingsRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);
            _bookingsRepositoryMock.Setup(repo => repo.GetPendingsAsync()).ReturnsAsync(() => localBookings.Where(b => b.Status == BookingStatus.Pending).ToList());
            _eventsRepositoryMock.Setup(repo => repo.GetById(eventId)).Returns(existingEvent);


            var processingService = new BookingProcessingService(_bookingsRepositoryMock.Object, _eventsRepositoryMock.Object, _logger.Object);

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
                EndAt = DateTime.UtcNow.AddHours(2),
                TotalSeats = 50,
                AvailableSeats = 50
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

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreAvailable_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange (Подготовка данных)
            var eventId = Guid.NewGuid();
            const int initialSeats = 50;

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = initialSeats,
                AvailableSeats = initialSeats
            };

            var expectedBooking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(expectedBooking);

            // Act 
            var result = await _service.CreateBookingAsync(eventId);

            // Assert 
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);

            var expectedAvailableSeats = initialSeats - 1;
            Assert.Equal(expectedAvailableSeats, existingEvent.AvailableSeats);

            _eventsServiceMock.Verify(service =>
                service.UpdateEvent(It.Is<Event>(e => e.Id == eventId && e.AvailableSeats == expectedAvailableSeats)),
                Times.Once);

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllShouldBeSuccessfulWithUniqueIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int totalSeatsLimit = 3; 

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 14, 19, 0, 0),
                EndAt = new DateTime(2026, 9, 14, 21, 30, 0),
                TotalSeats = totalSeatsLimit,
                AvailableSeats = totalSeatsLimit 
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(() => new Booking
                {
                    Id = Guid.NewGuid(), 
                    EventId = eventId,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.Now
                });

            var createdBookings = new List<Booking>();

            // Act
            for (int i = 0; i < totalSeatsLimit; i++)
            {
                var booking = await _service.CreateBookingAsync(eventId);

                Assert.NotNull(booking);
                createdBookings.Add(booking);
            }

            // Assert

            Assert.Equal(totalSeatsLimit, createdBookings.Count);

            Assert.Equal(0, existingEvent.AvailableSeats);

            var uniqueIdsCount = createdBookings.Select(b => b.Id).Distinct().Count();
            Assert.Equal(totalSeatsLimit, uniqueIdsCount);

            _eventsServiceMock.Verify(service => service.UpdateEvent(existingEvent), Times.Exactly(totalSeatsLimit));

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Exactly(totalSeatsLimit));
        }

        [Fact]
        public async Task CreateBookingAsync_FirstBookingSucceeds_SecondBookingThrowsNoAvailableSeatsException()
        {
            // Arrange (Подготовка данных)
            var eventId = Guid.NewGuid();

            // Создаем событие, у которого изначально доступно ровно 1 место
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = 10,
                AvailableSeats = 1 
            };

            var expectedBooking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(expectedBooking);


            var firstBooking = await _service.CreateBookingAsync(eventId);

            Assert.NotNull(firstBooking);
            Assert.Equal(0, existingEvent.AvailableSeats); 



            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _service.CreateBookingAsync(eventId)
            );

            Assert.Equal(0, existingEvent.AvailableSeats);

            _eventsServiceMock.Verify(service => service.UpdateEvent(existingEvent), Times.Once);

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = 50,
                AvailableSeats = 0 
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _service.CreateBookingAsync(eventId)
            );

            Assert.Equal(0, existingEvent.AvailableSeats);

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Never);

            _eventsServiceMock.Verify(service => service.UpdateEvent(It.IsAny<Event>()), Times.Never);
        }


        [Fact]
        public async Task CreateBookingAsync_WhenEventDoesNotExist_ShouldThrowEventNotFoundException()
        {
            // Arrange 
            var nonExistingEventId = Guid.NewGuid();

            _eventsServiceMock
                .Setup(service => service.GetEventById(nonExistingEventId))
                .Returns((Event?)null);

            // Act & Assert 
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
                await _service.CreateBookingAsync(nonExistingEventId)
            );

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Guid>()), Times.Never);
            _eventsServiceMock.Verify(service => service.UpdateEvent(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public void Confirm_WhenCalled_ShouldSetStatusToConfirmedAndPopulateProcessedAt()
        {
            // Arrange
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                CreatedAt = DateTime.Now.AddMinutes(-5),
                Status = BookingStatus.Pending
            };

            var testStartTime = DateTime.Now;

            // Act
            booking.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);

            Assert.NotNull(booking.ProcessedAt);

            Assert.True(booking.ProcessedAt >= testStartTime);
            Assert.True(booking.ProcessedAt <= DateTime.Now);
        }

        [Fact]
        public void Reject_WhenCalled_ShouldSetStatusToRejectedAndPopulateProcessedAt()
        {
            // Arrange
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                CreatedAt = DateTime.Now.AddMinutes(-5),
                Status = BookingStatus.Pending
            };

            var testStartTime = DateTime.Now;

            // Act 
            booking.Reject();

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);

            Assert.NotNull(booking.ProcessedAt);

            Assert.True(booking.ProcessedAt >= testStartTime, "ProcessedAt should be set to current time");
            Assert.True(booking.ProcessedAt <= DateTime.Now, "ProcessedAt should be set to current time");
        }

        [Fact]
        public void RejectAndReleaseSeats_ShouldSetStatusToRejectedAndRestoreAvailableSeats()
        {
            // Arrange (Подготовка данных)
            var eventId = Guid.NewGuid();
            const int totalSeats = 10;

            // Создаем событие, на которое уже забронировали одно место (осталось 9 из 10)
            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = totalSeats,
                AvailableSeats = 9
            };

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                CreatedAt = DateTime.Now,
                Status = BookingStatus.Pending
            };

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

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = 10,
                AvailableSeats = 1
            };

            var firstBooking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            var secondBooking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .SetupSequence(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(firstBooking)
                .ReturnsAsync(secondBooking);

            // Act & Assert 

            var result1 = await _service.CreateBookingAsync(eventId);
            Assert.NotNull(result1);
            Assert.Equal(0, existingEvent.AvailableSeats);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _service.CreateBookingAsync(eventId)
            );

            result1.Reject();
            existingEvent.ReleaseSeats(1); 
            Assert.Equal(1, existingEvent.AvailableSeats); 

            var result2 = await _service.CreateBookingAsync(eventId);

            Assert.NotNull(result2);
            Assert.Equal(secondBooking.Id, result2.Id); 
            Assert.Equal(0, existingEvent.AvailableSeats);

            _eventsServiceMock.Verify(service => service.UpdateEvent(existingEvent), Times.Exactly(2));

            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateBookingAsync_ConcurrentRequests_ShouldAllowExactlyMaxSeatsAndThrowForRest()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int availableSeatsCount = 5;
            const int totalRequestsCount = 20;

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = 10,
                AvailableSeats = availableSeatsCount 
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(() => new Booking
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.Now
                });

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
                        var booking = await _service.CreateBookingAsync(eventId);
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

            _eventsServiceMock.Verify(service => service.UpdateEvent(existingEvent), Times.Exactly(availableSeatsCount));
            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Exactly(availableSeatsCount));
        }

        [Fact]
        public async Task CreateBookingAsync_ConcurrentRequests_ShouldGenerateUniqueBookingIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            const int requestsAndSeatsCount = 10;

            var existingEvent = new Event
            {
                Id = eventId,
                Title = "Test event",
                StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                EndAt = new DateTime(2026, 9, 1, 21, 0, 0),
                TotalSeats = requestsAndSeatsCount,
                AvailableSeats = requestsAndSeatsCount
            };

            _eventsServiceMock
                .Setup(service => service.GetEventById(eventId))
                .Returns(existingEvent);

            _bookingsRepositoryMock
                .Setup(repo => repo.CreateAsync(eventId))
                .ReturnsAsync(() => new Booking
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.Now
                });

            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            var tasks = new Task[requestsAndSeatsCount];

            // Act 
            for (int i = 0; i < requestsAndSeatsCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var booking = await _service.CreateBookingAsync(eventId);
                    if (booking != null)
                    {
                        successfulBookings.Add(booking);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert 

            Assert.Equal(requestsAndSeatsCount, successfulBookings.Count);

            Assert.Equal(0, existingEvent.AvailableSeats);

            var uniqueIdsCount = successfulBookings.Select(b => b.Id).Distinct().Count();
            Assert.Equal(requestsAndSeatsCount, uniqueIdsCount);

            _eventsServiceMock.Verify(service => service.UpdateEvent(existingEvent), Times.Exactly(requestsAndSeatsCount));
            _bookingsRepositoryMock.Verify(repo => repo.CreateAsync(eventId), Times.Exactly(requestsAndSeatsCount));
        }


    }

}

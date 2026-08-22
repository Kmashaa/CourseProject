//using CourseProject.Application.Exceptions;
//using CourseProject.Application.Interfaces;
//using CourseProject.Application.Models;
//using CourseProject.Application.Services;
//using CourseProject.Domain.Entities;
//using CourseProject.Domain.Exceptions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System.Net.NetworkInformation;
//using System.Reflection;

//namespace CourseProject.Tests
//{
    

//    public class BookingServiceTests : IDisposable
//    {
//        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
//        private readonly Mock<IEventRepository> _eventRepositoryMock;
//        private readonly Mock<IBookingDtoMapperService> _bookingDtoMapperServiceMock;
//        private readonly Mock<IConfiguration> _configuration;

//        private readonly IBookingService _bookingService;

//        public BookingServiceTests()
//        {
//            _bookingRepositoryMock = new Mock<IBookingRepository>();
//            _eventRepositoryMock = new Mock<IEventRepository>();
//            _bookingDtoMapperServiceMock = new Mock<IBookingDtoMapperService>();
//            _configuration = new Mock<IConfiguration>();

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");


//            _bookingService = new BookingService(_bookingRepositoryMock.Object, _eventRepositoryMock.Object, _bookingDtoMapperServiceMock.Object, _configuration.Object);
//        }



//        public void Dispose()
//        {
//        }

//        [Fact]
//        public async Task CreateNewBooking_SuccessfullyCreatedWithPendingStatus()
//        {
//            // Arrange
//            var eventGuid = Guid.NewGuid();
//            var userId = Guid.NewGuid(); 

//            var existingEvent = new Event
//            (
//                eventGuid,
//                "Test Event 1",
//                new DateTime(2027, 4, 5, 0, 0, 0, DateTimeKind.Utc),
//                new DateTime(2027, 4, 5, 1, 0, 0, DateTimeKind.Utc),
//                50
//            );

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventGuid))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId=b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            // Act
//            var result = await _bookingService.CreateBookingAsync(eventGuid, userId);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(Application.Models.BookingStatus.Pending, result.Status);
//            Assert.Equal(eventGuid, result.EventId);
//            Assert.Equal(userId, result.UserId);

//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventGuid), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
//            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);
//        }


//        [Fact]
//        public async Task CreateBooking_WhenEventIdIsNull_ThrowsInvalidEventDataException()
//        {
//            // Arrange
//            Guid? nullEventId = null;
//            var userId = Guid.NewGuid(); 

//            // Act & Assert
//            await Assert.ThrowsAsync<InvalidEventDataException>(async () =>
//            {
//                await _bookingService.CreateBookingAsync(nullEventId, userId);
//            });

//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(It.IsAny<Guid>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//        }


//        [Fact]
//        public async Task CreateBooking_WhenEventDoesNotExist_ThrowsEventNotFoundException()
//        {
//            // Arrange
//            var nonExistentEventId = Guid.NewGuid();
//            var userId = Guid.NewGuid(); 
//            Event? nullEvent = null;

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(nonExistentEventId))
//                .ReturnsAsync(nullEvent);

//            // Act & Assert
//            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
//            {
//                await _bookingService.CreateBookingAsync(nonExistentEventId, userId);
//            });

//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistentEventId), Times.Once);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//        }

//        [Fact]
//        public async Task CreateBooking_WhenEventWasCreatedAndThenDeleted_ThrowsEventNotFoundException()
//        {
//            // Arrange
//            var eventGuid = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            Event? nullEvent = null;

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventGuid))
//                .ReturnsAsync(nullEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            // Act & Assert
//            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
//            {
//                await _bookingService.CreateBookingAsync(eventGuid, userId);
//            });

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventGuid), Times.Once);
//        }

//        [Fact]
//        public async Task GetBookingById_WhenBookingDoesNotExist_ThrowsBookingNotFoundException()
//        {
//            // Arrange
//            var nonExistingBookingId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            var role = "Admin"; 
//            Booking? nullBooking = null;

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(nonExistingBookingId))
//                .ReturnsAsync(nullBooking);

//            // Act & Assert
//            await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
//            {
//                await _bookingService.GetBookingByIdAsync(nonExistingBookingId, userId, role);
//            });

//            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistingBookingId), Times.Once);
//        }

//        [Fact]
//        public async Task GetBookingById_WhenBookingExists_ReturnsCorrectBookingData()
//        {
//            // Arrange
//            var targetBookingId = Guid.NewGuid();
//            var associatedEventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            var role = "User"; 
//            var bookingCreationTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

//            var expectedBooking = new Booking(
//                id: targetBookingId,
//                eventId: associatedEventId,
//                userId: userId,
//                status: Domain.Entities.BookingStatus.Confirmed,
//                createdAt: bookingCreationTime
//            );

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(targetBookingId))
//                .ReturnsAsync(expectedBooking);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            // Act
//            var result = await _bookingService.GetBookingByIdAsync(targetBookingId, userId, role);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(expectedBooking.Id, result.Id);
//            Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);
//            Assert.Equal(Application.Models.BookingStatus.Confirmed, result.Status);
//            Assert.Equal(expectedBooking.EventId, result.EventId);
//            Assert.Equal(expectedBooking.UserId, result.UserId);

//            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(targetBookingId), Times.Once);
//            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);
//        }

//        [Fact]
//        public async Task GetBookingById_ReflectsStatusChange_AfterBackgroundProcessing()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var targetBookingId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            var userRole = "Admin";

//            var existingEvent = new Event(
//                eventId,
//                "Test Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(1),
//                10
//            );

//            var pendingBooking = new Booking(
//                id: targetBookingId,
//                eventId: eventId,
//                userId: userId,
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow
//            );

//            var confirmedBooking = new Booking(
//                id: targetBookingId,
//                eventId: eventId,
//                userId: userId,
//                status: Domain.Entities.BookingStatus.Confirmed,
//                createdAt: pendingBooking.CreatedAt
//            );
//            confirmedBooking.Confirm();

//            var bookingRepositoryMock = new Mock<IBookingRepository>();
//            var eventRepositoryMock = new Mock<IEventRepository>();
//            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
//            var scopeMock = new Mock<IServiceScope>();
//            var serviceProviderMock = new Mock<IServiceProvider>();
//            var loggerMock = new Mock<ILogger<BookingProcessingService>>();

//            scopeFactoryMock
//                .Setup(factory => factory.CreateScope())
//                .Returns(scopeMock.Object);

//            scopeMock
//                .Setup(scope => scope.ServiceProvider)
//                .Returns(serviceProviderMock.Object);

//            serviceProviderMock
//                .Setup(provider => provider.GetService(typeof(IBookingRepository)))
//                .Returns(bookingRepositoryMock.Object);

//            serviceProviderMock
//                .Setup(provider => provider.GetService(typeof(IEventRepository)))
//                .Returns(eventRepositoryMock.Object);

           
//            bookingRepositoryMock
//                .Setup(repo => repo.GetPendingsAsync())
//                .ReturnsAsync(new List<Guid> { targetBookingId });

//            bookingRepositoryMock
//                .SetupSequence(repo => repo.GetByIdAsync(targetBookingId))
//                .ReturnsAsync(pendingBooking)   
//                .ReturnsAsync(confirmedBooking);

//            eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            bookingRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(targetBookingId))
//                .ReturnsAsync(confirmedBooking);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            var processingService = new BookingProcessingService(
//                scopeFactoryMock.Object,
//                loggerMock.Object
//            );

//            using var cts = new CancellationTokenSource();

//            // Act
//            var processingTask = processingService.StartAsync(cts.Token);

//            await Task.Delay(TimeSpan.FromSeconds(6));

//            cts.Cancel();

//            try
//            {
//                await processingTask;
//            }
//            catch (OperationCanceledException)
//            {
//            }

//            var result = await _bookingService.GetBookingByIdAsync(targetBookingId, userId, userRole);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(targetBookingId, result.Id);
//            Assert.Equal(Application.Models.BookingStatus.Confirmed, result.Status);
//            Assert.NotNull(result.ProcessedAt);

//            bookingRepositoryMock.Verify(repo => repo.GetPendingsAsync(), Times.AtLeastOnce);
//            bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(targetBookingId), Times.AtLeastOnce);
//            eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.AtLeastOnce);
//            bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.AtLeastOnce);
//        }

//        [Fact]
//        public async Task CreateMultipleBookings_ForSameEvent_AllHaveUniqueIds()
//        {
//            // Arrange
//            var eventGuid = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            var existingEvent = new Event(
//                eventGuid,
//                "Популярное событие",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(2),
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("10");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventGuid))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });
//            // Act
//            var booking1 = await _bookingService.CreateBookingAsync(eventGuid, userId);
//            var booking2 = await _bookingService.CreateBookingAsync(eventGuid, userId);
//            var booking3 = await _bookingService.CreateBookingAsync(eventGuid, userId);

//            // Assert
//            Assert.NotNull(booking1);
//            Assert.NotNull(booking2);
//            Assert.NotNull(booking3);

//            Assert.Equal(eventGuid, booking1.EventId);
//            Assert.Equal(eventGuid, booking2.EventId);
//            Assert.Equal(eventGuid, booking3.EventId);

//            Assert.Equal(userId, booking1.UserId);
//            Assert.Equal(userId, booking2.UserId);
//            Assert.Equal(userId, booking3.UserId);

//            Assert.NotEqual(booking1.Id, booking2.Id);
//            Assert.NotEqual(booking1.Id, booking3.Id);
//            Assert.NotEqual(booking2.Id, booking3.Id);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(3));
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventGuid), Times.Exactly(3));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Exactly(3));
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(3));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenSeatsAreAvailable_ShouldDecreaseAvailableSeatsByOne()
//        {
//            // Arrange 
//            const int initialSeats = 50;
//            var eventGuid = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var existingEvent = new Event(
//                eventGuid,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                initialSeats
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventGuid))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });


//            // Act 
//            var result = await _bookingService.CreateBookingAsync(eventGuid, userId);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(eventGuid, result.EventId);
//            Assert.Equal(Application.Models.BookingStatus.Pending, result.Status);
//            Assert.Equal(userId, result.UserId);

//            var expectedAvailableSeats = initialSeats - 1;
//            Assert.Equal(expectedAvailableSeats, existingEvent.AvailableSeats);

//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventGuid), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(
//                It.Is<Booking>(b =>
//                    b.EventId == eventGuid &&
//                    b.UserId == userId &&
//                    b.Status == Domain.Entities.BookingStatus.Pending
//                )
//            ), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllShouldBeSuccessfulWithUniqueIds()
//        {
//            // Arrange 
//            const int totalSeatsLimit = 3;
//            var eventGuid = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var existingEvent = new Event(
//                eventGuid,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(2).AddMinutes(30),
//                totalSeatsLimit
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("10");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventGuid))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            var createdBookings = new List<BookingDto>();

//            // Act 
//            for (int i = 0; i < totalSeatsLimit; i++)
//            {
//                var booking = await _bookingService.CreateBookingAsync(eventGuid, userId);
//                Assert.NotNull(booking);
//                createdBookings.Add(booking);
//            }

//            // Assert 
//            Assert.Equal(totalSeatsLimit, createdBookings.Count);

//            var uniqueIdsCount = createdBookings.Select(b => b.Id).Distinct().Count();
//            Assert.Equal(totalSeatsLimit, uniqueIdsCount);

//            Assert.Equal(0, existingEvent.AvailableSeats);

//            Assert.All(createdBookings, b => Assert.Equal(eventGuid, b.EventId));
//            Assert.All(createdBookings, b => Assert.Equal(userId, b.UserId));
//            Assert.All(createdBookings, b => Assert.Equal(Application.Models.BookingStatus.Pending, b.Status));

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(totalSeatsLimit));
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventGuid), Times.Exactly(totalSeatsLimit));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Exactly(totalSeatsLimit));
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(totalSeatsLimit));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_FirstBookingSucceeds_SecondBookingThrowsNoAvailableSeatsException()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1), 
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                10
//            );
//            existingEvent.AvailableSeats = 1;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            // Act & Assert
//            var firstBooking = await _bookingService.CreateBookingAsync(eventId, userId);
//            Assert.NotNull(firstBooking);
//            Assert.Equal(0, existingEvent.AvailableSeats);

//            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            Assert.Equal(0, existingEvent.AvailableSeats);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Exactly(2));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(2));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );
//            existingEvent.AvailableSeats = 0;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            // Act & Assert
//            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            Assert.Equal(0, existingEvent.AvailableSeats);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenEventDoesNotExist_ShouldThrowEventNotFoundException()
//        {
//            // Arrange
//            var nonExistingEventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            Event? nullEvent = null;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(nonExistingEventId))
//                .ReturnsAsync(nullEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            // Act & Assert
//            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
//                await _bookingService.CreateBookingAsync(nonExistingEventId, userId)
//            );

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistingEventId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//        }

//        [Fact]
//        public void Confirm_WhenCalled_ShouldSetStatusToConfirmedAndPopulateProcessedAt()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var booking = new Booking(
//                id: bookingId,
//                eventId: eventId,
//                userId: userId,
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow.AddMinutes(-5)
//            );

//            var testStartTime = DateTime.Now; 

//            // Act
//            booking.Confirm();

//            // Assert
//            Assert.Equal(Domain.Entities.BookingStatus.Confirmed, booking.Status);
//            Assert.NotNull(booking.ProcessedAt);
//            Assert.True(booking.ProcessedAt >= testStartTime);
//            Assert.True(booking.ProcessedAt <= DateTime.Now);
//        }

//        [Fact]
//        public async Task Reject_WhenCalled_ShouldSetStatusToRejectedAndPopulateProcessedAt()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var booking = Booking.CreatePending(eventId, userId);

//            var testStartTime = DateTime.Now; 

//            await Task.Delay(TimeSpan.FromSeconds(2));

//            // Act 
//            booking.Reject();

//            // Assert
//            Assert.Equal(Domain.Entities.BookingStatus.Rejected, booking.Status);
//            Assert.NotNull(booking.ProcessedAt);
//            Assert.True(booking.ProcessedAt >= testStartTime, "ProcessedAt should be set to current time 1");
//            Assert.True(booking.ProcessedAt <= DateTime.Now, "ProcessedAt should be set to current time 2");
//        }

//        [Fact]
//        public void RejectAndReleaseSeats_ShouldSetStatusToRejectedAndRestoreAvailableSeats()
//        {
//            // Arrange 
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int totalSeats = 10;

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1), 
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                totalSeats
//            );
//            existingEvent.AvailableSeats = 9; 

//            var booking = new Booking(
//                id: Guid.NewGuid(),
//                eventId: eventId,
//                userId: userId, 
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow
//            );

//            // Act 
//            booking.Reject();
//            existingEvent.ReleaseSeats();

//            // Assert
//            Assert.Equal(Domain.Entities.BookingStatus.Rejected, booking.Status);
//            Assert.NotNull(booking.ProcessedAt);
//            Assert.Equal(totalSeats, existingEvent.AvailableSeats);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_AfterRejectAndReleaseSeats_ShouldAllowToBookTheReleasedSeatSuccessfully()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                10
//            );
//            existingEvent.AvailableSeats = 1;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            var createdBookings = new List<Booking>();
//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .Callback<Booking>(b => createdBookings.Add(b))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });
//            // Act & Assert
//            var result1 = await _bookingService.CreateBookingAsync(eventId, userId);
//            Assert.NotNull(result1);
//            Assert.Equal(0, existingEvent.AvailableSeats);

//            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            var bookingEntity1 = createdBookings[0];
//            bookingEntity1.Reject();
//            existingEvent.ReleaseSeats(1);
//            Assert.Equal(1, existingEvent.AvailableSeats);

//            var result2 = await _bookingService.CreateBookingAsync(eventId, userId);

//            Assert.NotNull(result2);
//            Assert.NotEqual(result1.Id, result2.Id);
//            Assert.Equal(0, existingEvent.AvailableSeats);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(2));
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(
//                It.Is<Booking>(b => b.EventId == eventId && b.UserId == userId)
//            ), Times.Exactly(2));
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Exactly(3));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Exactly(2));
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(3));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_ConcurrentRequests_ShouldAllowExactlyMaxSeatsAndThrowForRest()
//        {
//            // Arrange 
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int availableSeatsCount = 5;
//            const int totalRequestsCount = 20;

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                10
//            );
//            existingEvent.AvailableSeats = availableSeatsCount;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("10"); 

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0); 

//            var createdBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .Callback<Booking>(b => createdBookings.Add(b))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<BookingDto>();
//            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

//            var tasks = new Task[totalRequestsCount];

//            // Act 
//            for (int i = 0; i < totalRequestsCount; i++)
//            {
//                tasks[i] = Task.Run(async () =>
//                {
//                    try
//                    {
//                        var booking = await _bookingService.CreateBookingAsync(eventId, userId);
//                        if (booking != null)
//                        {
//                            successfulBookings.Add(booking);
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        exceptions.Add(ex);
//                    }
//                });
//            }

//            await Task.WhenAll(tasks);

//            // Assert
//            Assert.Equal(availableSeatsCount, successfulBookings.Count);
//            Assert.Equal(totalRequestsCount - availableSeatsCount, exceptions.Count);
//            Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));
//            Assert.Equal(0, existingEvent.AvailableSeats);
//            Assert.Equal(availableSeatsCount, createdBookings.Count);
//            Assert.All(createdBookings, b => Assert.Equal(eventId, b.EventId));
//            Assert.All(createdBookings, b => Assert.Equal(userId, b.UserId));

//            var uniqueIds = createdBookings.Select(b => b.Id).Distinct().Count();
//            Assert.Equal(availableSeatsCount, uniqueIds);

//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Exactly(totalRequestsCount));
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(availableSeatsCount));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Exactly(availableSeatsCount));
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(totalRequestsCount));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_ConcurrentRequests_ShouldGenerateUniqueBookingIds()
//        {
//            // Arrange 
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int availableSeatsCount = 5;
//            const int totalRequestsCount = 5;

//            var existingEvent = new Event(
//                eventId,
//                "Test event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                10
//            );
//            existingEvent.AvailableSeats = availableSeatsCount;

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("10");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            var createdBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .Callback<Booking>(b => createdBookings.Add(b))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<BookingDto>();
//            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

//            var tasks = new Task[totalRequestsCount];

//            // Act 
//            for (int i = 0; i < totalRequestsCount; i++)
//            {
//                tasks[i] = Task.Run(async () =>
//                {
//                    try
//                    {
//                        var booking = await _bookingService.CreateBookingAsync(eventId, userId);
//                        if (booking != null)
//                        {
//                            successfulBookings.Add(booking);
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        exceptions.Add(ex);
//                    }
//                });
//            }

//            await Task.WhenAll(tasks);

//            // Assert
//            Assert.Equal(availableSeatsCount, successfulBookings.Count);
//            Assert.Empty(exceptions);
//            Assert.Equal(0, existingEvent.AvailableSeats);
//            Assert.Equal(availableSeatsCount, createdBookings.Count);

//            var bookingIds = createdBookings.Select(b => b.Id).ToList();
//            var uniqueIdsCount = bookingIds.Distinct().Count();
//            Assert.Equal(availableSeatsCount, uniqueIdsCount);

//            Assert.All(createdBookings, b => Assert.Equal(eventId, b.EventId));
//            Assert.All(createdBookings, b => Assert.Equal(userId, b.UserId));
//            Assert.All(createdBookings, b => Assert.Equal(Domain.Entities.BookingStatus.Pending, b.Status));

//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Exactly(totalRequestsCount));
//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(availableSeatsCount));
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Exactly(availableSeatsCount));
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(totalRequestsCount));
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenEventIsInPast_ShouldThrowPastEventException()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();

//            var pastEvent = new Event(
//                eventId,
//                "Past Event",
//                DateTime.UtcNow.AddDays(-2), 
//                DateTime.UtcNow.AddDays(-2).AddHours(3), 
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(pastEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(0);

//            // Act & Assert
//            await Assert.ThrowsAsync<PastEventException>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenUserReachesBookingLimit_ShouldThrowActiveBookingsLimit()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int bookingsLimit = 5;

//            var futureEvent = new Event(
//                eventId,
//                "Future Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(bookingsLimit);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(futureEvent);

//            // Act & Assert
//            await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Never); 
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenUserHasOneLessThanLimit_ShouldCreateBookingSuccessfully()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int bookingsLimit = 5;

//            var futureEvent = new Event(
//                eventId,
//                "Future Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(bookingsLimit - 1); 

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(futureEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });
//            // Act
//            var result = await _bookingService.CreateBookingAsync(eventId, userId);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(eventId, result.EventId);
//            Assert.Equal(userId, result.UserId);
//            Assert.Equal(Application.Models.BookingStatus.Pending, result.Status);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenOneUserReachesLimit_OtherUserCanStillBook()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var user1Id = Guid.NewGuid();
//            var user2Id = Guid.NewGuid();
//            const int bookingsLimit = 3;

//            var futureEvent = new Event(
//                eventId,
//                "Future Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(user1Id))
//                .ReturnsAsync(bookingsLimit);

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(user2Id))
//                .ReturnsAsync(0);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(futureEvent);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingRepositoryMock
//                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });

//            // Act & Assert
//            await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, user1Id)
//            );

//            var result = await _bookingService.CreateBookingAsync(eventId, user2Id);
//            Assert.NotNull(result);
//            Assert.Equal(user2Id, result.UserId);

//            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once); 
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(user1Id), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(user2Id), Times.Once);
//        }

//        [Fact]
//        public async Task CreateBookingAsync_WhenUserReachesLimit_ShouldThrowWithCorrectParameters()
//        {
//            // Arrange
//            var eventId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            const int bookingsLimit = 5;

//            var futureEvent = new Event(
//                eventId,
//                "Future Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
//                .ReturnsAsync(bookingsLimit);

//            // Act & Assert
//            var exception = await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
//                await _bookingService.CreateBookingAsync(eventId, userId)
//            );

//            Assert.NotNull(exception);
//        }

//        [Fact]
//        public async Task CancelBookingAsync_WhenUserTriesToCancelOtherUsersBooking_ShouldThrowNoPermissionException()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var eventId = Guid.NewGuid();
//            var ownerId = Guid.NewGuid(); 
//            var otherUserId = Guid.NewGuid();  
//            var role = "User"; 

//            var existingEvent = new Event(
//                eventId,
//                "Test Event",
//                DateTime.UtcNow.AddDays(1), 
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            var booking = new Booking(
//                id: bookingId,
//                eventId: eventId,
//                userId: ownerId,
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow
//            );

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(bookingId))
//                .ReturnsAsync(booking);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            // Act & Assert
//            await Assert.ThrowsAsync<NoPermissionException>(async () =>
//                await _bookingService.CancelBookingAsync(bookingId, otherUserId, role)
//            );

//            Assert.Equal(Domain.Entities.BookingStatus.Pending, booking.Status);
//            Assert.Null(booking.ProcessedAt);

//            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);

//            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
//        }

//        [Fact]
//        public async Task CancelBookingAsync_WhenAdminCancelsOtherUsersBooking_ShouldBeSuccessful()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var eventId = Guid.NewGuid();
//            var ownerId = Guid.NewGuid(); 
//            var adminId = Guid.NewGuid(); 
//            var role = "Admin"; 

//            var existingEvent = new Event(
//                eventId,
//                "Test Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );
//            existingEvent.AvailableSeats = 49; 

//            var booking = new Booking(
//                id: bookingId,
//                eventId: eventId,
//                userId: ownerId,
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow
//            );

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(bookingId))
//                .ReturnsAsync(booking);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });
//            // Act
//            var result = await _bookingService.CancelBookingAsync(bookingId, adminId, role);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(bookingId, result.Id);
//            Assert.Equal(Application.Models.BookingStatus.Cancelled, result.Status);
//            Assert.NotNull(result.ProcessedAt);

//            Assert.Equal(50, existingEvent.AvailableSeats);

//            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(eventId), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Once);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Once);
//            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);
//        }

//        [Fact]
//        public async Task CancelBookingAsync_WhenOwnerCancelsOwnBooking_ShouldBeSuccessful()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var eventId = Guid.NewGuid();
//            var ownerId = Guid.NewGuid(); 
//            var role = "User";

//            var existingEvent = new Event(
//                eventId,
//                "Test Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );
//            existingEvent.AvailableSeats = 49; 

//            var booking = new Booking(
//                id: bookingId,
//                eventId: eventId,
//                userId: ownerId,
//                status: Domain.Entities.BookingStatus.Pending,
//                createdAt: DateTime.UtcNow
//            );

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(bookingId))
//                .ReturnsAsync(booking);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            _bookingRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Booking>()))
//                .ReturnsAsync((Booking b) => b);

//            _eventRepositoryMock
//                .Setup(repo => repo.UpdateAsync(It.IsAny<Event>()))
//                .ReturnsAsync((Event e) => e);

//            _bookingDtoMapperServiceMock
//                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
//                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
//                {
//                    Id = b.Id,
//                    EventId = b.EventId,
//                    UserId = b.UserId,
//                    Status = (Application.Models.BookingStatus)b.Status,
//                    CreatedAt = b.CreatedAt,
//                    ProcessedAt = b.ProcessedAt
//                });


//            // Act
//            var result = await _bookingService.CancelBookingAsync(bookingId, ownerId, role);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(bookingId, result.Id);
//            Assert.Equal(Application.Models.BookingStatus.Cancelled, result.Status);
//            Assert.NotNull(result.ProcessedAt);

//            Assert.Equal(50, existingEvent.AvailableSeats);
//        }

//        [Fact]
//        public async Task CancelBookingAsync_WhenBookingDoesNotExist_ShouldThrowBookingNotFoundException()
//        {
//            // Arrange
//            var nonExistentBookingId = Guid.NewGuid();
//            var userId = Guid.NewGuid();
//            var role = "Admin"; 
//            Booking? nullBooking = null;

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(nonExistentBookingId))
//                .ReturnsAsync(nullBooking);

//            // Act & Assert
//            await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
//                await _bookingService.CancelBookingAsync(nonExistentBookingId, userId, role)
//            );

//            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistentBookingId), Times.Once);
//            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//        }

//        [Fact]
//        public async Task CancelBookingAsync_WhenBookingAlreadyCancelled_ShouldThrowBookingAlreadyInStatus()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var eventId = Guid.NewGuid();
//            var ownerId = Guid.NewGuid();
//            var role = "User";

//            var existingEvent = new Event(
//                eventId,
//                "Test Event",
//                DateTime.UtcNow.AddDays(1),
//                DateTime.UtcNow.AddDays(1).AddHours(3),
//                50
//            );

//            var cancelledBooking = new Booking(
//                id: bookingId,
//                eventId: eventId,
//                userId: ownerId,
//                status: Domain.Entities.BookingStatus.Cancelled,
//                createdAt: DateTime.UtcNow
//            );

//            cancelledBooking.ProcessedAt = DateTime.UtcNow;

//            _bookingRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(bookingId))
//                .ReturnsAsync(cancelledBooking);

//            _eventRepositoryMock
//                .Setup(repo => repo.GetByIdAsync(eventId))
//                .ReturnsAsync(existingEvent);

//            // Act & Assert
//            await Assert.ThrowsAsync<BookingAlreadyInStatus>(async () =>
//                await _bookingService.CancelBookingAsync(bookingId, ownerId, role)
//            );

//            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);
//            _eventRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Event>()), Times.Never);
//        }
//    }

//}

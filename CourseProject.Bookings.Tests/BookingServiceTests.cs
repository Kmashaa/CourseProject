using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Application.Models;
using CourseProject.Bookings.Application.Services;
using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.NetworkInformation;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace CourseProject.Bookings.Tests
{


    public class BookingServiceTests : IDisposable
    {
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
        private readonly Mock<IBookingDtoMapperService> _bookingDtoMapperServiceMock;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IBookingCreatedProducer> _bookingCreatedProducerMock;
        private readonly Mock<IBookingCancelledProducer> _bookingCancelledProducerMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;

        private readonly IBookingService _bookingService;

        public BookingServiceTests()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _bookingDtoMapperServiceMock = new Mock<IBookingDtoMapperService>();
            _configuration = new Mock<IConfiguration>();
            _bookingCreatedProducerMock = new Mock<IBookingCreatedProducer>();
            _bookingCancelledProducerMock = new Mock<IBookingCancelledProducer>();
            _loggerMock = new Mock<ILogger<BookingService>>();

            _configuration.Setup(c => c["BookingsLimit"]).Returns("5");


            _bookingService = new BookingService(_bookingRepositoryMock.Object, _bookingDtoMapperServiceMock.Object, _configuration.Object, _bookingCreatedProducerMock.Object, _bookingCancelledProducerMock.Object, _loggerMock.Object);

        }



        public void Dispose()
        {
        }

        [Fact]
        public async Task CreateNewBooking_SuccessfullyCreatedWithPendingStatus()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Booking? capturedBooking = null;

            _configuration
                .Setup(config => config["BookingsLimit"])
                .Returns("5");

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(0);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var result = await _bookingService.CreateBookingAsync(eventGuid, userId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, result.Status);
            Assert.Equal(eventGuid, result.EventId);
            Assert.Equal(userId, result.UserId);

            Assert.NotNull(capturedBooking);
            Assert.Equal(eventGuid, capturedBooking.EventId);
            Assert.Equal(userId, capturedBooking.UserId);
            Assert.Equal(Bookings.Domain.Entities.BookingStatus.Pending, capturedBooking.Status);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                It.Is<Guid>(id => id == capturedBooking.Id),
                It.Is<Guid>(id => id == eventGuid),
                It.Is<Guid>(id => id == userId)
            ), Times.Once);
            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);
        }


        [Fact]
        public async Task CreateBooking_WhenEventIdIsNull_ThrowsInvalidBookingDataException()
        {
            // Arrange
            Guid? nullEventId = null;
            var userId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidBookingDataException>(async () =>
            {
                await _bookingService.CreateBookingAsync(nullEventId, userId);
            });

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(It.IsAny<Guid>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Never);
            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);
        }


        [Fact]
        public async Task CreateBooking_WhenEventDoesNotExist_ShouldStillCreateBooking()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Booking? capturedBooking = null;

            _configuration
                .Setup(config => config["BookingsLimit"])
                .Returns("5");

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(0);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var result = await _bookingService.CreateBookingAsync(nonExistentEventId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nonExistentEventId, result.EventId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, result.Status);

            Assert.NotNull(capturedBooking);
            Assert.Equal(nonExistentEventId, capturedBooking.EventId);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                capturedBooking.Id,
                nonExistentEventId,
                userId
            ), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_WhenEventWasCreatedAndThenDeleted_ShouldStillCreateBooking()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();
            var userId = Guid.NewGuid();
            Booking? capturedBooking = null;

            _configuration
                .Setup(config => config["BookingsLimit"])
                .Returns("5");

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(0);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var result = await _bookingService.CreateBookingAsync(eventGuid, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventGuid, result.EventId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, result.Status);

            Assert.NotNull(capturedBooking);
            Assert.Equal(eventGuid, capturedBooking.EventId);
            Assert.Equal(userId, capturedBooking.UserId);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                capturedBooking.Id,
                eventGuid,
                userId
            ), Times.Once);

        }

        [Fact]
        public async Task GetBookingById_WhenBookingDoesNotExist_ThrowsBookingNotFoundException()
        {
            // Arrange
            var nonExistingBookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = "Admin";

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistingBookingId))
                .ReturnsAsync((Booking?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            {
                await _bookingService.GetBookingByIdAsync(nonExistingBookingId, userId, role);
            });

            Assert.NotNull(exception);

            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistingBookingId), Times.Once);

            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingExists_ReturnsCorrectBookingData()
        {
            // Arrange
            var targetBookingId = Guid.NewGuid();
            var associatedEventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = "User";
            var bookingCreationTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

            var expectedBooking = new Booking(
                id: targetBookingId,
                eventId: associatedEventId,
                userId: userId,
                status: Bookings.Domain.Entities.BookingStatus.Confirmed,
                createdAt: bookingCreationTime
            );

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(targetBookingId))
                .ReturnsAsync(expectedBooking);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var result = await _bookingService.GetBookingByIdAsync(targetBookingId, userId, role);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedBooking.Id, result.Id);
            Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Confirmed, result.Status);
            Assert.Equal(expectedBooking.EventId, result.EventId);
            Assert.Equal(expectedBooking.UserId, result.UserId);

            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(targetBookingId), Times.Once);
            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateMultipleBookings_ForSameEvent_AllHaveUniqueIds()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var bookings = new List<Booking>();

            _configuration.Setup(c => c["BookingsLimit"]).Returns("10");

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(0);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => bookings.Add(b))
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var booking1 = await _bookingService.CreateBookingAsync(eventGuid, userId);
            var booking2 = await _bookingService.CreateBookingAsync(eventGuid, userId);
            var booking3 = await _bookingService.CreateBookingAsync(eventGuid, userId);

            // Assert
            Assert.NotNull(booking1);
            Assert.NotNull(booking2);
            Assert.NotNull(booking3);

            Assert.Equal(eventGuid, booking1.EventId);
            Assert.Equal(eventGuid, booking2.EventId);
            Assert.Equal(eventGuid, booking3.EventId);

            Assert.Equal(userId, booking1.UserId);
            Assert.Equal(userId, booking2.UserId);
            Assert.Equal(userId, booking3.UserId);

            Assert.NotEqual(booking1.Id, booking2.Id);
            Assert.NotEqual(booking1.Id, booking3.Id);
            Assert.NotEqual(booking2.Id, booking3.Id);

            Assert.NotEqual(Guid.Empty, booking1.Id);
            Assert.NotEqual(Guid.Empty, booking2.Id);
            Assert.NotEqual(Guid.Empty, booking3.Id);

            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, booking1.Status);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, booking2.Status);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, booking3.Status);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Exactly(3));
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Exactly(3));
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Exactly(3));
        }

        [Fact]
        public void Confirm_WhenCalled_ShouldSetStatusToConfirmedAndPopulateProcessedAt()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = new Booking(
                id: bookingId,
                eventId: eventId,
                userId: userId,
                status: Bookings.Domain.Entities.BookingStatus.Pending,
                createdAt: DateTime.UtcNow.AddMinutes(-5)
            );

            var testStartTime = DateTime.Now;

            // Act
            booking.Confirm();

            // Assert
            Assert.Equal(Bookings.Domain.Entities.BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
            Assert.True(booking.ProcessedAt >= testStartTime);
            Assert.True(booking.ProcessedAt <= DateTime.Now);
        }

        [Fact]
        public async Task Reject_WhenCalled_ShouldSetStatusToRejectedAndPopulateProcessedAt()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = Booking.CreatePending(eventId, userId);

            var testStartTime = DateTime.Now;

            await Task.Delay(TimeSpan.FromSeconds(2));

            // Act 
            booking.Reject();

            // Assert
            Assert.Equal(Bookings.Domain.Entities.BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
            Assert.True(booking.ProcessedAt >= testStartTime, "ProcessedAt should be set to current time 1");
            Assert.True(booking.ProcessedAt <= DateTime.Now, "ProcessedAt should be set to current time 2");
        }

        [Fact]
        public async Task Cancel_WhenCalled_ShouldSetStatusToCancelledAndPopulateProcessedAt()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = Booking.CreatePending(eventId, userId);

            var testStartTime = DateTime.Now;

            await Task.Delay(TimeSpan.FromSeconds(2));

            // Act 
            booking.Cancel();

            // Assert
            Assert.Equal(Bookings.Domain.Entities.BookingStatus.Cancelled, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
            Assert.True(booking.ProcessedAt >= testStartTime, "ProcessedAt should be set to current time 1");
            Assert.True(booking.ProcessedAt <= DateTime.Now, "ProcessedAt should be set to current time 2");
        }



        [Fact]
        public async Task CreateBookingAsync_WhenUserReachesBookingLimit_ShouldThrowActiveBookingsLimit()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            const int bookingsLimit = 5;

            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(bookingsLimit);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
                await _bookingService.CreateBookingAsync(eventId, userId)
            );

            Assert.NotNull(exception);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);

            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Never);

            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);

        }

        [Fact]
        public async Task CreateBookingAsync_WhenUserHasOneLessThanLimit_ShouldCreateBookingSuccessfully()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            const int bookingsLimit = 5;
            Booking? capturedBooking = null;

            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(bookingsLimit - 1);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act
            var result = await _bookingService.CreateBookingAsync(eventId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, result.Status);
            Assert.NotEqual(Guid.Empty, result.Id);

            Assert.NotNull(capturedBooking);
            Assert.Equal(eventId, capturedBooking.EventId);
            Assert.Equal(userId, capturedBooking.UserId);
            Assert.Equal(Bookings.Domain.Entities.BookingStatus.Pending, capturedBooking.Status);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                capturedBooking.Id,
                eventId,
                userId
            ), Times.Once);
            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Once);

        }

        [Fact]
        public async Task CreateBookingAsync_WhenOneUserReachesLimit_OtherUserCanStillBook()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            const int bookingsLimit = 3;
            Booking? capturedBooking = null;

            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(user1Id))
                .ReturnsAsync(bookingsLimit);

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(user2Id))
                .ReturnsAsync(0);

            _bookingRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .ReturnsAsync((Booking b) => b);

            _bookingCreatedProducerMock
                .Setup(producer => producer.PublishBookingCreated(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _bookingDtoMapperServiceMock
                .Setup(mapper => mapper.EntityToDto(It.IsAny<Booking>()))
                .Returns((Booking b) => new BookingDto(b.Id, b.EventId, b.UserId, (Application.Models.BookingStatus)b.Status, b.CreatedAt, b.ProcessedAt)
                {
                    Id = b.Id,
                    EventId = b.EventId,
                    UserId = b.UserId,
                    Status = (Application.Models.BookingStatus)b.Status,
                    CreatedAt = b.CreatedAt,
                    ProcessedAt = b.ProcessedAt
                });

            // Act & Assert
            await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
                await _bookingService.CreateBookingAsync(eventId, user1Id)
            );

            var result = await _bookingService.CreateBookingAsync(eventId, user2Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user2Id, result.UserId);
            Assert.Equal(eventId, result.EventId);
            Assert.Equal(Bookings.Application.Models.BookingStatus.Pending, result.Status);

            Assert.NotNull(capturedBooking);
            Assert.Equal(user2Id, capturedBooking.UserId);
            Assert.Equal(eventId, capturedBooking.EventId);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(user1Id), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(user2Id), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Once);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                capturedBooking.Id,
                eventId,
                user2Id
            ), Times.Once);

        }

        [Fact]
        public async Task CreateBookingAsync_WhenUserReachesLimit_ShouldThrowWithCorrectParameters()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            const int bookingsLimit = 5;

            _configuration.Setup(c => c["BookingsLimit"]).Returns(bookingsLimit.ToString());

            _bookingRepositoryMock
                .Setup(repo => repo.GetActiveBookingsCountByUserIdAsync(userId))
                .ReturnsAsync(bookingsLimit);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ActiveBookingsLimit>(async () =>
                await _bookingService.CreateBookingAsync(eventId, userId)
            );

            Assert.NotNull(exception);

            Assert.NotNull(exception.Message);
            Assert.NotEmpty(exception.Message);


            _bookingRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Booking>()), Times.Never);
            _bookingCreatedProducerMock.Verify(producer => producer.PublishBookingCreated(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Never);
            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);

            _bookingRepositoryMock.Verify(repo => repo.GetActiveBookingsCountByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task CancelBookingAsync_WhenBookingDoesNotExist_ShouldThrowBookingNotFoundException()
        {
            // Arrange
            var nonExistentBookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = "Admin";

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistentBookingId))
                .ReturnsAsync((Booking?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
                await _bookingService.CancelBookingAsync(nonExistentBookingId, userId, role)
            );

            Assert.NotNull(exception);
            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistentBookingId), Times.Once);

            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);

            _bookingCancelledProducerMock.Verify(producer => producer.PublishBookingCancelled(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Never);

            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CancelBookingAsync_WhenBookingAlreadyCancelled_ShouldThrowBookingAlreadyInStatus()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var role = "User";

            var cancelledBooking = new Booking(
                id: bookingId,
                eventId: eventId,
                userId: ownerId,
                status: Bookings.Domain.Entities.BookingStatus.Cancelled,
                createdAt: DateTime.UtcNow.AddMinutes(-10)
            );

            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(cancelledBooking);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BookingAlreadyInStatus>(async () =>
                await _bookingService.CancelBookingAsync(bookingId, ownerId, role)
            );

            Assert.NotNull(exception);
            _bookingRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Booking>()), Times.Never);

            _bookingCancelledProducerMock.Verify(producer => producer.PublishBookingCancelled(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()
            ), Times.Never);

            _bookingDtoMapperServiceMock.Verify(mapper => mapper.EntityToDto(It.IsAny<Booking>()), Times.Never);
        }

    }
}
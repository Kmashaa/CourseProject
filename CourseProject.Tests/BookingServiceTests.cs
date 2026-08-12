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
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IBookingService _bookingService;
        private readonly AppDbContext _context;

        // Оставляем мок для EventService, если BookingService всё ещё требует его в конструкторе
        private readonly Mock<IEventService> _eventsServiceMock;

        public BookingServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<IBookingService, BookingService>();


            var loggerMockProcessing = new Mock<ILogger<BookingProcessingService>>();
            services.AddSingleton(loggerMockProcessing.Object);

            services.AddScoped<BookingProcessingService>();


            _eventsServiceMock = new Mock<IEventService>();
            services.AddSingleton(_eventsServiceMock.Object);

            var loggerMock = new Mock<ILogger<BookingService>>();
            services.AddSingleton(loggerMock.Object);

            _serviceProvider = services.BuildServiceProvider();

            _scope = _serviceProvider.CreateScope();

            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();

            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        }

        public void Dispose()
        {
            // Очищаем и удаляем InMemory базу данных после каждого теста
            _context.Database.EnsureDeleted();
            _context.Dispose();

            _scope.Dispose();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task CreateNewBooking_SuccessfullyCreatedWithPendingStatus()
        {
            // Arrange
            var existingEvent = Event.Create
            (
                "Test Event 1",
                new DateTime(2026, 4, 5, 0, 0, 0),
                new DateTime(2026, 4, 5, 1, 0, 0),
                50
            );
            var eventGuid = existingEvent.Id;


            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventGuid))
                .ReturnsAsync(existingEvent);


            // Act
            var result = await _bookingService.CreateBookingAsync(eventGuid);

            // Assert
            Assert.NotNull(result); // Бронирование успешно вернулось
            Assert.Equal(BookingStatus.Pending, result.Status); // Статус строго Pending
            Assert.Equal(eventGuid, result.EventId); // Привязано к правильному событию

        }

        [Fact]
        public async Task CreateBooking_WhenEventIdIsNull_ThrowsInvalidEventDataException()
        {
            // Arrange
            Guid? nullEventId = null;

            // Act Assert
            await Assert.ThrowsAsync<InvalidEventDataException>(async () =>
            {
                await _bookingService.CreateBookingAsync(nullEventId);
            });

            var bookingInDb = await _context.Bookings.FirstOrDefaultAsync(o => o.EventId == nullEventId);
            Assert.Null(bookingInDb);

        }

        [Fact]
        public async Task CreateBooking_WhenEventDoesNotExist_ThrowsEventNotFoundException()
        {
            // Arrange
            var nonExistentEventId = Guid.NewGuid();

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(nonExistentEventId)).ReturnsAsync((Event?)null);

            // Act & Assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(nonExistentEventId);

            });

            var bookingInDb = await _context.Bookings.FirstOrDefaultAsync(o => o.EventId == nonExistentEventId);
            Assert.Null(bookingInDb);


        }


        [Fact]
        public async Task CreateBooking_WhenEventWasCreatedAndThenDeleted_ThrowsEventNotFoundException()
        {
            // Arrange
            var eventGuid = Guid.NewGuid();

            var localEvents = new List<Event>
            {
                Event.Create
                (
                    "Test Event 1",
                    new DateTime(2026, 4, 5, 0, 0, 0),
                    new DateTime(2026, 4, 5, 1, 0, 0),
                    50
                )
            };

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventGuid)).ReturnsAsync(() => localEvents.FirstOrDefault(e => e.Id == eventGuid));


            var eventToDelete = localEvents.FirstOrDefault(e => e.Id == eventGuid);
            if (eventToDelete != null)
            {
                localEvents.Remove(eventToDelete);
            }


            // Act assert
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventGuid);
            });

            var bookingInDb = await _context.Bookings.FirstOrDefaultAsync(o => o.EventId == eventGuid);
            Assert.Null(bookingInDb);


        }

        [Fact]
        public async Task GetBookingById_WhenBookingDoesNotExist_ReturnsNull()
        {
            // Arrange
            var nonExistingBookingId = Guid.NewGuid();


            // Act
            var result = await _bookingService.GetBookingByIdAsync(nonExistingBookingId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBookingById_WhenBookingExists_ReturnsCorrectBookingData()
        {
            var targetBookingId = Guid.NewGuid();
            var associatedEventId = Guid.NewGuid();
            var bookingCreationTime = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);

            var expectedBooking = new Booking(
                id: targetBookingId,
                eventId: associatedEventId,
                status: BookingStatus.Confirmed,
                createdAt: bookingCreationTime
            );

            _context.Bookings.Add(expectedBooking);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // 2. Act
            var result = await _bookingService.GetBookingByIdAsync(targetBookingId);

            // 3. Assert
            Assert.NotNull(result);
            Assert.Equal(expectedBooking.Id, result.Id);
            Assert.Equal(expectedBooking.CreatedAt, result.CreatedAt);

        }

        [Fact]
        public async Task GetBookingById_ReflectsStatusChange_AfterBackgroundProcessing()
        {
            // 1. Arrange
            var eventId = Guid.NewGuid();

            var existingEvent = Event.Create(
                "Test Event",
                new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 5, 1, 0, 0, DateTimeKind.Utc),
                10
            );
            existingEvent.Id = eventId;

            var targetBookingId = Guid.NewGuid();
            var booking = new Booking(
                id: targetBookingId,
                eventId: eventId,
                status: BookingStatus.Pending,
                createdAt: DateTime.UtcNow
            );

            _context.Events.Add(existingEvent);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var processingService = _scope.ServiceProvider.GetRequiredService<BookingProcessingService>();

            using var cts = new CancellationTokenSource();

            // 2. Act
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

            _context.ChangeTracker.Clear();

            var result = await _bookingService.GetBookingByIdAsync(targetBookingId);

            // 3. Assert 
            Assert.NotNull(result);
            Assert.Equal(targetBookingId, result.Id);
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            // Настраиваем мок EventService, так как BookingService обращается к нему
            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventGuid))
                .ReturnsAsync(existingEvent);

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

            _context.ChangeTracker.Clear();
            var countInDb = await _context.Bookings.CountAsync(b => b.EventId == eventGuid);
            Assert.Equal(3, countInDb);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreAvailable_ShouldDecreaseAvailableSeatsByOne()
        {
            // Arrange 
            const int initialSeats = 50;

            // Создаем реальное событие через фабрику
            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                initialSeats
            );
            var eventId = existingEvent.Id;

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act 
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventId, result.EventId);

            _context.ChangeTracker.Clear();

            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);

            var expectedAvailableSeats = initialSeats - 1;
            Assert.Equal(expectedAvailableSeats, eventInDb.AvailableSeats);

            var bookingInDb = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == result.Id);
            Assert.NotNull(bookingInDb);
            Assert.Equal(BookingStatus.Pending, bookingInDb.Status);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

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

            _context.ChangeTracker.Clear();

            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats);

            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(totalSeatsLimit, bookingsCountInDb);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act & Assert 

            var firstBooking = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(firstBooking);

            Assert.Equal(0, existingEvent.AvailableSeats);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _bookingService.CreateBookingAsync(eventId)
            );

            _context.ChangeTracker.Clear();

            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats); 

            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(1, bookingsCountInDb);
        }
        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange 

            // Создаем реальное событие с 0 свободных мест строго через фабрику
            var existingEvent = Event.Create(
                "Test event",
                new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 21, 0, 0, DateTimeKind.Utc),
                50
            );
            
            existingEvent.AvailableSeats = 0; 
            var eventId = existingEvent.Id;

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            _eventsServiceMock.Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act & Assert 
            await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
                await _bookingService.CreateBookingAsync(eventId)
            );

            _context.ChangeTracker.Clear();


            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats);

            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(0, bookingsCountInDb); 
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventDoesNotExist_ShouldThrowEventNotFoundException()
        {
            // Arrange 
            var nonExistingEventId = Guid.NewGuid();

            _eventsServiceMock
                .Setup(service => service.GetEventByIdAsync(nonExistingEventId))
                .ReturnsAsync((Event?)null);

            // Act & Assert 
            await Assert.ThrowsAsync<EventNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(nonExistingEventId)
            );

            _context.ChangeTracker.Clear();

            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == nonExistingEventId);
            Assert.Equal(0, bookingsCountInDb);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();

            _eventsServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(existingEvent);

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

            _context.Events.Update(existingEvent);
            _context.Bookings.Update(result1);
            await _context.SaveChangesAsync();

            var result2 = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotNull(result2);
            Assert.NotEqual(result1.Id, result2.Id);

            _context.ChangeTracker.Clear();

            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats); 

            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(2, bookingsCountInDb);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Специальный семафор ТОЛЬКО для теста, чтобы защитить общий _context от одновременного доступа в Task.Run
            var testSemaphore = new SemaphoreSlim(1, 1);

            // Динамическая настройка мока: при каждом обращении он идет в базу за свежими данными
            _eventsServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(() =>
                {
                    // Прямо перед чтением очищаем кэш контекста, чтобы он стер старые данные из памяти
                    _context.ChangeTracker.Clear();
                    // Возвращаем актуальное состояние события из InMemory-базы данных
                    return _context.Events.FirstOrDefault(e => e.Id == eventId)!;
                });

            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = new Task[totalRequestsCount];

            // Act 
            for (int i = 0; i < totalRequestsCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    await testSemaphore.WaitAsync(); // Выстраиваем потоки теста в очередь для безопасной работы с БД
                    try
                    {
                        // Вызываем ОДИН И ТОТ ЖЕ экземпляр сервиса из полей класса
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
                    finally
                    {
                        testSemaphore.Release(); // Освобождаем очередь для следующего потока
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            // 1. Проверяем, что успешных броней ровно 5 (сколько и было мест)
            Assert.Equal(availableSeatsCount, successfulBookings.Count);

            // 2. Проверяем, что остальные 15 запросов завершились ошибкой
            var expectedExceptionsCount = totalRequestsCount - availableSeatsCount;
            Assert.Equal(expectedExceptionsCount, exceptions.Count);

            // 3. Убеждаемся, что все ошибки — это строго NoAvailableSeatsException
            Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));

            _context.ChangeTracker.Clear();

            // 4. Проверяем состояние мероприятия в СУБД — места должны опуститься строго до 0
            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats);

            // 5. Проверяем количество физических строк в таблице Bookings — их должно быть строго 5
            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(availableSeatsCount, bookingsCountInDb);
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

            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Специальный семафор ТОЛЬКО для теста, чтобы защитить общий _context от одновременного доступа в Task.Run
            var testSemaphore = new SemaphoreSlim(1, 1);

            // Динамическая настройка мока: при каждом обращении он идет в базу за свежими данными
            _eventsServiceMock
                .Setup(service => service.GetEventByIdAsync(eventId))
                .ReturnsAsync(() =>
                {
                    // Прямо перед чтением очищаем кэш контекста, чтобы он стер старые данные из памяти
                    _context.ChangeTracker.Clear();
                    // Возвращаем актуальное состояние события из InMemory-базы данных
                    return _context.Events.FirstOrDefault(e => e.Id == eventId)!;
                });

            var successfulBookings = new System.Collections.Concurrent.ConcurrentBag<Booking>();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = new Task[totalRequestsCount];

            // Act 
            for (int i = 0; i < totalRequestsCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    await testSemaphore.WaitAsync(); // Выстраиваем потоки теста в очередь для безопасной работы с БД
                    try
                    {
                        // Вызываем ОДИН И ТОТ ЖЕ экземпляр сервиса из полей класса
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
                    finally
                    {
                        testSemaphore.Release(); // Освобождаем очередь для следующего потока
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            // 1. Проверяем, что успешных броней ровно 5 (сколько и было мест)
            Assert.Equal(availableSeatsCount, successfulBookings.Count);

            // 2. Проверяем, что остальные 15 запросов завершились ошибкой
            var expectedExceptionsCount = totalRequestsCount - availableSeatsCount;
            Assert.Equal(expectedExceptionsCount, exceptions.Count);

            _context.ChangeTracker.Clear();

            // 4. Проверяем состояние мероприятия в СУБД — места должны опуститься строго до 0
            var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            Assert.NotNull(eventInDb);
            Assert.Equal(0, eventInDb.AvailableSeats);

            // 5. Проверяем количество физических строк в таблице Bookings — их должно быть строго 5
            var bookingsCountInDb = await _context.Bookings.CountAsync(b => b.EventId == eventId);
            Assert.Equal(availableSeatsCount, bookingsCountInDb);

            var bookingsIds = await _context.Bookings.Where(o => o.EventId == eventId).Select(o => o.Id).Distinct().ToListAsync();
            var uniqueIdsCount = bookingsIds.Count();
            Assert.Equal(availableSeatsCount, uniqueIdsCount);

        }


    }

}

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Domain.Entities;
using CourseProject.Bookings.Infrastructure.Messaging.Consumers;
using CourseProject.Bookings.Infrastructure.Messaging.Producers;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Testcontainers.Kafka;



namespace CourseProject.Bookings.IntegrationTests
{
    public class KafkaIntegrationTests : IAsyncLifetime
    {
        private readonly KafkaContainer _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .Build();

        private IConfiguration _configuration;
        private IServiceProvider _serviceProvider;
        private Mock<IBookingRepository> _bookingRepositoryMock;
        private Mock<IBookingConfirmedProducer> _bookingConfirmedProducerMock;
        private Mock<IBookingRejectedProducer> _bookingRejectedProducerMock;
        private Mock<IBookingCancelledProducer> _bookingCancelledProducerMock;
        private Mock<IBookingCreatedProducer> _bookingCreatedProducerMock;

        public async Task InitializeAsync()
        {
            await _kafka.StartAsync();

            var configurationBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
                    ["Kafka:ConsumerGroup"] = $"bookings-service-group-{Guid.NewGuid()}"
                });

            _configuration = configurationBuilder.Build();

            InitializeMocks();
            InitializeServiceProvider();
        }

        private void InitializeServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton(_bookingRepositoryMock.Object);
            services.AddSingleton(_bookingConfirmedProducerMock.Object);
            services.AddSingleton(_bookingRejectedProducerMock.Object);
            services.AddSingleton(_bookingCancelledProducerMock.Object);
            services.AddSingleton(_bookingCreatedProducerMock.Object);

            _serviceProvider = services.BuildServiceProvider();
        }

        private IServiceScopeFactory CreateScopeFactory()
        {
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();

            scopeFactoryMock
                .Setup(f => f.CreateScope())
                .Returns(scopeMock.Object);

            scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(_serviceProvider);

            return scopeFactoryMock.Object;
        }

        private void InitializeMocks()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _bookingConfirmedProducerMock = new Mock<IBookingConfirmedProducer>();
            _bookingRejectedProducerMock = new Mock<IBookingRejectedProducer>();
            _bookingCancelledProducerMock = new Mock<IBookingCancelledProducer>();
            _bookingCreatedProducerMock = new Mock<IBookingCreatedProducer>();
        }

        public async Task DisposeAsync()
        {
            await _kafka.DisposeAsync();
        }

        private IProducer<string, string> CreateProducer()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress(),
                Acks = Acks.All
            };

            return new ProducerBuilder<string, string>(config).Build();
        }

        private IConsumer<string, string> CreateConsumer(string groupId = null)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress(),
                GroupId = groupId ?? $"test-consumer-{Guid.NewGuid()}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            return new ConsumerBuilder<string, string>(config).Build();
        }

        private async Task CreateTopicIfNotExistsAsync(string topicName)
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress()
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var existingTopics = metadata.Topics.Select(t => t.Topic).ToList();

                if (!existingTopics.Contains(topicName))
                {
                    await adminClient.CreateTopicsAsync(new List<TopicSpecification>
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                });

                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create topic {topicName}: {ex.Message}");
            }
        }

        private Booking CreateTestBooking(
        Guid? id = null,
        Guid? eventId = null,
        Guid? userId = null,
        BookingStatus status = BookingStatus.Pending)
        {
            return new Booking(
                id ?? Guid.NewGuid(),
                eventId ?? Guid.NewGuid(),
                userId ?? Guid.NewGuid(),
                status,
                DateTime.UtcNow
            );
        }


        [Fact]
        public async Task BookingCreatedProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var logger = NullLogger<BookingCreatedProducer>.Instance;
            using var producer = new BookingCreatedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(BookingCreated.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var numOfSeats = 2;

            // Act
            await producer.PublishBookingCreated(bookingId, eventId, userId, numOfSeats);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(BookingCreated.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<BookingCreated>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
            Assert.Equal(numOfSeats, deserializedEvent.NumOfSeats);
        }


        [Fact]
        public async Task BookingConfirmedProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var logger = NullLogger<BookingConfirmedProducer>.Instance;
            using var producer = new BookingConfirmedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(BookingConfirmed.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            await producer.PublishBookingConfirmed(bookingId, eventId, userId);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(BookingConfirmed.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<BookingConfirmed>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
        }


        [Fact]
        public async Task BookingRejectedProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var logger = NullLogger<BookingRejectedProducer>.Instance;
            using var producer = new BookingRejectedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(BookingRejected.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            await producer.PublishBookingRejected(bookingId, eventId, userId);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(BookingRejected.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<BookingRejected>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
        }


        [Fact]
        public async Task BookingCancelledProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var logger = NullLogger<BookingCancelledProducer>.Instance;
            using var producer = new BookingCancelledProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(BookingCancelled.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var numOfSeats = 3;

            // Act
            await producer.PublishBookingCancelled(bookingId, eventId, userId, numOfSeats);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(BookingCancelled.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<BookingCancelled>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
            Assert.Equal(numOfSeats, deserializedEvent.NumOfSeats);
        }


        [Fact]
        public async Task EventAvailablenessConsumer_WithEventSeatsReserved_ConfirmsBooking()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = CreateTestBooking(
                id: bookingId,
                eventId: eventId,
                userId: userId,
                status: BookingStatus.Pending
            );

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
                .ReturnsAsync(booking);

            _bookingConfirmedProducerMock
                .Setup(p => p.PublishBookingConfirmed(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

             var topicInitializer = new KafkaTopicInitializer(
                _configuration,
                NullLogger<KafkaTopicInitializer>.Instance
            );
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var scopeFactory = CreateScopeFactory();
            var logger = NullLogger<EventAvailablenessConsumer>.Instance;
            var consumer = new EventAvailablenessConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var eventSeatsReserved = new EventSeatsReserved
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(3000);

            await producer.ProduceAsync(EventSeatsReserved.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsReserved)
            });

            await Task.Delay(5000);

            // Assert
            _bookingRepositoryMock.Verify(r => r.GetByIdAsync(bookingId), Times.AtLeastOnce);
            _bookingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.AtLeastOnce);
            _bookingConfirmedProducerMock.Verify(
                p => p.PublishBookingConfirmed(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task EventAvailablenessConsumer_WithEventSeatsUnavailable_RejectsBooking()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var booking = CreateTestBooking(
                id: bookingId,
                eventId: eventId,
                userId: userId,
                status: BookingStatus.Pending
            );

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            _bookingRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
                .ReturnsAsync(booking);

            _bookingRejectedProducerMock
                .Setup(p => p.PublishBookingRejected(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

             var topicInitializer = new KafkaTopicInitializer(
                _configuration,
                NullLogger<KafkaTopicInitializer>.Instance
            );
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var scopeFactory = CreateScopeFactory();
            var logger = NullLogger<EventAvailablenessConsumer>.Instance;
            var consumer = new EventAvailablenessConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var eventSeatsUnavailable = new EventSeatsUnavailable
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                Reason = "no available seats",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(3000);

            await producer.ProduceAsync(EventSeatsUnavailable.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsUnavailable)
            });

            await Task.Delay(5000);

            // Assert
            _bookingRepositoryMock.Verify(r => r.GetByIdAsync(bookingId), Times.AtLeastOnce);
            _bookingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.AtLeastOnce);
            _bookingRejectedProducerMock.Verify(
                p => p.PublishBookingRejected(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task EventAvailablenessConsumer_WhenBookingNotFound_PublishesRejected()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

             var topicInitializer = new KafkaTopicInitializer(
                _configuration,
                NullLogger<KafkaTopicInitializer>.Instance
            );
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((Booking?)null);

            _bookingRejectedProducerMock
                .Setup(p => p.PublishBookingRejected(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<EventAvailablenessConsumer>.Instance;
            var consumer = new EventAvailablenessConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var eventSeatsReserved = new EventSeatsReserved
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(5000);

            await producer.ProduceAsync(EventSeatsReserved.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsReserved)
            });

            await Task.Delay(5000);

            // Assert
            _bookingRejectedProducerMock.Verify(
                p => p.PublishBookingRejected(
                    bookingId,
                    eventId,
                    userId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );
            _bookingConfirmedProducerMock.Verify(
                p => p.PublishBookingConfirmed(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }
        [Fact]
        public async Task EventAvailablenessConsumer_WithInvalidJson_SkipsMessage()
        {
            // Arrange

             var topicInitializer = new KafkaTopicInitializer(
                _configuration,
                NullLogger<KafkaTopicInitializer>.Instance
            );
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var scopeFactory = CreateScopeFactory();
            var logger = NullLogger<EventAvailablenessConsumer>.Instance;
            var consumer = new EventAvailablenessConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var eventId = Guid.NewGuid();

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(3000);

            await producer.ProduceAsync(EventSeatsReserved.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = "invalid json"
            });

            await Task.Delay(5000);

            // Assert
            _bookingRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _bookingConfirmedProducerMock.Verify(
                p => p.PublishBookingConfirmed(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task KafkaTopicInitializer_CreatesTopics_WhenTheyDontExist()
        {
            // Arrange
            var logger = NullLogger<KafkaTopicInitializer>.Instance;
            var initializer = new KafkaTopicInitializer(_configuration, logger);

            // Act
            await initializer.StartAsync(CancellationToken.None);

            // Assert
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress()
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            var topicNames = metadata.Topics.Select(t => t.Topic).ToList();

            Assert.Contains(EventSeatsReserved.TopicName, topicNames);
            Assert.Contains(EventSeatsUnavailable.TopicName, topicNames);

            await initializer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task KafkaTopicInitializer_WhenTopicsExist_DoesNotThrowException()
        {
            // Arrange
            var logger = NullLogger<KafkaTopicInitializer>.Instance;
            var initializer = new KafkaTopicInitializer(_configuration, logger);

            // Act - первый вызов создает топики
            await initializer.StartAsync(CancellationToken.None);

            // Act - второй вызов не должен бросать исключение
            await initializer.StartAsync(CancellationToken.None);

            // Assert
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = _kafka.GetBootstrapAddress()
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

            var topicNames = metadata.Topics.Select(t => t.Topic).ToList();

            Assert.Contains(EventSeatsReserved.TopicName, topicNames);
            Assert.Contains(EventSeatsUnavailable.TopicName, topicNames);

            await initializer.StopAsync(CancellationToken.None);
        }

    }
}

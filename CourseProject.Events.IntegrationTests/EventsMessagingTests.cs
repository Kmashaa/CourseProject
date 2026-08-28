using Confluent.Kafka;
using CourseProject.Contracts;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Infrastructure.Messaging.Consumers;
using CourseProject.Events.Infrastructure.Messaging.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Testcontainers.Kafka;



namespace CourseProject.Events.IntegrationTests
{
    public class KafkaIntegrationTests : IAsyncLifetime
    {
        private readonly KafkaContainer _kafka = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .Build();

        private IConfiguration _configuration;
        private IServiceProvider _serviceProvider;
        private Mock<IEventRepository> _eventRepositoryMock;
        private Mock<ICacheService> _cacheServiceMock;
        private Mock<IEventSeatsReleasedProducer> _eventSeatsReleasedProducerMock;
        private Mock<IEventSeatsReservedProducer> _eventSeatsReservedProducerMock;
        private Mock<IEventSeatsUnavailableProducer> _eventSeatsUnavailableProducerMock;

        public async Task InitializeAsync()
        {
            await _kafka.StartAsync();

            var configurationBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
                    ["Kafka:ConsumerGroup"] = $"events-service-group-{Guid.NewGuid()}"
                });

            _configuration = configurationBuilder.Build();

            InitializeMocks();
            InitializeServiceProvider();
        }

        private void InitializeServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton(_eventRepositoryMock.Object);
            services.AddSingleton(_cacheServiceMock.Object);
            services.AddSingleton(_eventSeatsReleasedProducerMock.Object);
            services.AddSingleton(_eventSeatsReservedProducerMock.Object);
            services.AddSingleton(_eventSeatsUnavailableProducerMock.Object);

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
            _eventRepositoryMock = new Mock<IEventRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _eventSeatsReleasedProducerMock = new Mock<IEventSeatsReleasedProducer>();
            _eventSeatsReservedProducerMock = new Mock<IEventSeatsReservedProducer>();
            _eventSeatsUnavailableProducerMock = new Mock<IEventSeatsUnavailableProducer>();
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


        [Fact]
        public async Task EventSeatsReleasedProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var topicInitializer = new KafkaTopicInitializer(
_configuration,
NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var logger = NullLogger<EventSeatsReleasedProducer>.Instance;
            using var producer = new EventSeatsReleasedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(EventSeatsReleased.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            await producer.PublishEventSeatsReleased(bookingId, eventId, userId);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(EventSeatsReleased.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<EventSeatsReleased>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
            Assert.NotEqual(default, deserializedEvent.CreatedAt);
        }

        [Fact]
        public async Task EventSeatsReleasedProducer_MultipleMessages_AllPublishedCorrectly()
        {
            // Arrange
            var topicInitializer = new KafkaTopicInitializer(
_configuration,
NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var logger = NullLogger<EventSeatsReleasedProducer>.Instance;
            using var producer = new EventSeatsReleasedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(EventSeatsReleased.TopicName);

            var messages = new List<(Guid BookingId, Guid EventId, Guid UserId)>();
            for (int i = 0; i < 5; i++)
            {
                messages.Add((Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
            }

            // Act
            foreach (var (bookingId, eventId, userId) in messages)
            {
                await producer.PublishEventSeatsReleased(bookingId, eventId, userId);
            }

            // Assert
            var receivedMessages = new List<EventSeatsReleased>();
            for (int i = 0; i < messages.Count; i++)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
                Assert.NotNull(consumeResult);

                var deserializedEvent = JsonSerializer.Deserialize<EventSeatsReleased>(
                    consumeResult.Message.Value
                );
                receivedMessages.Add(deserializedEvent!);
            }

            Assert.Equal(messages.Count, receivedMessages.Count);

            foreach (var (bookingId, eventId, userId) in messages)
            {
                Assert.Contains(receivedMessages, e =>
                    e.BookingId == bookingId &&
                    e.EventId == eventId &&
                    e.UserId == userId
                );
            }
        }



        [Fact]
        public async Task EventSeatsReservedProducer_PublishesMessage_ToCorrectTopic()
        {
            // Arrange
            var topicInitializer = new KafkaTopicInitializer(
_configuration,
NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            var logger = NullLogger<EventSeatsReservedProducer>.Instance;
            using var producer = new EventSeatsReservedProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(EventSeatsReserved.TopicName);


            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            await producer.PublishEventSeatsReserved(bookingId, eventId, userId);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(15));

            Assert.NotNull(consumeResult);
            Assert.Equal(EventSeatsReserved.TopicName, consumeResult.Topic);
            Assert.Equal(eventId.ToString(), consumeResult.Message.Key);

            var deserializedEvent = JsonSerializer.Deserialize<EventSeatsReserved>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
        }

        [Fact]
        public async Task EventSeatsReservedProducer_WithCancellation_ThrowsOperationCancelled()
        {
            // Arrange
            var topicInitializer = new KafkaTopicInitializer(
_configuration,
NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var logger = NullLogger<EventSeatsReservedProducer>.Instance;
            using var producer = new EventSeatsReservedProducer(_configuration, logger);
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => producer.PublishEventSeatsReserved(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    cancellationToken
                )
            );
        }



        [Fact]
        public async Task EventSeatsUnavailableProducer_PublishesMessage_WithReason()
        {
            // Arrange
            var topicInitializer = new KafkaTopicInitializer(
_configuration,
NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            var logger = NullLogger<EventSeatsUnavailableProducer>.Instance;
            using var producer = new EventSeatsUnavailableProducer(_configuration, logger);
            using var consumer = CreateConsumer();
            consumer.Subscribe(EventSeatsUnavailable.TopicName);

            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var reason = "no available seats";

            // Act
            await producer.PublishEventSeatsUnavailable(bookingId, eventId, userId, reason);

            // Assert
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

            Assert.NotNull(consumeResult);
            Assert.Equal(EventSeatsUnavailable.TopicName, consumeResult.Topic);

            var deserializedEvent = JsonSerializer.Deserialize<EventSeatsUnavailable>(
                consumeResult.Message.Value
            );

            Assert.NotNull(deserializedEvent);
            Assert.Equal(bookingId, deserializedEvent.BookingId);
            Assert.Equal(eventId, deserializedEvent.EventId);
            Assert.Equal(userId, deserializedEvent.UserId);
            Assert.Equal(reason, deserializedEvent.Reason);
        }



        [Fact]
        public async Task BookingCreatedConsumer_WithValidEvent_ReservesSeatsAndPublishesReserved()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
    _configuration,
    NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            var @event = new Event(
                eventId,
                "Test Event",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                100
            );

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(@event);

            _eventRepositoryMock
                .Setup(r => r.UpdateAsync(@event))
                .ReturnsAsync(@event);

            _cacheServiceMock
                .Setup(c => c.SetById(eventId, @event))
                .ReturnsAsync(@event);

            _eventSeatsReservedProducerMock
                .Setup(p => p.PublishEventSeatsReserved(bookingId, eventId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCreatedConsumer>.Instance;
            var consumer = new BookingCreatedConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCreated = new BookingCreated
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(2000);
            await producer.ProduceAsync(BookingCreated.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCreated)
            });

            await Task.Delay(3000);
            // Assert
            _eventRepositoryMock.Verify(r => r.GetByIdAsync(eventId), Times.AtLeastOnce);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(@event), Times.AtLeastOnce);
            _eventSeatsReservedProducerMock.Verify(
                p => p.PublishEventSeatsReserved(bookingId, eventId, userId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );
            _cacheServiceMock.Verify(c => c.SetById(eventId, @event), Times.AtLeastOnce);

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task BookingCreatedConsumer_WhenEventNotFound_PublishesUnavailable()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
   _configuration,
   NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((Event?)null);

            _eventSeatsUnavailableProducerMock
                .Setup(p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "event doesn't exist",
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCreatedConsumer>.Instance;
            var consumer = new BookingCreatedConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCreated = new BookingCreated
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(5000);

            await producer.ProduceAsync(BookingCreated.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCreated)
            });

            await Task.Delay(5000);

            // Assert
            _eventSeatsUnavailableProducerMock.Verify(
                p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "event doesn't exist",
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task BookingCreatedConsumer_WhenEventAlreadyStarted_PublishesUnavailable()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
    _configuration,
    NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            var @event = new Event(
                eventId,
                "Past Event",
                DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1), 100
            );

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(@event);

            _eventSeatsUnavailableProducerMock
                .Setup(p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "event has already started",
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCreatedConsumer>.Instance;
            var consumer = new BookingCreatedConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCreated = new BookingCreated
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(2000);

            await producer.ProduceAsync(BookingCreated.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCreated)
            });

            await Task.Delay(3000);

            // Assert
            _eventSeatsUnavailableProducerMock.Verify(
                p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "event has already started",
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task BookingCreatedConsumer_WhenNoAvailableSeats_PublishesUnavailable()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
    _configuration,
    NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            var @event = new Event(
                eventId,
                "Full Event",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                1
            );
            @event.TryReserveSeats(1);
            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(@event);

            _eventSeatsUnavailableProducerMock
                .Setup(p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "no available seats",
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCreatedConsumer>.Instance;
            var consumer = new BookingCreatedConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCreated = new BookingCreated
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(2000);

            await producer.ProduceAsync(BookingCreated.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCreated)
            });

            await Task.Delay(3000);

            // Assert
            _eventSeatsUnavailableProducerMock.Verify(
                p => p.PublishEventSeatsUnavailable(
                    bookingId,
                    eventId,
                    userId,
                    "no available seats",
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }



        [Fact]
        public async Task BookingCancelledConsumer_WithValidEvent_ReleasesSeatsAndPublishesReleased()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
    _configuration,
    NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            var @event = new Event(
                eventId,
                "Test Event",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                100
            );
            @event.TryReserveSeats(1);
            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(@event);

            _eventRepositoryMock
                .Setup(r => r.UpdateAsync(@event))
                .ReturnsAsync(@event);

            _cacheServiceMock
                .Setup(c => c.SetById(eventId, @event))
                .ReturnsAsync(@event);

            _eventSeatsReleasedProducerMock
                .Setup(p => p.PublishEventSeatsReleased(bookingId, eventId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCancelledConsumer>.Instance;
            var consumer = new BookingCancelledConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCancelled = new BookingCancelled
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(2000);

            await producer.ProduceAsync(BookingCancelled.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCancelled)
            });

            await Task.Delay(3000);

            // Assert
            _eventRepositoryMock.Verify(r => r.GetByIdAsync(eventId), Times.AtLeastOnce);
            _eventRepositoryMock.Verify(r => r.UpdateAsync(@event), Times.AtLeastOnce);
            _eventSeatsReleasedProducerMock.Verify(
                p => p.PublishEventSeatsReleased(bookingId, eventId, userId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );
            _cacheServiceMock.Verify(c => c.SetById(eventId, @event), Times.AtLeastOnce);

            cts.Cancel();
            await consumer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task BookingCancelledConsumer_WhenEventNotFound_DoesNotPublish()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var topicInitializer = new KafkaTopicInitializer(
    _configuration,
    NullLogger<KafkaTopicInitializer>.Instance
);
            await topicInitializer.StartAsync(CancellationToken.None);
            await topicInitializer.StopAsync(CancellationToken.None);


            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((Event?)null);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var logger = NullLogger<BookingCancelledConsumer>.Instance;
            var consumer = new BookingCancelledConsumer(_configuration, logger, scopeFactory);

            using var producer = CreateProducer();

            var bookingCancelled = new BookingCancelled
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = 1,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var cts = new CancellationTokenSource();
            var consumerTask = consumer.StartAsync(cts.Token);

            await Task.Delay(2000);

            await producer.ProduceAsync(BookingCancelled.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCancelled)
            });

            await Task.Delay(3000);

            // Assert
            _eventSeatsReleasedProducerMock.Verify(
                p => p.PublishEventSeatsReleased(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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

            Assert.Contains(BookingCreated.TopicName, topicNames);
            Assert.Contains(BookingCancelled.TopicName, topicNames);
            Assert.Contains(BookingRejected.TopicName, topicNames);
            Assert.Contains(BookingConfirmed.TopicName, topicNames);

            await initializer.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task KafkaTopicInitializer_WhenTopicsExist_DoesNotThrowException()
        {
            // Arrange
            var logger = NullLogger<KafkaTopicInitializer>.Instance;
            var initializer = new KafkaTopicInitializer(_configuration, logger);

            // Act
            await initializer.StartAsync(CancellationToken.None);

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

            Assert.Contains(BookingCreated.TopicName, topicNames);
            Assert.Contains(BookingCancelled.TopicName, topicNames);
            Assert.Contains(BookingRejected.TopicName, topicNames);
            Assert.Contains(BookingConfirmed.TopicName, topicNames);

            await initializer.StopAsync(CancellationToken.None);
        }

    }
}

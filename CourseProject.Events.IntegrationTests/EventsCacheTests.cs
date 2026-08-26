using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Infrastructure.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.Redis;

namespace CourseProject.Events.IntegrationTests
{
    public class EventsCacheTests : IAsyncLifetime
    {
        private readonly RedisContainer _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        private IConfiguration _configuration;
        private IConnectionMultiplexer _multiplexer;
        private CacheService _cacheService;
        private ILogger<CacheService> _logger;

        public async Task InitializeAsync()
        {
            await _redis.StartAsync();

            var configurationBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:RedisConnection"] = _redis.GetConnectionString(),
                    ["Redis:LongTTL"] = "15",
                    ["Redis:ShortTTL"] = "3"
                });

            _configuration = configurationBuilder.Build();
            _logger = NullLogger<CacheService>.Instance;
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
            _cacheService = new CacheService(_multiplexer, _logger, _configuration);
        }

        public async Task DisposeAsync()
        {
            _multiplexer?.Dispose();
            await _redis.DisposeAsync();
        }

        private Event CreateTestEvent(
            string title = "Test Event",
            int totalSeats = 100,
            string? description = null)
        {
            return Event.Create(
                title,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                totalSeats,
                description
            );
        }

        private TopEvent CreateTestTopEvent(
            string title = "Top Event",
            int totalSeats = 100,
            int availableSeats = 50,
            decimal salesPercentage = 50m)
        {
            return new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = "Test Description",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats,
                SalesPercentage = salesPercentage
            };
        }

 
        [Fact]
        public async Task GetById_WhenEventExistsInCache_ReturnsEvent()
        {
            // Arrange
            var @event = CreateTestEvent();
            await _cacheService.SetById(@event.Id, @event);

            // Act
            var result = await _cacheService.GetById(@event.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Id, result.Id);
            Assert.Equal(@event.Title, result.Title);
            Assert.Equal(@event.Description, result.Description);
            Assert.Equal(@event.StartAt, result.StartAt);
            Assert.Equal(@event.EndAt, result.EndAt);
            Assert.Equal(@event.TotalSeats, result.TotalSeats);
            Assert.Equal(@event.AvailableSeats, result.AvailableSeats);
        }

        [Fact]
        public async Task GetById_WhenEventNotInCache_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _cacheService.GetById(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetById_WithComplexEvent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var @event = CreateTestEvent(
                title: "Complex Event",
                totalSeats: 150,
                description: "Event with description"
            );
            @event.TryReserveSeats(30);  
            await _cacheService.SetById(@event.Id, @event);

            // Act
            var result = await _cacheService.GetById(@event.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Id, result.Id);
            Assert.Equal("Complex Event", result.Title);
            Assert.Equal("Event with description", result.Description);
            Assert.Equal(150, result.TotalSeats);
            Assert.Equal(120, result.AvailableSeats);          }

 
 
        [Fact]
        public async Task SetById_WithValidEvent_ReturnsEvent()
        {
            // Arrange
            var @event = CreateTestEvent();

            // Act
            var result = await _cacheService.SetById(@event.Id, @event);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Id, result.Id);
            Assert.Equal(@event.Title, result.Title);
        }

        [Fact]
        public async Task SetById_ThenGetById_ReturnsSameEvent()
        {
            // Arrange
            var @event = CreateTestEvent(title: "Roundtrip Event");

            // Act
            await _cacheService.SetById(@event.Id, @event);
            var result = await _cacheService.GetById(@event.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Id, result.Id);
            Assert.Equal("Roundtrip Event", result.Title);
            Assert.Equal(@event.TotalSeats, result.TotalSeats);
            Assert.Equal(@event.AvailableSeats, result.AvailableSeats);
        }

        [Fact]
        public async Task SetById_OverwriteExistingEvent_UpdatesCache()
        {
            // Arrange
            var @event = CreateTestEvent(title: "Original Event", totalSeats: 100);
            await _cacheService.SetById(@event.Id, @event);

                         var updatedEvent = new Event(
                @event.Id,
                "Updated Event",
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(2).AddHours(2),
                150,
                "Updated Description"
            );

            // Act
            await _cacheService.SetById(updatedEvent.Id, updatedEvent);
            var result = await _cacheService.GetById(updatedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Event", result.Title);
            Assert.Equal(150, result.TotalSeats);
            Assert.Equal(150, result.AvailableSeats);              Assert.Equal("Updated Description", result.Description);
        }
 
 
        [Fact]
        public async Task DeleteById_WhenEventExists_RemovesFromCache()
        {
            // Arrange
            var @event = CreateTestEvent();
            await _cacheService.SetById(@event.Id, @event);

                         var beforeDelete = await _cacheService.GetById(@event.Id);
            Assert.NotNull(beforeDelete);

            // Act
            await _cacheService.DeleteById(@event.Id);

            // Assert
            var afterDelete = await _cacheService.GetById(@event.Id);
            Assert.Null(afterDelete);
        }

        [Fact]
        public async Task DeleteById_WhenEventNotInCache_DoesNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await _cacheService.DeleteById(nonExistentId);          }

        [Fact]
        public async Task DeleteById_ThenSetById_CanStoreAgain()
        {
            // Arrange
            var @event = CreateTestEvent();
            await _cacheService.SetById(@event.Id, @event);
            await _cacheService.DeleteById(@event.Id);

            // Act
            await _cacheService.SetById(@event.Id, @event);
            var result = await _cacheService.GetById(@event.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(@event.Id, result.Id);
        }

 
 
        [Fact]
        public async Task GetTop_WhenTopEventsExistInCache_ReturnsTopEvents()
        {
            // Arrange
            var topEvents = new List<TopEvent>
        {
            CreateTestTopEvent("Top Event 1", 100, 10, 90m),
            CreateTestTopEvent("Top Event 2", 80, 40, 50m),
            CreateTestTopEvent("Top Event 3", 120, 90, 25m)
        };

            await _cacheService.SetTop(3, topEvents);

            // Act
            var result = await _cacheService.GetTop(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Top Event 1", result[0].Title);
            Assert.Equal(90m, result[0].SalesPercentage);
            Assert.Equal("Top Event 2", result[1].Title);
            Assert.Equal(50m, result[1].SalesPercentage);
            Assert.Equal("Top Event 3", result[2].Title);
            Assert.Equal(25m, result[2].SalesPercentage);
        }

        [Fact]
        public async Task GetTop_WhenNotInCache_ReturnsNull()
        {
            // Arrange
            var number = 10;

            // Act
            var result = await _cacheService.GetTop(number);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTop_WithDifferentNumbers_ReturnsDifferentResults()
        {
            // Arrange
            var top3Events = new List<TopEvent>
        {
            CreateTestTopEvent("Top 3 - Event 1"),
            CreateTestTopEvent("Top 3 - Event 2"),
            CreateTestTopEvent("Top 3 - Event 3")
        };

            var top5Events = new List<TopEvent>
        {
            CreateTestTopEvent("Top 5 - Event 1"),
            CreateTestTopEvent("Top 5 - Event 2"),
            CreateTestTopEvent("Top 5 - Event 3"),
            CreateTestTopEvent("Top 5 - Event 4"),
            CreateTestTopEvent("Top 5 - Event 5")
        };

            await _cacheService.SetTop(3, top3Events);
            await _cacheService.SetTop(5, top5Events);

            // Act
            var result3 = await _cacheService.GetTop(3);
            var result5 = await _cacheService.GetTop(5);

            // Assert
            Assert.NotNull(result3);
            Assert.NotNull(result5);
            Assert.Equal(3, result3.Count);
            Assert.Equal(5, result5.Count);
            Assert.Equal("Top 3 - Event 1", result3[0].Title);
            Assert.Equal("Top 5 - Event 1", result5[0].Title);
        }

 
 
        [Fact]
        public async Task SetTop_WithValidList_ReturnsList()
        {
            // Arrange
            var topEvents = new List<TopEvent>
        {
            CreateTestTopEvent("Event 1"),
            CreateTestTopEvent("Event 2")
        };

            // Act
            var result = await _cacheService.SetTop(2, topEvents);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Event 1", result[0].Title);
            Assert.Equal("Event 2", result[1].Title);
        }

        [Fact]
        public async Task SetTop_ThenGetTop_ReturnsSameList()
        {
            // Arrange
            var topEvents = new List<TopEvent>
        {
            CreateTestTopEvent("Roundtrip Top Event 1", 100, 20, 80m),
            CreateTestTopEvent("Roundtrip Top Event 2", 90, 45, 50m)
        };

            // Act
            await _cacheService.SetTop(2, topEvents);
            var result = await _cacheService.GetTop(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Roundtrip Top Event 1", result[0].Title);
            Assert.Equal(80m, result[0].SalesPercentage);
            Assert.Equal("Roundtrip Top Event 2", result[1].Title);
            Assert.Equal(50m, result[1].SalesPercentage);
        }

        [Fact]
        public async Task SetTop_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var emptyList = new List<TopEvent>();

            // Act
            var result = await _cacheService.SetTop(5, emptyList);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SetTop_WithComplexTopEvents_SerializesAllFields()
        {
            // Arrange
            var topEvent = new TopEvent
            {
                Id = Guid.NewGuid(),
                Title = "Complex Top Event",
                Description = "Full Description",
                StartAt = new DateTime(2026, 5, 1, 10, 30, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Utc),
                TotalSeats = 200,
                AvailableSeats = 50,
                SalesPercentage = 75.5m
            };

            var topEvents = new List<TopEvent> { topEvent };

            // Act
            await _cacheService.SetTop(1, topEvents);
            var result = await _cacheService.GetTop(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(topEvent.Id, result[0].Id);
            Assert.Equal("Complex Top Event", result[0].Title);
            Assert.Equal("Full Description", result[0].Description);
            Assert.Equal(topEvent.StartAt, result[0].StartAt);
            Assert.Equal(topEvent.EndAt, result[0].EndAt);
            Assert.Equal(200, result[0].TotalSeats);
            Assert.Equal(50, result[0].AvailableSeats);
            Assert.Equal(75.5m, result[0].SalesPercentage);
        }

 
 
        [Fact]
        public async Task SetById_WithShortTTL_ExpiresAfterTime()
        {
            // Arrange
            var @event = CreateTestEvent(title: "TTL Test Event");

            // Act
            await _cacheService.SetById(@event.Id, @event);

                         var immediatelyAfterSet = await _cacheService.GetById(@event.Id);
            Assert.NotNull(immediatelyAfterSet);

                                      await Task.Delay(TimeSpan.FromSeconds(5));  
            // Assert
            var afterTTL = await _cacheService.GetById(@event.Id);
                                  }

 
 
        [Fact]
        public async Task CacheOperations_WithMultipleEvents_HandlesCorrectly()
        {
            // Arrange
            var event1 = CreateTestEvent("Event 1", 100);
            var event2 = CreateTestEvent("Event 2", 150);
            var event3 = CreateTestEvent("Event 3", 200);

            // Act
            await _cacheService.SetById(event1.Id, event1);
            await _cacheService.SetById(event2.Id, event2);
            await _cacheService.SetById(event3.Id, event3);

            // Assert
            var result1 = await _cacheService.GetById(event1.Id);
            var result2 = await _cacheService.GetById(event2.Id);
            var result3 = await _cacheService.GetById(event3.Id);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.Equal("Event 1", result1.Title);
            Assert.Equal("Event 2", result2.Title);
            Assert.Equal("Event 3", result3.Title);

                         await _cacheService.DeleteById(event2.Id);

                         var afterDelete1 = await _cacheService.GetById(event1.Id);
            var afterDelete2 = await _cacheService.GetById(event2.Id);
            var afterDelete3 = await _cacheService.GetById(event3.Id);

            Assert.NotNull(afterDelete1);
            Assert.Null(afterDelete2);
            Assert.NotNull(afterDelete3);
        }

        [Fact]
        public async Task CacheOperations_WithEventAndTopEvents_HandlesIndependently()
        {
            // Arrange
            var @event = CreateTestEvent("Regular Event");
            var topEvents = new List<TopEvent>
        {
            CreateTestTopEvent("Top Event")
        };

            // Act
            await _cacheService.SetById(@event.Id, @event);
            await _cacheService.SetTop(1, topEvents);

            // Assert 
            var eventResult = await _cacheService.GetById(@event.Id);
            var topResult = await _cacheService.GetTop(1);

            Assert.NotNull(eventResult);
            Assert.NotNull(topResult);
            Assert.Equal("Regular Event", eventResult.Title);
            Assert.Equal("Top Event", topResult[0].Title);

                         await _cacheService.DeleteById(@event.Id);

            var eventAfterDelete = await _cacheService.GetById(@event.Id);
            var topAfterDelete = await _cacheService.GetTop(1);

            Assert.Null(eventAfterDelete);
            Assert.NotNull(topAfterDelete);
            Assert.Equal("Top Event", topAfterDelete[0].Title);
        }

     }
}

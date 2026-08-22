using Confluent.Kafka;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Producers
{
    public class EventSeatsReleasedProducer : IEventSeatsReleasedProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<EventSeatsReleasedProducer> _logger;


        public EventSeatsReleasedProducer(IConfiguration configuration, ILogger<EventSeatsReleasedProducer> logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();

            _logger = logger;
        }

        public async Task PublishEventSeatsReleased(Guid bookingId, Guid eventId, Guid userId, CancellationToken ct = default)
        {
            var eventSeatsReleased = new EventSeatsReleased
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _producer.ProduceAsync(EventSeatsReleased.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsReleased)
            },
            ct);

            _logger.LogInformation($"Доставлено: {result.TopicPartitionOffset}");
            Console.WriteLine($"Доставлено: {result.TopicPartitionOffset}");

        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));

            _producer.Dispose();
        }

    }
}

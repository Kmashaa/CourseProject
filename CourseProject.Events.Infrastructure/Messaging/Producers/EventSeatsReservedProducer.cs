using Confluent.Kafka;
using CourseProject.Contracts;
using CourseProject.Events.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Producers
{
    public class EventSeatsReservedProducer : IEventSeatsReservedProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<EventSeatsReservedProducer> _logger;


        public EventSeatsReservedProducer(IConfiguration configuration, ILogger<EventSeatsReservedProducer> logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;

        }

        public async Task PublishEventSeatsReserved(Guid bookingId, Guid eventId, Guid userId, CancellationToken ct = default)
        {
            var eventSeatsReserved = new EventSeatsReserved
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _producer.ProduceAsync(EventSeatsReserved.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsReserved)
            },
            ct);

            _logger.LogInformation("Доставлено: {TopicPartitionOffset}", result.TopicPartitionOffset);
        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));

            _producer.Dispose();
        }

    }
}

using Confluent.Kafka;
using CourseProject.Contracts;
using CourseProject.Events.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Producers
{
    public class EventSeatsUnavailableProducer : IEventSeatsUnavailableProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public EventSeatsUnavailableProducer(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();

        }

        public async Task PublishEventSeatsUnavailable(Guid bookingId, Guid eventId, string? reason, CancellationToken ct = default)
        {
            var bookingCreated = new EventSeatsUnavailable
            {
                BookingId = bookingId,
                EventId = eventId,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

        var result = await _producer.ProduceAsync(EventSeatsUnavailable.TopicName, new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(bookingCreated)
        },
        ct);

        Console.WriteLine($"Доставлено: {result.TopicPartitionOffset}");
        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));

            _producer.Dispose();
        }

    }
}

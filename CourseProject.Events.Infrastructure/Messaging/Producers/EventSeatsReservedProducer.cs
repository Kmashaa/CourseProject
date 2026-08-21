using Confluent.Kafka;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Producers
{
    public class EventSeatsReservedProducer : IEventSeatsReservedProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public EventSeatsReservedProducer(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();

        }

        public async Task PublishEventSeatsReserved(Guid bookingId, Guid eventId, CancellationToken ct = default)
        {
            var eventSeatsReserved = new EventSeatsReserved
            {
                BookingId = bookingId,
                EventId = eventId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _producer.ProduceAsync(EventSeatsReserved.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(eventSeatsReserved)
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

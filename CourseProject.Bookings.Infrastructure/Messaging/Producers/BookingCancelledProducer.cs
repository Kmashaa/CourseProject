using Confluent.Kafka;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CourseProject.Bookings.Infrastructure.Messaging.Producers
{
    public class BookingCancelledProducer : IBookingCancelledProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public BookingCancelledProducer(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();

        }

        public async Task PublishBookingCancelled(Guid bookingId, Guid eventId, Guid userId, int numOfSeats = 1, CancellationToken ct = default)
        {
            var bookingCancelled = new BookingCancelled
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = numOfSeats,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _producer.ProduceAsync(BookingCancelled.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCancelled)
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

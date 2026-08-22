using Confluent.Kafka;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Infrastructure.Messaging.Consumers;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Bookings.Infrastructure.Messaging.Producers
{
    public class BookingCancelledProducer : IBookingCancelledProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<BookingCancelledProducer> _logger;


        public BookingCancelledProducer(IConfiguration configuration, ILogger<BookingCancelledProducer> logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;
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

            _logger.LogInformation($"Доставлено: {result.TopicPartitionOffset}");
        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));

            _producer.Dispose();
        }

    }
}

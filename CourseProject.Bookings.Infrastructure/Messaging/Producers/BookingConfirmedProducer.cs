using Confluent.Kafka;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Bookings.Infrastructure.Messaging.Producers
{
    public class BookingConfirmedProducer : IBookingConfirmedProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<BookingConfirmedProducer> _logger;


        public BookingConfirmedProducer(IConfiguration configuration, ILogger<BookingConfirmedProducer> logger)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger = logger;
        }

        public async Task PublishBookingConfirmed(Guid bookingId, Guid eventId, Guid userId, int numOfSeats = 1, CancellationToken ct = default)
        {
            var bookingCreated = new BookingConfirmed
            {
                BookingId = bookingId,
                EventId = eventId,
                UserId = userId,
                NumOfSeats = numOfSeats,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _producer.ProduceAsync(BookingConfirmed.TopicName, new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = JsonSerializer.Serialize(bookingCreated)
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

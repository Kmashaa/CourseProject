using Confluent.Kafka;
using CourseProject.Bookings.Application.Interfaces;
using CourseProject.Bookings.Infrastructure.Messaging.Producers;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Bookings.Infrastructure.Messaging.Consumers
{
    public class EventAvailablenessConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventAvailablenessConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;


        public EventAvailablenessConsumer(
            IConfiguration configuration,
            ILogger<EventAvailablenessConsumer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(
                () => Consume(stoppingToken),
                stoppingToken,
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default).Unwrap(); 
        }

        private async Task Consume(CancellationToken stoppingToken)
        {

            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = _configuration["Kafka:ConsumerGroup"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false

            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();

            consumer.Subscribe([EventSeatsReserved.TopicName, EventSeatsUnavailable.TopicName]);

            _logger.LogInformation("Consumer запущен. Ожидание сообщений из топика 'bookings'...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value == null) continue;

                    try
                    {
                        if (consumeResult.Topic == EventSeatsReserved.TopicName)
                        {
                            var eventSeatsReserved = JsonSerializer.Deserialize<EventSeatsReserved>(consumeResult.Message.Value);
                            if (eventSeatsReserved == null)
                            {
                                _logger.LogWarning("Получено пустое или некорректное сообщение. Пропуск.");
                                FinalizeMessageProcessing(consumer, consumeResult);
                                continue;
                            }

                            _logger.LogInformation($"[{consumeResult.TopicPartitionOffset}] " +
                                          $"Key: {consumeResult.Message.Key} " +
                                          $"Value: {consumeResult.Message.Value}");


                            using (var scope = _scopeFactory.CreateScope())
                            {
                                stoppingToken.ThrowIfCancellationRequested();
                                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                                var bookingConfirmedProducer = scope.ServiceProvider.GetRequiredService<IBookingConfirmedProducer>();
                                var bookingRejectedProducer = scope.ServiceProvider.GetRequiredService<IBookingRejectedProducer>();

                                var booking = await bookingRepository.GetByIdAsync(eventSeatsReserved.BookingId);

                                if (booking == null)
                                {
                                    _logger.LogWarning("BookingId={BookingId} не найдено. Пропуск сообщения.", eventSeatsReserved.BookingId);

                                    await bookingRejectedProducer.PublishBookingRejected(eventSeatsReserved.BookingId, eventSeatsReserved.EventId, eventSeatsReserved.UserId);
                                    FinalizeMessageProcessing(consumer, consumeResult);
                                    continue;

                                }

                                booking.Confirm();
                                await bookingRepository.UpdateAsync(booking);

                                await bookingConfirmedProducer.PublishBookingConfirmed(eventSeatsReserved.BookingId, eventSeatsReserved.EventId, eventSeatsReserved.UserId);
                                _logger.LogInformation("booking confirmed");
                            }
                        }
                        else if (consumeResult.Topic == EventSeatsUnavailable.TopicName)
                        {
                            var eventSeatsUnavailable = JsonSerializer.Deserialize<EventSeatsUnavailable>(consumeResult.Message.Value);
                            if (eventSeatsUnavailable == null)
                            {
                                _logger.LogWarning("Получено пустое или некорректное сообщение. Пропуск.");
                                FinalizeMessageProcessing(consumer, consumeResult);
                                continue;
                            }

                            _logger.LogInformation($"[{consumeResult.TopicPartitionOffset}] " +
                                          $"Key: {consumeResult.Message.Key} " +
                                          $"Value: {consumeResult.Message.Value}");


                            using (var scope = _scopeFactory.CreateScope())
                            {
                                stoppingToken.ThrowIfCancellationRequested();
                                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                                var bookingRejectedProducer = scope.ServiceProvider.GetRequiredService<IBookingRejectedProducer>();

                                var booking = await bookingRepository.GetByIdAsync(eventSeatsUnavailable.BookingId);

                                if (booking == null)
                                {
                                    _logger.LogWarning("BookingId={BookingId} не найдено. Пропуск сообщения.", eventSeatsUnavailable.BookingId);

                                    await bookingRejectedProducer.PublishBookingRejected(eventSeatsUnavailable.BookingId, eventSeatsUnavailable.EventId, eventSeatsUnavailable.UserId); 
                                    FinalizeMessageProcessing(consumer, consumeResult);
                                    continue;

                                }

                                booking.Reject();
                                await bookingRepository.UpdateAsync(booking);

                                await bookingRejectedProducer.PublishBookingRejected(eventSeatsUnavailable.BookingId, eventSeatsUnavailable.EventId, eventSeatsUnavailable.UserId);
                                _logger.LogWarning("booking rejected");
                            }
                        }
                        FinalizeMessageProcessing(consumer, consumeResult);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Ошибка десериализации сообщения Kafka. Сообщение будет пропущено.");
                        FinalizeMessageProcessing(consumer, consumeResult);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка обработки сообщения бизнес-логикой (БД/Продюсер).");
                        FinalizeMessageProcessing(consumer, consumeResult);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer остановлен штатно.");
            }
            finally
            {
                consumer.Close();
            }

            _logger.LogWarning("Consumer не реализован.");
        }

        private void FinalizeMessageProcessing(IConsumer<string, string> consumer, ConsumeResult<string, string> result)
        {
            consumer.StoreOffset(result);
            consumer.Commit(result);
        }

    }

}

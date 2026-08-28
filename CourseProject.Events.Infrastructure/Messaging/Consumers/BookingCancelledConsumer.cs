using Confluent.Kafka;
using CourseProject.Contracts;
using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Infrastructure.Messaging.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Consumers
{
    public class BookingCancelledConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingCancelledConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;


        public BookingCancelledConsumer(
            IConfiguration configuration,
            ILogger<BookingCancelledConsumer> logger,
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

            consumer.Subscribe(BookingCancelled.TopicName);

            _logger.LogInformation("Consumer запущен. Ожидание сообщений из топика 'bookings'...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value == null) continue;

                    try
                    {

                        var bookingCancelled = JsonSerializer.Deserialize<BookingCancelled>(consumeResult.Message.Value);
                        if (bookingCancelled == null)
                        {
                            _logger.LogWarning("Получено пустое или некорректное сообщение. Пропуск.");
                            FinalizeMessageProcessing(consumer, consumeResult);
                            continue;
                        }

                        _logger.LogInformation("[{TopicPartitionOffset}] Key: {Message.Key} Value: {Message.Value}", consumeResult.TopicPartitionOffset, consumeResult.Message.Key, consumeResult.Message.Value);

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            stoppingToken.ThrowIfCancellationRequested();
                            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                            var eventSeatsReleasedProducer = scope.ServiceProvider.GetRequiredService<IEventSeatsReleasedProducer>();


                            var @event = await eventRepository.GetByIdAsync(bookingCancelled.EventId);

                            if (@event == null)
                            {
                                _logger.LogWarning("Событие EventId={EventId} не найдено для BookingId={BookingId}. Пропуск сообщения.", bookingCancelled.EventId, bookingCancelled.BookingId);
                            }
                            else if (@event.StartAt <= DateTime.Now)
                            {
                                _logger.LogWarning("Событие EventId={EventId} уже началось. Резервация невозможна.", @event.Id);
                            }
                            else if (@event.ReleaseSeats())
                            {
                                await eventRepository.UpdateAsync(@event);
                                await eventSeatsReleasedProducer.PublishEventSeatsReleased(bookingCancelled.BookingId, bookingCancelled.EventId, bookingCancelled.UserId, stoppingToken);
                                await cache.SetById(@event.Id, @event);

                                _logger.LogInformation("Seats: {AvailableSeats}", @event.AvailableSeats);
                            }
                            else
                            {
                                _logger.LogWarning("Невозможно освободить места на событие EventId={EventId}.", @event.Id);
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

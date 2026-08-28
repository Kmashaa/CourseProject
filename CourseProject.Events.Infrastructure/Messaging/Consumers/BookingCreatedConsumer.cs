using Confluent.Kafka;
using CourseProject.Contracts;
using CourseProject.Events.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Messaging.Consumers
{
    public class BookingCreatedConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingCreatedConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;


        public BookingCreatedConsumer(
            IConfiguration configuration,
            ILogger<BookingCreatedConsumer> logger,
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

            consumer.Subscribe(BookingCreated.TopicName);

            _logger.LogInformation("Consumer запущен. Ожидание сообщений из топика 'bookings'...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value == null) continue;

                    try
                    {

                        var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(consumeResult.Message.Value);
                        if (bookingCreated == null)
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

                            var eventSeatsReservedProducer = scope.ServiceProvider.GetRequiredService<IEventSeatsReservedProducer>();
                            var eventSeatsUnavailableProducer = scope.ServiceProvider.GetRequiredService<IEventSeatsUnavailableProducer>();

                            var @event = await eventRepository.GetByIdAsync(bookingCreated.EventId);

                            if (@event == null)
                            {
                                _logger.LogWarning("Событие EventId={EventId} не найдено для BookingId={BookingId}. Пропуск сообщения.", bookingCreated.EventId, bookingCreated.BookingId);

                                await eventSeatsUnavailableProducer.PublishEventSeatsUnavailable(bookingCreated.BookingId, bookingCreated.EventId, bookingCreated.UserId, "event doesn't exist", stoppingToken);
                            }
                            else if (@event.StartAt <= DateTime.Now)
                            {
                                _logger.LogWarning("Событие EventId={EventId} уже началось. Резервация невозможна.", @event.Id);

                                await eventSeatsUnavailableProducer.PublishEventSeatsUnavailable(bookingCreated.BookingId, bookingCreated.EventId, bookingCreated.UserId, "event has already started", stoppingToken);
                            }
                            else if (@event.TryReserveSeats())
                            {
                                await eventRepository.UpdateAsync(@event);
                                await eventSeatsReservedProducer.PublishEventSeatsReserved(bookingCreated.BookingId, bookingCreated.EventId, bookingCreated.UserId, stoppingToken);
                                await cache.SetById(@event.Id, @event);

                                _logger.LogInformation("Seats: {AvailableSeats}", @event.AvailableSeats);
                            }
                            else
                            {
                                _logger.LogWarning("Нет свободных мест на событие EventId={EventId}.", @event.Id);

                                await eventSeatsUnavailableProducer.PublishEventSeatsUnavailable(bookingCreated.BookingId, bookingCreated.EventId, bookingCreated.UserId, "no available seats", stoppingToken);
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

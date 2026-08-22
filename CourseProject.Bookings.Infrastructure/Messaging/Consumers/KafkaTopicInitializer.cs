using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CourseProject.Bookings.Infrastructure.Messaging.Consumers
{
    public class KafkaTopicInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaTopicInitializer> _logger;

        public KafkaTopicInitializer(IConfiguration configuration, ILogger<KafkaTopicInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"];
            var topicsToCreate = new List<string>
            {
                EventSeatsReserved.TopicName,
                EventSeatsUnavailable.TopicName
            };

            var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            foreach (var topicName in topicsToCreate)
            {


                try
                {
                    var topicSpecification = new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = 3,
                        ReplicationFactor = 1
                    };

                    await adminClient.CreateTopicsAsync(new List<TopicSpecification> { topicSpecification });
                }
                catch (CreateTopicsException ex)
                {
                    if (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
                    {
                        _logger.LogWarning($"Топик '{topicName}' уже существует. Пропускаем создание.");
                    }
                    else
                    {
                        _logger.LogWarning($"Не удалось создать топик '{topicName}' из-за ошибки Kafka, но запуск продолжается.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Непредвиденная ошибка при инициализации топика '{topicName}'. Запуск продолжается.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }
}

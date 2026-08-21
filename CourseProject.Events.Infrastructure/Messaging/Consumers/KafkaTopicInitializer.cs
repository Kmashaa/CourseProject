using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CourseProject.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CourseProject.Events.Infrastructure.Messaging.Consumers
{
    public class KafkaTopicInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        public KafkaTopicInitializer(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"];
            var topicsToCreate = new List<string>
            {
                BookingCreated.TopicName,
                BookingCancelled.TopicName
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
                        Console.WriteLine($"Топик '{topicName}' уже существует. Пропускаем создание.");
                    }
                    else
                    {
                        Console.WriteLine($"Не удалось создать топик '{topicName}' из-за ошибки Kafka, но запуск продолжается.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Непредвиденная ошибка при инициализации топика '{topicName}'. Запуск продолжается.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }
}

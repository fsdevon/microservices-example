using System;
using Confluent.Kafka;
using MicroservicesExample.BuildingBlocks.EventBus.Abstractions;
using MicroservicesExample.BuildingBlocks.EventBus.Events;
using Microsoft.Extensions.Logging;
using MicroservicesExample.BuildingBlocks.EventBus;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Confluent.Kafka.Admin;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace MicroservicesExample.BuildingBlocks.EventBusKafka
{
    public class EventBusKafka : IEventBus, IDisposable
    {
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly KafkaSettings _settings;
        private readonly ILogger<EventBusKafka> _logger;

        public EventBusKafka(IEventBusSubscriptionsManager subsManager,
            IOptions<KafkaSettings> settings,
            ILogger<EventBusKafka> logger)
        {
            _subsManager = subsManager;
            _settings = settings.Value;
            _logger = logger;

            RegisterTopicSpecification();
        }

        public void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;
            var config = new ProducerConfig { BootstrapServers = _settings.EventBusConnection };

            _logger.LogInformation("Publishing event to Kafka: {EventId} ({EventName})", @event.Id, eventName);

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                producer.ProduceAsync(_settings.Topic, new Message<string, string> { Key = eventName, Value = JsonConvert.SerializeObject(@event) }).GetAwaiter().GetResult();
            }
        }

        public void Subscribe<T, TH>(IServiceProvider serviceProvider)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();

            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

            _subsManager.AddSubscription<T, TH>();
            StartBasicConsume(serviceProvider);
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();

            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Unsubscribing from dynamic event {EventName}", eventName);

            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            _subsManager.Clear();
        }

        private void RegisterTopicSpecification()
        {
            var config = new AdminClientConfig { BootstrapServers = _settings.EventBusConnection };
            using (var adminClent = new AdminClientBuilder(config).Build())
            {
                try
                {
                    var mseTopicSpec = new TopicSpecification
                    {
                        Name = _settings.Topic,
                        ReplicationFactor = _settings.ReplicationFactor,
                        NumPartitions = _settings.NumPartitions
                    };

                    var meta = adminClent.GetMetadata(_settings.Topic, TimeSpan.FromSeconds(20));
                    if (meta.Topics.Count > 0)
                    {
                        var retries = 10;
                        var retry = Policy.Handle<DeleteTopicsException>()
                        .Retry(
                            retryCount: retries,
                            onRetry: (exception, retry) =>
                            {
                                _logger.LogWarning(exception, "Kafka error delete topic {Topic}, detected on attempt {retry} of {retries}", _settings.Topic, retry, retries);
                            });

                        retry.Execute(async () =>
                        {
                            await adminClent.DeleteTopicsAsync(new List<string> { _settings.Topic });
                        });
                    }

                    adminClent.CreateTopicsAsync(new List<TopicSpecification> { mseTopicSpec }).GetAwaiter().GetResult();
                }
                catch (CreateTopicsException ex)
                {
                    if (ex.Results.Select(r => r.Error.Code).Where(el => el != ErrorCode.TopicAlreadyExists && el != ErrorCode.NoError).Count() > 0)
                    {
                        _logger.LogWarning(ex, "Unable to create Kafka topics");
                    }
                }
            }
        }

        private void StartBasicConsume(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("Starting Kafka consume");
            Task.Run(async () => {
                var config = new ConsumerConfig
                {
                    GroupId = _settings.GroupId,
                    BootstrapServers = _settings.EventBusConnection,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoOffsetStore = false,
                    EnableAutoCommit = false
                };

                using (var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogWarning($"Kafka consumer error: {e.Reason}"))
                .Build())
                {
                    consumer.Subscribe(_settings.Topic);
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume();
                                // Complete the message so that it is not received again.
                                if (await ProcessEvent(serviceProvider, consumeResult.Message.Key, consumeResult.Message.Value))
                                {
                                    try
                                    {
                                        consumer.Commit(consumeResult);
                                    }
                                    catch (KafkaException ex)
                                    {
                                        _logger.LogWarning(ex, $"Event {consumeResult.Message.Key} commit error: {ex.Error.Reason}");
                                    }
                                }
                            }
                            catch (ConsumeException ex)
                            {
                                _logger.LogWarning(ex, $"Kafka Consume error: {ex.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogWarning(ex, "Kafa Closing consumer.");
                        consumer.Close();
                    }
                }
            });
        }

        private async Task<bool> ProcessEvent(IServiceProvider serviceProvider, string eventName, string message)
        {
            _logger.LogInformation("Processing Kafka event: {EventName}", eventName);
            var processed = false;
            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    var handler = serviceProvider.GetRequiredService(subscription.HandlerType);
                    if (handler == null) continue;
                    var eventType = _subsManager.GetEventTypeByName(eventName);
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
                processed = true;
            }
            return processed;
        }
    }
}

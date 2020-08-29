using System;
namespace MicroservicesExample.BuildingBlocks.EventBusKafka
{
    public class KafkaSettings
    {
        public string EventBusConnection { get; set; }
        public string Topic { get; set; } = "mse_event_bus";
        public string GroupId { get; set; } = "mse-consumer-group";
        public short ReplicationFactor { get; set; } = 1;
        public short NumPartitions { get; set; } = 1;
    }
}

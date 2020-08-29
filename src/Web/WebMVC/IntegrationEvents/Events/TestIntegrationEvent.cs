using System;
using MicroservicesExample.BuildingBlocks.EventBus.Events;

namespace MicroservicesExample.Web.WebMVC.IntegrationEvents.Events
{
    public class TestIntegrationEvent : IntegrationEvent
    {
        public int OrderId { get; }

        public TestIntegrationEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}

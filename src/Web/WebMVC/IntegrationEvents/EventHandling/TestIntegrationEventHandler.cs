using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MicroservicesExample.BuildingBlocks.EventBus.Abstractions;
using MicroservicesExample.Web.WebMVC.IntegrationEvents.Events;
using Microsoft.Extensions.Logging;

namespace MicroservicesExample.Web.WebMVC.IntegrationEvents.EventHandling
{
    public class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        private readonly ILogger<TestIntegrationEventHandler> _logger;

        public TestIntegrationEventHandler(ILogger<TestIntegrationEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(TestIntegrationEvent @event)
        {
            _logger.LogInformation("tesst ====================================" + @event.Id);
            return Task.CompletedTask;
        }
    }
}

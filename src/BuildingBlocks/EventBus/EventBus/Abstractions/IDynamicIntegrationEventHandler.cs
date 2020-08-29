using System;
using System.Threading.Tasks;

namespace MicroservicesExample.BuildingBlocks.EventBus.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}

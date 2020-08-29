using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using MicroservicesExample.BuildingBlocks.EventBus.Abstractions;
using MicroservicesExample.Web.WebMVC.IntegrationEvents.Events;
using MicroservicesExample.Web.WebMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MicroservicesExample.Web.WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEventBus _eventBus;

        public HomeController(ILogger<HomeController> logger, IEventBus eventBus)
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        public IActionResult Index()
        {
            return View();
        }


        [Authorize(AuthenticationSchemes = "OpenIdConnect")]
        public IActionResult Privacy()
        {
            var testEvent = new TestIntegrationEvent(1);
            _eventBus.Publish(testEvent);
            //var conf = new ProducerConfig { BootstrapServers = "broker:29092" };

            //Action<DeliveryReport<Null, string>> handler = r =>
            //    Debug.WriteLine(!r.Error.IsError
            //        ? $"Delivered message to {r.TopicPartitionOffset}"
            //        : $"Delivery Error: {r.Error.Reason}");

            //using (var p = new ProducerBuilder<Null, string>(conf).Build())
            //{
            //    for (int i = 0; i < 2; ++i)
            //    {
            //        p.Produce("my-topic", new Message<Null, string> { Value = i.ToString() }, handler);
            //    }

            //    // wait for up to 10 seconds for any inflight messages to be delivered.
            //    //p.Flush(TimeSpan.FromSeconds(10));
            //}
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using System;
namespace MicroservicesExample.ApiGateways.WebBff.Aggregator.Config
{
    public class UrlsConfig
    {
        public string Basket { get; set; }
        public string Catalog { get; set; }
        public string Orders { get; set; }
        public string GrpcBasket { get; set; }
        public string GrpcCatalog { get; set; }
        public string GrpcOrdering { get; set; }
    }
}

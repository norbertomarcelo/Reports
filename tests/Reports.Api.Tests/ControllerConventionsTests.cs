using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Reports.Api.Tests;

public class ControllerConventionsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Endpoint_de_relatorio_esta_marcado_como_ApiController()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;

        var endpoint = Assert.Single(endpoints.OfType<RouteEndpoint>(),
            e => e.RoutePattern.RawText == "report");

        Assert.NotNull(endpoint.Metadata.GetMetadata<ApiControllerAttribute>());
    }
}

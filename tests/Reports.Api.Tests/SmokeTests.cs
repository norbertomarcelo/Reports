using System.Net;

namespace Reports.Api.Tests;

public class SmokeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SmokeTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Swagger_responde_em_development()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

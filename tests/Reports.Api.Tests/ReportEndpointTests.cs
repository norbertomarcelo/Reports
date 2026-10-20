using System.Net;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Tests.Fakes;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class ReportEndpointTests
{
    [Theory]
    [InlineData("""{ "include": { "projects": true } }""")]
    [InlineData("""{ "Include": { "PROJECTS": true } }""")]
    public async Task Retorna_pdf_com_os_projetos(string body)
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "p1", Name = "Projeto Alfa", State = "wellFormed", Visibility = "private" }]
        };
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route, ReportHost.Json(body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("index.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(FakePdfRenderer.Output, await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("Projeto Alfa", pdf.Html);
    }

    [Fact]
    public async Task Get_nao_e_mais_permitido()
    {
        using var factory = ReportHost.Create(new FakeAzureDevOpsApi(), new FakePdfRenderer());

        var response = await factory.CreateClient().GetAsync(ReportHost.Route);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("POST", response.Content.Headers.Allow);
    }
}

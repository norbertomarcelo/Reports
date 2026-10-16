using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Reports.Api.Interfaces;
using Reports.Api.Models;
using Reports.Api.Services.Pdf;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class ProjectReportEndpointTests
{
    [Fact]
    public async Task Retorna_pdf_com_os_projetos()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Name = "Projeto Alfa", State = "wellFormed", Visibility = "private" }]
        };
        var pdf = new FakePdfRenderer();

        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAzureDevOpsApi>(api);
                services.AddSingleton<IPdfRenderer>(pdf);
            }));

        var response = await factory.CreateClient().GetAsync("/report");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("index.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(FakePdfRenderer.Output, await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("Projeto Alfa", pdf.Html);
    }
}

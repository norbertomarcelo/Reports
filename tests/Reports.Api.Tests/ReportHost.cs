using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Reports.Api.Interfaces;
using Reports.Api.Services.Pdf;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

/// <summary>Monta o host de teste com o Azure DevOps e o renderizador de PDF simulados.</summary>
public static class ReportHost
{
    public const string Route = "/azure-devops";

    public static WebApplicationFactory<Program> Create(FakeAzureDevOpsApi api, FakePdfRenderer pdf) =>
        new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAzureDevOpsApi>(api);
                services.AddSingleton<IPdfRenderer>(pdf);
            }));

    public static StringContent Json(string body, string mediaType = "application/json") =>
        new(body, Encoding.UTF8, mediaType);
}

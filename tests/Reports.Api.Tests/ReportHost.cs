using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reports.AzureDevOps.Interfaces;
using Reports.Api.Services.Pdf;
using Reports.AzureDevOps.Tests.Fakes;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

/// <summary>Monta o host de teste com o Azure DevOps e o renderizador de PDF simulados.</summary>
public static class ReportHost
{
    public const string Route = "/azure-devops";

    public static WebApplicationFactory<Program> Create(
        FakeAzureDevOpsApi api, FakePdfRenderer pdf, params string[] internalDomains) =>
        Create(api, pdf, new FakeAzureDevOpsAnalyticsApi(), internalDomains);

    /// <param name="internalDomains">Valor de <c>AzureDevOps:InternalDomains</c>; vazio = chave ausente.</param>
    public static WebApplicationFactory<Program> Create(
        FakeAzureDevOpsApi api, FakePdfRenderer pdf, FakeAzureDevOpsAnalyticsApi analytics,
        params string[] internalDomains) =>
        new ApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(internalDomains.Select((domain, i) =>
                    new KeyValuePair<string, string?>($"AzureDevOps:InternalDomains:{i}", domain))));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAzureDevOpsApi>(api);
                services.AddSingleton<IAzureDevOpsAnalyticsApi>(analytics);
                services.AddSingleton<IPdfRenderer>(pdf);
            });
        });

    public static StringContent Json(string body, string mediaType = "application/json") =>
        new(body, Encoding.UTF8, mediaType);
}

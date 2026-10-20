using Microsoft.Extensions.Configuration;
using Reports.AzureDevOps.Settings;

namespace Reports.AzureDevOps.Tests;

public class AzureDevOpsOptionsDefaultsTests
{
    [Fact]
    public void Usa_versao_preview_padrao_quando_a_chave_nao_e_configurada()
    {
        Assert.Equal("7.1-preview.1", new AzureDevOpsOptions().ProjectPropertiesApiVersion);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AzureDevOps:Organization"] = "org" })
            .Build();
        var options = configuration.GetSection(AzureDevOpsOptions.SectionName).Get<AzureDevOpsOptions>()!;

        Assert.Equal("7.1-preview.1", options.ProjectPropertiesApiVersion);
    }

    [Fact]
    public void Usa_o_host_padrao_do_Analytics_quando_a_chave_nao_e_configurada()
    {
        Assert.Equal("https://analytics.dev.azure.com", new AzureDevOpsOptions().AnalyticsServer);
    }
}

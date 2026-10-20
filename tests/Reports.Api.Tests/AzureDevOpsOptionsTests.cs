using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reports.AzureDevOps.Settings;

namespace Reports.Api.Tests;

public class AzureDevOpsOptionsTests
{
    [Fact]
    public void Vincula_secao_AzureDevOps()
    {
        using var factory = new ApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        Assert.Equal(ApiFactory.CoreServer, options.CoreServer);
        Assert.Equal(ApiFactory.Organization, options.Organization);
        Assert.Equal(ApiFactory.ApiVersion, options.ApiVersion);
        Assert.Equal(ApiFactory.AccessToken, options.AccessToken);
    }

    [Fact]
    public void Le_a_versao_da_api_de_propriedades_do_projeto_da_configuracao()
    {
        using var factory = new ApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        Assert.Equal(ApiFactory.ProjectPropertiesApiVersion, options.ProjectPropertiesApiVersion);
    }

    [Fact]
    public void Sobe_sem_InternalDomains_com_a_lista_vazia()
    {
        using var factory = new ApiFactory();

        factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        Assert.Empty(options.InternalDomains);
    }

    [Fact]
    public void Le_InternalDomains_da_configuracao()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AzureDevOps:InternalDomains:0"] = "empresa.com.br",
                    ["AzureDevOps:InternalDomains:1"] = "empresa.com"
                })));

        var options = factory.Services.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        Assert.Equal(["empresa.com.br", "empresa.com"], options.InternalDomains);
    }

    [Fact]
    public void Le_o_host_do_Analytics_da_configuracao()
    {
        using var factory = new ApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;

        Assert.Equal(ApiFactory.AnalyticsServer, options.AnalyticsServer);
    }

    [Fact]
    public void Falha_ao_iniciar_com_AnalyticsServer_que_nao_e_url()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AzureDevOps:AnalyticsServer"] = "nao-e-url"
                })));

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains(nameof(AzureDevOpsOptions.AnalyticsServer), ex.Message);
    }

    [Fact]
    public void Falha_ao_iniciar_sem_AccessToken()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AzureDevOps:AccessToken"] = ""
                })));

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains(nameof(AzureDevOpsOptions.AccessToken), ex.Message);
    }
}

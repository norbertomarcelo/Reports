using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reports.Api.Settings;

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

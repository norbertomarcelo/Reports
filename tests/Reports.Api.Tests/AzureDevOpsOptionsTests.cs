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

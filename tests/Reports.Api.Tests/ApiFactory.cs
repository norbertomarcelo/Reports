using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Reports.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string CoreServer = "https://dev.azure.example";
    public const string Organization = "org-teste";
    public const string ApiVersion = "7.1";
    public const string AccessToken = "pat-teste";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureDevOps:CoreServer"] = CoreServer,
                ["AzureDevOps:Organization"] = Organization,
                ["AzureDevOps:ApiVersion"] = ApiVersion,
                ["AzureDevOps:AccessToken"] = AccessToken
            }));
    }
}

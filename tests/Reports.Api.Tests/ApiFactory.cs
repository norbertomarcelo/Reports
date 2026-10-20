using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Reports.AzureDevOps.Tests;

namespace Reports.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string CoreServer = TestSettings.CoreServer;
    public const string Organization = TestSettings.Organization;
    public const string AnalyticsServer = TestSettings.AnalyticsServer;
    public const string ApiVersion = TestSettings.ApiVersion;
    public const string ProjectPropertiesApiVersion = TestSettings.ProjectPropertiesApiVersion;
    public const string AccessToken = TestSettings.AccessToken;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureDevOps:CoreServer"] = CoreServer,
                ["AzureDevOps:AnalyticsServer"] = AnalyticsServer,
                ["AzureDevOps:Organization"] = Organization,
                ["AzureDevOps:ApiVersion"] = ApiVersion,
                ["AzureDevOps:ProjectPropertiesApiVersion"] = ProjectPropertiesApiVersion,
                ["AzureDevOps:AccessToken"] = AccessToken
            }));
    }
}

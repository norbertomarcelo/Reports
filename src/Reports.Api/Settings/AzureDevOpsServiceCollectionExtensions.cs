using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Refit;
using Reports.Api.Interfaces;

namespace Reports.Api.Settings;

public static class AzureDevOpsServiceCollectionExtensions
{
    /// <summary>Registra o cliente da API REST e o do Analytics (outro host, mesmo PAT). Devolve o builder do primeiro.</summary>
    public static IHttpClientBuilder AddAzureDevOpsClient(this IServiceCollection services)
    {
        services.AddRefitClient<IAzureDevOpsAnalyticsApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
                Configure(client, serviceProvider, options => options.AnalyticsServer));

        return services.AddRefitClient<IAzureDevOpsApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
                Configure(client, serviceProvider, options => options.CoreServer));
    }

    private static void Configure(HttpClient client, IServiceProvider serviceProvider, Func<AzureDevOpsOptions, string> server)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{options.AccessToken}"));

        client.BaseAddress = new Uri(server(options).TrimEnd('/'));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }
}

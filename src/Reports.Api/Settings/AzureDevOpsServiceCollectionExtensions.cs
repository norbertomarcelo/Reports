using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Refit;
using Reports.Api.Interfaces;

namespace Reports.Api.Settings;

public static class AzureDevOpsServiceCollectionExtensions
{
    public static IHttpClientBuilder AddAzureDevOpsClient(this IServiceCollection services)
    {
        return services.AddRefitClient<IAzureDevOpsApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{options.AccessToken}"));

                client.BaseAddress = new Uri(options.CoreServer.TrimEnd('/'));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            });
    }
}

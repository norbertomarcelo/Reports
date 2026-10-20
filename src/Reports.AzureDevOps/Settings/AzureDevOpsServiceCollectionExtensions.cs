using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Reports.AzureDevOps.Interfaces;

namespace Reports.AzureDevOps.Settings;

public static class AzureDevOpsServiceCollectionExtensions
{
    /// <summary>Registra o cliente da API REST e o do Analytics (outro host, mesmo PAT). Devolve o builder do primeiro.</summary>
    public static IHttpClientBuilder AddAzureDevOpsClient(this IServiceCollection services)
    {
        var settings = new RefitSettings { UrlParameterFormatter = new LowercaseBooleanUrlParameterFormatter() };

        services.AddRefitClient<IAzureDevOpsAnalyticsApi>(settings)
            .ConfigureHttpClient((serviceProvider, client) =>
                Configure(client, serviceProvider, options => options.AnalyticsServer));

        return services.AddRefitClient<IAzureDevOpsApi>(settings)
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

    /// <summary>O Refit escreve booleanos na URL com <c>ToString()</c> (<c>True</c>); a documentação do Azure DevOps usa <c>true</c>.</summary>
    private sealed class LowercaseBooleanUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        public override string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) =>
            value is bool flag ? (flag ? "true" : "false") : base.Format(value, attributeProvider, type);
    }
}

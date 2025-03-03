namespace Reports.Api.Settings;

public class HttpClientFactorySettings
{
    public IServiceCollection AddHttpConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("AzureDevOps", client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>("AzureDevOps:CoreSerever"));
            client.DefaultRequestHeaders.Add("Authorization", configuration.GetValue<string>("AzureDevOps:AccessToken"));
        });
        return services;
    }
}

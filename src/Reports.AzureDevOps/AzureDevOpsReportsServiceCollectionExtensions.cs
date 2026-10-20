using Microsoft.Extensions.DependencyInjection;
using Reports.AzureDevOps.Services;
using Reports.AzureDevOps.Settings;

namespace Reports.AzureDevOps;

public static class AzureDevOpsReportsServiceCollectionExtensions
{
    /// <summary>
    /// Registra tudo o que o relatório do Azure DevOps precisa: opções validadas na partida,
    /// clientes Refit e serviços. O PDF fica por conta de quem usa a biblioteca (spec 019).
    /// </summary>
    public static IServiceCollection AddAzureDevOpsReports(this IServiceCollection services)
    {
        services.AddOptions<AzureDevOpsOptions>()
            .BindConfiguration(AzureDevOpsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddAzureDevOpsClient();

        services.AddScoped<AzureDevOpsService>();
        services.AddScoped<AzureDevOpsReportService>();
        return services;
    }
}

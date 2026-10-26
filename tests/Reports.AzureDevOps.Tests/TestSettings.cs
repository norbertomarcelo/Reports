namespace Reports.AzureDevOps.Tests;

/// <summary>Valores fictícios da configuração do Azure DevOps usados pelos testes.</summary>
public static class TestSettings
{
    public const string CoreServer = "https://dev.azure.example";
    public const string Organization = "org-teste";
    public const string AnalyticsServer = "https://analytics.example";
    public const string ApiVersion = "7.1";
    public const string ProjectPropertiesApiVersion = "7.1-preview.9";
    public const string AccessToken = "pat-teste";

    /// <summary>Instante fixo da geração do relatório: 26/10/2026 15:42 UTC (código <c>AD-20261026-1542</c>).</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 26, 15, 42, 0, TimeSpan.Zero);
}

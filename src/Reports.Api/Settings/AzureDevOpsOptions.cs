using System.ComponentModel.DataAnnotations;

namespace Reports.Api.Settings;

public class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    [Required, Url]
    public string CoreServer { get; set; } = "https://dev.azure.com";

    [Required]
    public string Organization { get; set; } = string.Empty;

    [Required]
    public string ApiVersion { get; set; } = "7.1";

    /// <summary>O endpoint de propriedades do projeto só existe como versão preview na 7.1.</summary>
    [Required]
    public string ProjectPropertiesApiVersion { get; set; } = "7.1-preview.1";

    /// <summary>Host do Analytics (OData), usado para contar itens de trabalho já agregados (spec 017).</summary>
    [Required, Url]
    public string AnalyticsServer { get; set; } = "https://analytics.dev.azure.com";

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Domínios de e-mail da organização; quem estiver fora é marcado como externo. Opcional (spec 016).</summary>
    public string[] InternalDomains { get; set; } = [];
}

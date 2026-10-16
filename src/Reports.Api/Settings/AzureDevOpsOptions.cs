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

    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

using System.Text.Json;
using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Services;

/// <summary>Valores das propriedades do projeto já traduzidos para o relatório; <c>null</c> = sem valor.</summary>
public record ProjectDetails(
    string? Process,
    string? SourceControl,
    string? GitEnabled,
    string? GitPermissionsInitialized);

/// <summary>Escolhe e traduz, entre as propriedades do projeto, as que entram no relatório.</summary>
internal static class ProjectPropertiesReader
{
    private const string ProcessTemplate = "System.Process Template";
    private const string SourceControlCapabilityFlags = "System.SourceControlCapabilityFlags";
    private const string SourceControlGitEnabled = "System.SourceControlGitEnabled";
    private const string SourceControlGitPermissionsInitialized = "System.SourceControlGitPermissionsInitialized";

    public static ProjectDetails Read(IEnumerable<ProjectProperty> properties)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in properties)
        {
            var text = AsText(property.Value);
            if (text is not null)
                values.TryAdd(property.Name, text);
        }

        values.TryGetValue(ProcessTemplate, out var process);
        values.TryGetValue(SourceControlCapabilityFlags, out var flags);
        values.TryGetValue(SourceControlGitEnabled, out var gitEnabled);
        values.TryGetValue(SourceControlGitPermissionsInitialized, out var gitPermissions);

        return new ProjectDetails(
            string.IsNullOrWhiteSpace(process) ? null : process,
            flags switch
            {
                "1" => "TFVC",
                "2" => "Git",
                "3" => "Git e TFVC",
                _ => null
            },
            YesOrNo(gitEnabled),
            YesOrNo(gitPermissions));
    }

    private static string? YesOrNo(string? value) =>
        bool.TryParse(value, out var result) ? (result ? "Sim" : "Não") : null;

    private static string? AsText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
        _ => null
    };
}

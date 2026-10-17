using System.Text.Json;

namespace Reports.Api.Models;

public class ProjectProperty
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Valor como veio no JSON: o Azure DevOps o tipa como objeto (string, número ou booleano).</summary>
    public JsonElement Value { get; set; }
}

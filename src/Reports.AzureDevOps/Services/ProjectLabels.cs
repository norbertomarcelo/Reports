namespace Reports.AzureDevOps.Services;

/// <summary>Traduz o estado e a visibilidade do projeto (enums do Azure DevOps) para o relatório.</summary>
internal static class ProjectLabels
{
    private static readonly Dictionary<string, string> States = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wellFormed"] = "Ativo",
        ["new"] = "Em criação",
        ["createPending"] = "Criação pendente",
        ["deleting"] = "Em exclusão",
        ["deleted"] = "Excluído",
        ["unchanged"] = "Sem alteração"
    };

    private static readonly Dictionary<string, string> Visibilities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["private"] = "Privado",
        ["public"] = "Público"
    };

    /// <summary>Devolve <c>null</c> para valor ausente ou não reconhecido.</summary>
    public static string? State(string? value) => Translate(States, value);

    /// <summary>Devolve <c>null</c> para valor ausente ou não reconhecido.</summary>
    public static string? Visibility(string? value) => Translate(Visibilities, value);

    private static string? Translate(Dictionary<string, string> map, string? value) =>
        value is not null && map.TryGetValue(value, out var label) ? label : null;
}

using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Services;

/// <summary>Linha de contagem: abertos e fechados de um tipo, de uma área ou de um projeto.</summary>
public record CountRow(string Name, long Open, long Closed)
{
    public long Total => Open + Closed;
}

/// <summary>Contagem de um projeto. <c>Partial</c>: o Analytics devolveu mais de uma página (spec 017, RF-09).</summary>
public record WorkItemSummary(
    long Open, long Closed, IReadOnlyList<CountRow> ByType, IReadOnlyList<CountRow> ByArea, bool Partial)
{
    public long Total => Open + Closed;
}

/// <summary>
/// Soma os grupos devolvidos pelo Analytics (spec 017). Fechado = categoria de estado <c>Completed</c>;
/// removido não conta; o resto é aberto. A categoria vale para qualquer processo, ao contrário do nome do estado.
/// </summary>
internal static class WorkItemCounter
{
    public const string NoArea = "(sem área)";
    public const string NoType = "(sem tipo)";

    public static bool IsClosed(string? stateCategory) =>
        string.Equals(stateCategory, "Completed", StringComparison.OrdinalIgnoreCase);

    public static WorkItemSummary Summarize(WorkItemGroups source)
    {
        var groups = source.Groups
            .Where(g => !string.Equals(g.StateCategory, "Removed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new WorkItemSummary(
            groups.Where(g => !IsClosed(g.StateCategory)).Sum(g => g.Count),
            groups.Where(g => IsClosed(g.StateCategory)).Sum(g => g.Count),
            Rows(groups, g => string.IsNullOrWhiteSpace(g.WorkItemType) ? NoType : g.WorkItemType),
            Rows(groups, g => string.IsNullOrWhiteSpace(g.Area?.AreaPath) ? NoArea : g.Area.AreaPath),
            source.Partial);
    }

    private static List<CountRow> Rows(List<WorkItemGroup> groups, Func<WorkItemGroup, string> key) =>
        groups
            .GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CountRow(
                g.Key,
                g.Where(x => !IsClosed(x.StateCategory)).Sum(x => x.Count),
                g.Where(x => IsClosed(x.StateCategory)).Sum(x => x.Count)))
            .OrderBy(r => r.Name, ReportComparer.PtBr)
            .ToList();
}

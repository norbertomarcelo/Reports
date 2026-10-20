namespace Reports.AzureDevOps.Models;

/// <summary>Grupo devolvido pelo Analytics: quantos itens há de um tipo, categoria de estado e área.</summary>
public class WorkItemGroup
{
    public string? WorkItemType { get; set; }

    /// <summary>Proposed, InProgress, Resolved, Completed ou Removed (vale para qualquer processo).</summary>
    public string? StateCategory { get; set; }

    public WorkItemArea? Area { get; set; }
    public long Count { get; set; }
}

public class WorkItemArea
{
    public string? AreaPath { get; set; }
}

/// <summary><c>Partial</c>: o Analytics indicou mais páginas (<c>@odata.nextLink</c>) e só a primeira foi lida.</summary>
public record WorkItemGroups(IReadOnlyList<WorkItemGroup> Groups, bool Partial);

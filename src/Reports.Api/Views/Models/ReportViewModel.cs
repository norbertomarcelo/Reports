using Reports.Api.Models;

namespace Reports.Api.Views.Models;

/// <summary>
/// Dados do relatório. Uma seção <c>null</c> não foi pedida; uma lista vazia foi pedida mas não tem dados.
/// </summary>
public class ReportViewModel
{
    public IReadOnlyList<Project>? Projects { get; init; }
    public IReadOnlyList<TeamRow>? Teams { get; init; }
    public IReadOnlyList<UserRow>? Users { get; init; }
}

public record TeamRow(string Project, string Team, string? Description);

public record UserRow(string Project, string Team, string Name, string? Email, bool IsAdmin);

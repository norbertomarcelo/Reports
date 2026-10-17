using Reports.Api.Services;

namespace Reports.Api.Views.Models;

/// <summary>
/// Dados do relatório, já na hierarquia de capítulos e com a numeração calculada.
/// <c>Projects</c> <c>null</c> = capítulo de projetos não pedido.
/// </summary>
public class ReportViewModel
{
    public Chapter<IReadOnlyList<ProjectRow>>? Projects { get; init; }
    public IReadOnlyList<ProjectChapter> ProjectChapters { get; init; } = [];

    /// <summary>Organização sem projetos e capítulo de projetos não pedido: só a mensagem.</summary>
    public bool NoProjects { get; init; }
}

/// <summary>Conteúdo numerado de um nível do relatório (ex.: <c>"2.1"</c>).</summary>
public record Chapter<T>(string Number, T Content);

/// <summary><c>Teams</c> e <c>UsersByTeam</c> <c>null</c> = subcapítulo não pedido.</summary>
public record ProjectChapter(
    string Number,
    string Name,
    Chapter<IReadOnlyList<TeamRow>>? Teams,
    Chapter<IReadOnlyList<TeamUsers>>? UsersByTeam);

public record TeamUsers(string Number, string Team, IReadOnlyList<UserRow> Users);

public record ProjectRow(
    string Name,
    string? State,
    string? Visibility,
    DateTime LastUpdateTime,
    ProjectDetails Details);

public record TeamRow(string Team, string? Description);

public record UserRow(string Name, string? Email, bool IsAdmin);

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

/// <summary><c>Teams</c>, <c>UsersByTeam</c> e <c>ProjectUsers</c> <c>null</c> = subcapítulo não pedido.</summary>
public record ProjectChapter(
    string Number,
    string Name,
    Chapter<IReadOnlyList<TeamRow>>? Teams,
    Chapter<IReadOnlyList<TeamUsers>>? UsersByTeam,
    Chapter<IReadOnlyList<ProjectUserRow>>? ProjectUsers);

public record TeamUsers(string Number, string Team, IReadOnlyList<UserRow> Users);

public record ProjectRow(
    string Name,
    string? State,
    string? Visibility,
    DateTime LastUpdateTime,
    ProjectDetails Details);

/// <summary>
/// <c>MemberCount</c> e <c>AdminCount</c> <c>null</c> = membros não buscados (sem <c>users</c>); grupos não contam.
/// <c>NoAdmin</c>: o time tem membros (grupos incluídos) e nenhum administrador.
/// </summary>
public record TeamRow(string Team, string? Description, int? MemberCount, int? AdminCount, bool NoAdmin);

public record UserRow(string Name, string? Email, bool IsAdmin, bool IsGroup, bool IsExternal);

/// <summary>Usuário do projeto, uma vez só, com os times de que participa e onde é administrador.</summary>
public record ProjectUserRow(
    string Name,
    string? Email,
    bool IsExternal,
    IReadOnlyList<string> Teams,
    IReadOnlyList<string> AdminOf);

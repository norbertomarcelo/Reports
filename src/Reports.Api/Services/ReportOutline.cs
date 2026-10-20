using Reports.Api.Dtos.Requests;

namespace Reports.Api.Services;

/// <summary>Numeração dos capítulos: <c>null</c> = o nível não existe no relatório.</summary>
public record Outline(string? Projects, IReadOnlyList<ProjectOutline> ProjectChapters, string? WorkItems);

/// <summary>
/// <c>TeamNumbers</c> só é preenchida com a seção de usuários (um número por time);
/// <c>ProjectUsers</c> também só existe com ela (spec 016). <c>WorkItems</c> vem depois (spec 017) e <c>Repositories</c> é o último subcapítulo (spec 018).
/// </summary>
public record ProjectOutline(
    string Chapter,
    string? Teams,
    string? UsersByTeam,
    IReadOnlyList<string> TeamNumbers,
    string? ProjectUsers,
    string? WorkItems,
    string? Repositories);

/// <summary>Calcula a numeração sequencial sobre o que foi incluído no relatório (spec 015, RF-03).</summary>
public static class ReportOutline
{
    /// <param name="teamCounts">Quantidade de times de cada projeto, na ordem de exibição dos capítulos.</param>
    public static Outline Build(ReportSections sections, IReadOnlyList<int> teamCounts)
    {
        var chapter = 0;
        var projects = sections.Projects ? (++chapter).ToString() : null;
        var workItemsOverview = sections.WorkItems ? (++chapter).ToString() : null;

        var chapters = new List<ProjectOutline>();
        if (sections.Teams || sections.Users || sections.WorkItems || sections.Repositories)
        {
            foreach (var teamCount in teamCounts)
            {
                var number = ++chapter;
                var sub = 0;
                var teams = sections.Teams ? $"{number}.{++sub}" : null;
                string? usersByTeam = null;
                string? projectUsers = null;
                var teamNumbers = new List<string>();
                if (sections.Users)
                {
                    usersByTeam = $"{number}.{++sub}";
                    for (var team = 1; team <= teamCount; team++)
                        teamNumbers.Add($"{usersByTeam}.{team}");
                    projectUsers = $"{number}.{++sub}";
                }

                var workItems = sections.WorkItems ? $"{number}.{++sub}" : null;
                var repositories = sections.Repositories ? $"{number}.{++sub}" : null;

                chapters.Add(new ProjectOutline(
                    number.ToString(), teams, usersByTeam, teamNumbers, projectUsers, workItems, repositories));
            }
        }

        return new Outline(projects, chapters, workItemsOverview);
    }
}

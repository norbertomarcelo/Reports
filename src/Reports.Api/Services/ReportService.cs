using Microsoft.Extensions.Options;
using Razor.Templating.Core;
using Reports.Api.Dtos.Requests;
using Reports.Api.Models;
using Reports.Api.Services.Pdf;
using Reports.Api.Settings;
using Reports.Api.Views.Models;

namespace Reports.Api.Services;

public class ReportService
{
    private static readonly StringComparer Comparer = ReportComparer.PtBr;

    private readonly AzureDevOpsService _azureDevOps;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly string[] _internalDomains;

    public ReportService(AzureDevOpsService azureDevOps, IPdfRenderer pdfRenderer, IOptions<AzureDevOpsOptions> options)
    {
        _azureDevOps = azureDevOps;
        _pdfRenderer = pdfRenderer;
        _internalDomains = options.Value.InternalDomains;
    }

    public async Task<byte[]> GenerateReportAsync(ReportSections sections)
    {
        var model = await BuildAsync(sections);
        var html = await RazorTemplateEngine.RenderAsync("Views/Index.cshtml", model);
        return await _pdfRenderer.RenderAsync(html);
    }

    private async Task<ReportViewModel> BuildAsync(ReportSections sections)
    {
        var projects = (await _azureDevOps.GetAllProjectsAsync()).ToList();

        var projectRows = new List<ProjectRow>();
        if (sections.Projects)
        {
            // Uma chamada por vez, na ordem da listagem (sem Task.WhenAll), por decisão da spec 013.
            foreach (var project in projects)
            {
                var properties = await _azureDevOps.GetProjectPropertiesAsync(project.Id);
                projectRows.Add(new ProjectRow(
                    project.Name,
                    ProjectLabels.State(project.State),
                    ProjectLabels.Visibility(project.Visibility),
                    project.LastUpdateTime,
                    ProjectPropertiesReader.Read(properties)));
            }
        }

        // Times por projeto e membros por time (chaves: id), buscados só quando alguma seção precisa.
        var teamsByProject = new Dictionary<string, List<Team>>();
        if (sections.Teams || sections.Users)
        {
            foreach (var project in projects)
                teamsByProject[project.Id] = (await _azureDevOps.GetTeamsPerProjectAsync(project.Id)).ToList();
        }

        var membersByTeam = new Dictionary<string, List<TeamMember>>();
        if (sections.Users)
        {
            foreach (var project in projects)
            {
                foreach (var team in teamsByProject[project.Id])
                    membersByTeam[team.Id] = (await _azureDevOps.GetTeamMembersAsync(project.Id, team.Id)).ToList();
            }
        }

        // Contagem de itens por projeto: uma chamada ao Analytics por vez, na ordem da listagem (spec 017, RF-08).
        var summaryByProject = new Dictionary<string, WorkItemSummary>();
        if (sections.WorkItems)
        {
            foreach (var project in projects)
                summaryByProject[project.Id] =
                    WorkItemCounter.Summarize(await _azureDevOps.GetWorkItemGroupsAsync(project.Name));
        }

        // Repositórios: uma listagem da organização; commits (mais recente e mais antigo) um repositório por vez,
        // só para os que têm branch padrão e não estão desativados (spec 018, RF-06).
        var repositoriesByProject = new Dictionary<string, List<RepositoryRow>>();
        if (sections.Repositories)
        {
            foreach (var repository in await _azureDevOps.GetAllRepositoriesAsync())
            {
                var projectId = repository.Project?.Id;
                if (projectId is null)
                    continue;

                if (!repositoriesByProject.TryGetValue(projectId, out var rows))
                    repositoriesByProject[projectId] = rows = [];
                rows.Add(await ToRepositoryRowAsync(projectId, repository));
            }
        }

        var orderedProjects = projects.OrderBy(p => p.Name, Comparer).ToList();
        var outline = ReportOutline.Build(sections,
            orderedProjects.Select(p => teamsByProject.GetValueOrDefault(p.Id)?.Count ?? 0).ToList());

        var chapters = new List<ProjectChapter>();
        for (var i = 0; i < outline.ProjectChapters.Count; i++)
        {
            var project = orderedProjects[i];
            var numbers = outline.ProjectChapters[i];
            var teams = (teamsByProject.GetValueOrDefault(project.Id) ?? []).OrderBy(t => t.Name, Comparer).ToList();

            // Membros do time já classificados (grupo/externo); vazio quando `users` não foi pedido.
            var members = teams.ToDictionary(t => t.Id, t => membersByTeam.TryGetValue(t.Id, out var list)
                ? list.Select(ToUserRow).OrderBy(u => u.Row.Name, Comparer).ToList()
                : []);

            chapters.Add(new ProjectChapter(
                numbers.Chapter,
                project.Name,
                numbers.Teams is null
                    ? null
                    : new Chapter<IReadOnlyList<TeamRow>>(numbers.Teams,
                        teams.Select(t => ToTeamRow(t, sections.Users ? members[t.Id] : null)).ToList()),
                numbers.UsersByTeam is null
                    ? null
                    : new Chapter<IReadOnlyList<TeamUsers>>(numbers.UsersByTeam,
                        teams.Select((team, n) => new TeamUsers(
                            numbers.TeamNumbers[n],
                            team.Name,
                            members[team.Id].Select(m => m.Row).ToList())).ToList()),
                numbers.ProjectUsers is null
                    ? null
                    : new Chapter<IReadOnlyList<ProjectUserRow>>(numbers.ProjectUsers,
                        ProjectUsers(teams, members)),
                numbers.WorkItems is null
                    ? null
                    : new Chapter<WorkItemSummary>(numbers.WorkItems, summaryByProject[project.Id]),
                numbers.Repositories is null
                    ? null
                    : new Chapter<IReadOnlyList<RepositoryRow>>(numbers.Repositories,
                        (repositoriesByProject.GetValueOrDefault(project.Id) ?? [])
                            .OrderBy(r => r.Name, Comparer).ToList())));
        }

        return new ReportViewModel
        {
            Projects = outline.Projects is null
                ? null
                : new Chapter<IReadOnlyList<ProjectRow>>(outline.Projects,
                    projectRows.OrderBy(p => p.Name, Comparer).ToList()),
            // Sem `projects` e sem projetos, o relatório mostra só a mensagem de projetos (spec 015, RF-09).
            WorkItems = outline.WorkItems is null || (!sections.Projects && projects.Count == 0)
                ? null
                : new Chapter<IReadOnlyList<CountRow>>(outline.WorkItems,
                    orderedProjects
                        .Select(p => new CountRow(p.Name, summaryByProject[p.Id].Open, summaryByProject[p.Id].Closed))
                        .ToList()),
            ProjectChapters = chapters,
            NoProjects = !sections.Projects && projects.Count == 0
        };
    }

    private async Task<RepositoryRow> ToRepositoryRowAsync(string projectId, GitRepository repository)
    {
        var isEmpty = string.IsNullOrEmpty(repository.DefaultBranch);
        if (isEmpty || repository.IsDisabled)
            return new RepositoryRow(repository.Name, null, null, null, isEmpty, repository.IsDisabled);

        var latest = await _azureDevOps.GetLatestCommitAsync(projectId, repository.Id);
        var first = await _azureDevOps.GetFirstCommitAsync(projectId, repository.Id);
        return new RepositoryRow(
            repository.Name,
            first?.Author?.Name,
            latest?.Committer?.Date,
            latest?.Author?.Name,
            false,
            false);
    }

    /// <summary>Membro com a linha do relatório e a chave da identidade (para consolidar o projeto).</summary>
    private sealed record ClassifiedMember(string Key, UserRow Row);

    private ClassifiedMember ToUserRow(TeamMember member)
    {
        var email = member.Identity?.UniqueName;
        var isGroup = MemberClassifier.IsGroup(email);
        var row = new UserRow(
            member.Identity?.DisplayName ?? string.Empty,
            isGroup ? null : email,
            member.IsTeamAdmin,
            isGroup,
            !isGroup && MemberClassifier.IsExternal(email, _internalDomains));

        // Sem id, a identidade é reconhecida pelo uniqueName.
        return new ClassifiedMember(member.Identity?.Id ?? email ?? row.Name, row);
    }

    /// <param name="members"><c>null</c> = membros não buscados; as contagens ficam de fora.</param>
    private static TeamRow ToTeamRow(Team team, List<ClassifiedMember>? members)
    {
        if (members is null)
            return new TeamRow(team.Name, team.Description, null, null, false);

        var people = members.Where(m => !m.Row.IsGroup).ToList();
        var admins = people.Count(m => m.Row.IsAdmin);
        return new TeamRow(team.Name, team.Description, people.Count, admins, members.Count > 0 && admins == 0);
    }

    /// <summary>Usuários do projeto sem repetição e sem grupos; os times seguem a ordem recebida (por nome).</summary>
    private static List<ProjectUserRow> ProjectUsers(List<Team> teams, Dictionary<string, List<ClassifiedMember>> members)
    {
        var users = new Dictionary<string, (UserRow Row, List<string> Teams, List<string> AdminOf)>();
        foreach (var team in teams)
        {
            foreach (var member in members[team.Id].Where(m => !m.Row.IsGroup))
            {
                if (!users.TryGetValue(member.Key, out var user))
                    users[member.Key] = user = (member.Row, [], []);

                user.Teams.Add(team.Name);
                if (member.Row.IsAdmin)
                    user.AdminOf.Add(team.Name);
            }
        }

        return users.Values
            .Select(u => new ProjectUserRow(u.Row.Name, u.Row.Email, u.Row.IsExternal, u.Teams, u.AdminOf))
            .OrderBy(u => u.Name, Comparer)
            .ToList();
    }
}

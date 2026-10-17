using System.Globalization;
using Razor.Templating.Core;
using Reports.Api.Dtos.Requests;
using Reports.Api.Models;
using Reports.Api.Services.Pdf;
using Reports.Api.Views.Models;

namespace Reports.Api.Services;

public class ReportService
{
    private static readonly StringComparer Comparer =
        StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    private readonly AzureDevOpsService _azureDevOps;
    private readonly IPdfRenderer _pdfRenderer;

    public ReportService(AzureDevOpsService azureDevOps, IPdfRenderer pdfRenderer)
    {
        _azureDevOps = azureDevOps;
        _pdfRenderer = pdfRenderer;
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

        var orderedProjects = projects.OrderBy(p => p.Name, Comparer).ToList();
        var outline = ReportOutline.Build(sections,
            orderedProjects.Select(p => teamsByProject.GetValueOrDefault(p.Id)?.Count ?? 0).ToList());

        var chapters = new List<ProjectChapter>();
        for (var i = 0; i < outline.ProjectChapters.Count; i++)
        {
            var project = orderedProjects[i];
            var numbers = outline.ProjectChapters[i];
            var teams = teamsByProject[project.Id].OrderBy(t => t.Name, Comparer).ToList();

            chapters.Add(new ProjectChapter(
                numbers.Chapter,
                project.Name,
                numbers.Teams is null
                    ? null
                    : new Chapter<IReadOnlyList<TeamRow>>(numbers.Teams,
                        teams.Select(t => new TeamRow(t.Name, t.Description)).ToList()),
                numbers.UsersByTeam is null
                    ? null
                    : new Chapter<IReadOnlyList<TeamUsers>>(numbers.UsersByTeam,
                        teams.Select((team, n) => new TeamUsers(
                            numbers.TeamNumbers[n],
                            team.Name,
                            membersByTeam[team.Id]
                                .Select(m => new UserRow(
                                    m.Identity?.DisplayName ?? string.Empty,
                                    m.Identity?.UniqueName,
                                    m.IsTeamAdmin))
                                .OrderBy(u => u.Name, Comparer)
                                .ToList())).ToList())));
        }

        return new ReportViewModel
        {
            Projects = outline.Projects is null
                ? null
                : new Chapter<IReadOnlyList<ProjectRow>>(outline.Projects,
                    projectRows.OrderBy(p => p.Name, Comparer).ToList()),
            ProjectChapters = chapters,
            NoProjects = !sections.Projects && projects.Count == 0
        };
    }
}

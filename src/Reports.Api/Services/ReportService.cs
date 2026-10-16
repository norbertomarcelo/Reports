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

        var teams = new List<(Project Project, Team Team)>();
        if (sections.Teams || sections.Users)
        {
            foreach (var project in projects)
            {
                var projectTeams = await _azureDevOps.GetTeamsPerProjectAsync(project.Id);
                teams.AddRange(projectTeams.Select(team => (project, team)));
            }
        }

        var users = new List<UserRow>();
        if (sections.Users)
        {
            foreach (var (project, team) in teams)
            {
                var members = await _azureDevOps.GetTeamMembersAsync(project.Id, team.Id);
                users.AddRange(members.Select(member => new UserRow(
                    project.Name,
                    team.Name,
                    member.Identity?.DisplayName ?? string.Empty,
                    member.Identity?.UniqueName,
                    member.IsTeamAdmin)));
            }
        }

        return new ReportViewModel
        {
            Projects = sections.Projects
                ? projects.OrderBy(p => p.Name, Comparer).ToList()
                : null,
            Teams = sections.Teams
                ? teams.Select(t => new TeamRow(t.Project.Name, t.Team.Name, t.Team.Description))
                    .OrderBy(t => t.Project, Comparer).ThenBy(t => t.Team, Comparer).ToList()
                : null,
            Users = sections.Users
                ? users.OrderBy(u => u.Project, Comparer).ThenBy(u => u.Team, Comparer)
                    .ThenBy(u => u.Name, Comparer).ToList()
                : null
        };
    }
}

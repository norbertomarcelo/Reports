using Reports.AzureDevOps.Dtos.Responses;
using Reports.AzureDevOps.Interfaces;
using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Tests.Fakes;

public class FakeAzureDevOpsAnalyticsApi : IAzureDevOpsAnalyticsApi
{
    /// <summary>Grupos devolvidos por nome de projeto; projeto ausente = nenhum grupo.</summary>
    public Dictionary<string, List<WorkItemGroup>> GroupsByProject { get; } = [];

    /// <summary>Projetos cuja resposta traz <c>@odata.nextLink</c> (mais de uma página).</summary>
    public HashSet<string> ProjectsWithNextPage { get; } = [];

    public Exception? Error { get; set; }

    public List<string> Calls { get; } = [];

    public Task<WorkItemGroupsResponse> GetWorkItemGroupsAsync(string organization, string project, string apply)
    {
        Calls.Add($"analytics {organization} {project} {apply}");
        if (Error is not null)
            return Task.FromException<WorkItemGroupsResponse>(Error);

        return Task.FromResult(new WorkItemGroupsResponse
        {
            Value = GroupsByProject.GetValueOrDefault(project) ?? [],
            NextLink = ProjectsWithNextPage.Contains(project) ? "https://analytics.example/proxima-pagina" : null
        });
    }
}

using Refit;
using Reports.AzureDevOps.Dtos.Responses;

namespace Reports.AzureDevOps.Interfaces;

/// <summary>Analytics (OData) do Azure DevOps: agrega no servidor, sem trazer os itens individuais.</summary>
public interface IAzureDevOpsAnalyticsApi
{
    [Get("/{organization}/{project}/_odata/v4.0-preview/WorkItems")]
    Task<WorkItemGroupsResponse> GetWorkItemGroupsAsync(
        string organization,
        string project,
        [AliasAs("$apply")] string apply);
}

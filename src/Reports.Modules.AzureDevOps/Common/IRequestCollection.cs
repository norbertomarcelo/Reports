using Refit;
using Reports.Modules.AzureDevOps.Entities;

namespace Reports.Modules.AzureDevOps.Common;

public interface IRequestCollection
{
    [Get("/{organization}/_apis/projects?api-version=5.0")]
    Task<SuccessfulResponse> GetProjects(string organization, [Header("Authorization")] string authorization);
}
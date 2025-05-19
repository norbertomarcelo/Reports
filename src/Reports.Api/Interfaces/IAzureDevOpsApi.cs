using Refit;
using Reports.Api.Dtos;

namespace Reports.Api.Interfaces;

public interface IAzureDevOpsApi
{
    [Get("/MNS-Tech/_apis/projects?api-version=5.0")]
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
}

using Refit;
using Reports.Api.Dtos.Responses;

namespace Reports.Api.Interfaces;

public interface IAzureDevOpsApi
{
    [Get("/{organization}/_apis/projects")]
    Task<ProjectsResponse> GetProjectsAsync(
        string organization,
        [AliasAs("api-version")] string apiVersion);

    [Get("/{organization}/_apis/projects/{projectId}/properties")]
    Task<ProjectPropertiesResponse> GetProjectPropertiesAsync(
        string organization,
        string projectId,
        [AliasAs("api-version")] string apiVersion);

    [Get("/{organization}/_apis/projects/{projectId}/teams")]
    Task<TeamsResponse> GetTeamsAsync(
        string organization,
        string projectId,
        [AliasAs("api-version")] string apiVersion);

    [Get("/{organization}/_apis/projects/{projectId}/teams/{teamId}/members")]
    Task<TeamMembersResponse> GetTeamMembersAsync(
        string organization,
        string projectId,
        string teamId,
        [AliasAs("api-version")] string apiVersion);

    [Get("/{organization}/_apis/git/repositories")]
    Task<RepositoriesResponse> GetRepositoriesAsync(
        string organization,
        [AliasAs("api-version")] string apiVersion);

    [Get("/{organization}/{projectId}/_apis/git/repositories/{repositoryId}/commits")]
    Task<CommitsResponse> GetCommitsAsync(
        string organization,
        string projectId,
        string repositoryId,
        [AliasAs("searchCriteria.$top")] int top,
        [AliasAs("searchCriteria.showOldestCommitsFirst")] bool? oldestFirst,
        [AliasAs("api-version")] string apiVersion);
}

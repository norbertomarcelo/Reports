using Reports.Api.Dtos.Responses;
using Reports.Api.Interfaces;
using Reports.Api.Models;

namespace Reports.Api.Tests.Fakes;

public class FakeAzureDevOpsApi : IAzureDevOpsApi
{
    public List<Project>? Projects { get; set; } = [];
    public List<Team>? Teams { get; set; } = [];
    public List<TeamMember>? Members { get; set; } = [];
    public Exception? Error { get; set; }

    public List<string> Calls { get; } = [];

    public Task<ProjectsResponse> GetProjectsAsync(string organization, string apiVersion)
    {
        Calls.Add($"projects {organization} {apiVersion}");
        return Respond(new ProjectsResponse { Count = Projects?.Count ?? 0, Value = Projects! });
    }

    public Task<TeamsResponse> GetTeamsAsync(string organization, string projectId, string apiVersion)
    {
        Calls.Add($"teams {organization} {projectId} {apiVersion}");
        return Respond(new TeamsResponse { Count = Teams?.Count ?? 0, Value = Teams! });
    }

    public Task<TeamMembersResponse> GetTeamMembersAsync(string organization, string projectId, string teamId, string apiVersion)
    {
        Calls.Add($"members {organization} {projectId} {teamId} {apiVersion}");
        return Respond(new TeamMembersResponse { Count = Members?.Count ?? 0, Value = Members! });
    }

    private Task<T> Respond<T>(T value) => Error is null ? Task.FromResult(value) : Task.FromException<T>(Error);
}

using Reports.AzureDevOps.Dtos.Responses;
using Reports.AzureDevOps.Interfaces;
using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Tests.Fakes;

public class FakeAzureDevOpsApi : IAzureDevOpsApi
{
    public List<Project>? Projects { get; set; } = [];
    public List<Team>? Teams { get; set; } = [];
    public List<TeamMember>? Members { get; set; } = [];
    public Dictionary<string, List<Team>> TeamsByProject { get; } = [];
    public Dictionary<string, List<TeamMember>> MembersByTeam { get; } = [];
    public Dictionary<string, List<ProjectProperty>> PropertiesByProject { get; } = [];
    public List<ProjectProperty>? Properties { get; set; } = [];
    public Exception? Error { get; set; }
    public Exception? PropertiesError { get; set; }
    public List<GitRepository> Repositories { get; set; } = [];

    /// <summary>Commits por id de repositório, do mais novo para o mais antigo.</summary>
    public Dictionary<string, List<GitCommit>> CommitsByRepository { get; } = [];

    public Exception? RepositoriesError { get; set; }
    public Exception? CommitsError { get; set; }

    public List<string> Calls { get; } = [];

    public Task<ProjectsResponse> GetProjectsAsync(string organization, string apiVersion)
    {
        Calls.Add($"projects {organization} {apiVersion}");
        return Respond(new ProjectsResponse { Count = Projects?.Count ?? 0, Value = Projects! });
    }

    public Task<ProjectPropertiesResponse> GetProjectPropertiesAsync(string organization, string projectId, string apiVersion)
    {
        Calls.Add($"properties {organization} {projectId} {apiVersion}");
        var properties = PropertiesByProject.TryGetValue(projectId, out var byProject) ? byProject : Properties;
        if (PropertiesError is not null)
            return Task.FromException<ProjectPropertiesResponse>(PropertiesError);
        return Respond(new ProjectPropertiesResponse { Count = properties?.Count ?? 0, Value = properties! });
    }

    public Task<TeamsResponse> GetTeamsAsync(string organization, string projectId, string apiVersion)
    {
        Calls.Add($"teams {organization} {projectId} {apiVersion}");
        var teams = TeamsByProject.TryGetValue(projectId, out var byProject) ? byProject : Teams;
        return Respond(new TeamsResponse { Count = teams?.Count ?? 0, Value = teams! });
    }

    public Task<TeamMembersResponse> GetTeamMembersAsync(string organization, string projectId, string teamId, string apiVersion)
    {
        Calls.Add($"members {organization} {projectId} {teamId} {apiVersion}");
        var members = MembersByTeam.TryGetValue(teamId, out var byTeam) ? byTeam : Members;
        return Respond(new TeamMembersResponse { Count = members?.Count ?? 0, Value = members! });
    }

    public Task<RepositoriesResponse> GetRepositoriesAsync(string organization, string apiVersion)
    {
        Calls.Add($"repositories {organization} {apiVersion}");
        if (RepositoriesError is not null)
            return Task.FromException<RepositoriesResponse>(RepositoriesError);
        return Respond(new RepositoriesResponse { Count = Repositories.Count, Value = Repositories });
    }

    public Task<CommitsResponse> GetCommitsAsync(
        string organization, string projectId, string repositoryId, int top, bool? oldestFirst, string apiVersion)
    {
        Calls.Add($"commits {organization} {projectId} {repositoryId} {top} {oldestFirst?.ToString().ToLowerInvariant() ?? "-"} {apiVersion}");
        if (CommitsError is not null)
            return Task.FromException<CommitsResponse>(CommitsError);

        var all = CommitsByRepository.GetValueOrDefault(repositoryId) ?? [];
        var commits = oldestFirst == true ? all.AsEnumerable().Reverse() : all;
        var chosen = commits.Take(top).ToList();
        return Respond(new CommitsResponse { Count = chosen.Count, Value = chosen });
    }

    private Task<T> Respond<T>(T value) => Error is null ? Task.FromResult(value) : Task.FromException<T>(Error);
}

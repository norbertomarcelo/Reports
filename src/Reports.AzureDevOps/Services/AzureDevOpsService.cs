using Microsoft.Extensions.Options;
using Refit;
using Reports.AzureDevOps.Interfaces;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Settings;

namespace Reports.AzureDevOps.Services;

public class AzureDevOpsService
{
    /// <summary>
    /// Conta no servidor, sem os removidos, agrupando por tipo, categoria de estado e área (spec 017, RF-08).
    /// </summary>
    public const string WorkItemGroupsQuery =
        "filter(StateCategory ne 'Removed')/groupby((WorkItemType,StateCategory,Area/AreaPath),aggregate($count as Count))";

    private readonly IAzureDevOpsApi _api;
    private readonly IAzureDevOpsAnalyticsApi _analytics;
    private readonly AzureDevOpsOptions _options;

    public AzureDevOpsService(
        IAzureDevOpsApi api, IAzureDevOpsAnalyticsApi analytics, IOptions<AzureDevOpsOptions> options)
    {
        _api = api;
        _analytics = analytics;
        _options = options.Value;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        var response = await CallAsync("projetos",
            () => _api.GetProjectsAsync(_options.Organization, _options.ApiVersion));
        return response?.Value ?? new List<Project>();
    }

    public async Task<IEnumerable<ProjectProperty>> GetProjectPropertiesAsync(string projectId)
    {
        var response = await CallAsync("propriedades do projeto",
            () => _api.GetProjectPropertiesAsync(_options.Organization, projectId, _options.ProjectPropertiesApiVersion));
        return response?.Value ?? new List<ProjectProperty>();
    }

    public async Task<IEnumerable<Team>> GetTeamsPerProjectAsync(string projectId)
    {
        var response = await CallAsync("equipes",
            () => _api.GetTeamsAsync(_options.Organization, projectId, _options.ApiVersion));
        return response?.Value ?? new List<Team>();
    }

    public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(string projectId, string teamId)
    {
        var response = await CallAsync("membros da equipe",
            () => _api.GetTeamMembersAsync(_options.Organization, projectId, teamId, _options.ApiVersion));
        return response?.Value ?? new List<TeamMember>();
    }

    /// <summary>Uma única chamada ao Analytics por projeto; o Analytics identifica o projeto pelo nome.</summary>
    public async Task<WorkItemGroups> GetWorkItemGroupsAsync(string projectName)
    {
        var response = await CallAsync("contagem de itens de trabalho",
            () => _analytics.GetWorkItemGroupsAsync(_options.Organization, projectName, WorkItemGroupsQuery));
        return new WorkItemGroups(response?.Value ?? [], response?.NextLink is not null);
    }

    public async Task<IEnumerable<GitRepository>> GetAllRepositoriesAsync()
    {
        var response = await CallAsync("repositórios",
            () => _api.GetRepositoriesAsync(_options.Organization, _options.ApiVersion));
        return response?.Value ?? new List<GitRepository>();
    }

    /// <summary>Commit mais recente do branch padrão; <c>null</c> se não houver commits (spec 018).</summary>
    public Task<GitCommit?> GetLatestCommitAsync(string projectId, string repositoryId) =>
        GetSingleCommitAsync(projectId, repositoryId, null);

    /// <summary>Commit mais antigo do branch padrão; <c>null</c> se não houver commits (spec 018).</summary>
    public Task<GitCommit?> GetFirstCommitAsync(string projectId, string repositoryId) =>
        GetSingleCommitAsync(projectId, repositoryId, true);

    private async Task<GitCommit?> GetSingleCommitAsync(string projectId, string repositoryId, bool? oldestFirst)
    {
        var response = await CallAsync("commits do repositório",
            () => _api.GetCommitsAsync(_options.Organization, projectId, repositoryId, 1, oldestFirst, _options.ApiVersion));
        return response?.Value?.FirstOrDefault();
    }

    private static async Task<T?> CallAsync<T>(string resource, Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (ApiException ex)
        {
            throw new AzureDevOpsException(
                $"Erro ao obter {resource} do Azure DevOps: {(int)ex.StatusCode} - {ex.ReasonPhrase}",
                ex.StatusCode,
                ex);
        }
    }
}

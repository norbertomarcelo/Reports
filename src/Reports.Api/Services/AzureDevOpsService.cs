using Microsoft.Extensions.Options;
using Refit;
using Reports.Api.Interfaces;
using Reports.Api.Models;
using Reports.Api.Settings;

namespace Reports.Api.Services;

public class AzureDevOpsService
{
    private readonly IAzureDevOpsApi _api;
    private readonly AzureDevOpsOptions _options;

    public AzureDevOpsService(IAzureDevOpsApi api, IOptions<AzureDevOpsOptions> options)
    {
        _api = api;
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

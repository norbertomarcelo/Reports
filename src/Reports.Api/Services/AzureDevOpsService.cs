using Newtonsoft.Json;
using Razor.Templating.Core;
using Reports.Api.Dtos.Responses;
using Reports.Api.Models;
using System.Net.Http.Headers;
using System.Text;

namespace Reports.Api.Services;

public class AzureDevOpsService
{
    private readonly IConfiguration _configuration;
    private readonly string _coreServer;
    private readonly string _organization;
    private readonly string _project;
    private readonly string _apiVersion;
    private readonly string _accessToken;

    public AzureDevOpsService(IConfiguration configuration)
    {
        _configuration = configuration;
        _coreServer = _configuration.GetValue<string>("AzureDevOps:CoreSerever");
        _organization = _configuration.GetValue<string>("AzureDevOps:Organization");
        _project = _configuration.GetValue<string>("AzureDevOps:Project");
        _apiVersion = _configuration.GetValue<string>("AzureDevOps:ApiVersion");
        _accessToken = _configuration.GetValue<string>("AzureDevOps:AccessToken");

    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        var apiUrl = $"{_coreServer}/{_organization}/_apis/projects?api-version={_apiVersion}";

        using (HttpClient client = new HttpClient())
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_accessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            try
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var projectsResponse = JsonConvert.DeserializeObject<ProjectsResponse>(result);
                    return projectsResponse?.Value ?? new List<Project>();
                }
                else
                {
                    throw new Exception($"Erro ao obter projetos: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar projetos do Azure DevOps.", ex);
            }
        }
    }

    public async Task<IEnumerable<Team>> GetTeamsPerProjectAsync(string projectId)
    {
        var apiUrl = $"{_coreServer}/{_organization}/_apis/projects/{projectId}/teams?$mine=false&$top=false&$skip=&api-version={_apiVersion}";

        using (HttpClient client = new HttpClient())
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_accessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            try
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var teamsResponse = JsonConvert.DeserializeObject<TeamsResponse>(result);
                    return teamsResponse?.Value ?? new List<Team>();
                }
                else
                {
                    throw new Exception($"Erro ao obter projetos: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar projetos do Azure DevOps.", ex);
            }
        }
    }

    public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(string projectId, string teamId)
    {
        var apiUrl = $"{_coreServer}/{_organization}/_apis/projects/{projectId}/teams/{teamId}/members?$mine=false&$top=10&$skip&api-version={_apiVersion}";

        using (HttpClient client = new HttpClient())
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_accessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            try
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var teamMembersResponse = JsonConvert.DeserializeObject<TeamMembersResponse>(result);
                    return teamMembersResponse?.Value ?? new List<TeamMember>();
                }
                else
                {
                    throw new Exception($"Erro ao obter projetos: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar projetos do Azure DevOps.", ex);
            }
        }
    }

    public async Task<byte[]> GenerateIndexAsync()
    {
        var projectList = await this.GetAllProjectsAsync();
        var html = await RazorTemplateEngine.RenderAsync("Views/Index.cshtml", projectList);
        var render = new ChromePdfRenderer();
        using var pdfDocument = render.RenderHtmlAsPdf(html);
        return pdfDocument.BinaryData;
    }
}

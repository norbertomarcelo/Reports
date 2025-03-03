using Microsoft.AspNetCore.Mvc;

namespace Reports.Api.Controllers;

[Route("[controller]")]
public class AzureDevOpsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _coreServer;
    private readonly string _organization;
    private readonly string _project;
    private readonly string _apiVersion;
    private readonly string _accessToken;
    private readonly IHttpClientFactory _httpClientFactory;


    public AzureDevOpsController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _coreServer = _configuration.GetValue<string>("AzureDevOps:CoreSerever");
        _organization = _configuration.GetValue<string>("AzureDevOps:Organization");
        _project = _configuration.GetValue<string>("AzureDevOps:Project");
        _apiVersion = _configuration.GetValue<string>("AzureDevOps:ApiVersion");
        _accessToken = _configuration.GetValue<string>("AzureDevOps:AccessToken");
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("/project")]
    public async Task<IActionResult> ListAllProjectsAsync()
    {
        var endpoint = $"/{_organization}/_apis/projects?api-version={_apiVersion}";

        var client = _httpClientFactory.CreateClient("AzureDevOps");
        var response = await client.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "Error fetching projects");
        }

        var content = await response.Content.ReadAsStringAsync();
        return Ok(content);
    }
}

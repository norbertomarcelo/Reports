using Refit;
using Reports.Modules.AzureDevOps.Common;
using Reports.Modules.AzureDevOps.Entities;
using DotNetEnv;

namespace Reports.Modules.AzureDevOps.Services;

public class ProjectService
{
    public string PersonalAccessToken { get; set; }
    public string Organization { get; set; }
    public string Url { get; set; }

    private readonly IRequestCollection _requestCollection;

    public ProjectService(string personalAccessToken, string organization, string url)
    {
        PersonalAccessToken = personalAccessToken;
        Organization = organization;
        Url = url;

        _requestCollection = RestService
            .For<IRequestCollection>(Url);
    }

    public async Task<SuccessfulResponse> GetProjectsList()
    {
        var token =
            $"Basic {Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($":{PersonalAccessToken}"))}";
        var response = await _requestCollection.GetProjects(Organization, token);
        return response;
    }
}
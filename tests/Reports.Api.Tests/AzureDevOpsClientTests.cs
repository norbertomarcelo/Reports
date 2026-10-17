using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Reports.Api.Interfaces;
using Reports.Api.Settings;

namespace Reports.Api.Tests;

public class AzureDevOpsClientTests
{
    [Fact]
    public async Task Envia_requisicao_com_url_versao_e_autenticacao_basic()
    {
        var handler = new CapturingHandler(
            """{"count":1,"value":[{"isTeamAdmin":true,"identity":{"displayName":"Ana"}}]}""");

        var services = new ServiceCollection();
        services.AddOptions<AzureDevOpsOptions>().Configure(o =>
        {
            o.CoreServer = ApiFactory.CoreServer;
            o.Organization = ApiFactory.Organization;
            o.ApiVersion = ApiFactory.ApiVersion;
            o.AccessToken = ApiFactory.AccessToken;
        });
        services.AddAzureDevOpsClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var api = services.BuildServiceProvider().GetRequiredService<IAzureDevOpsApi>();
        var response = await api.GetTeamMembersAsync(ApiFactory.Organization, "p1", "t1", ApiFactory.ApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(
            $"{ApiFactory.CoreServer}/{ApiFactory.Organization}/_apis/projects/p1/teams/t1/members?api-version={ApiFactory.ApiVersion}",
            request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{ApiFactory.AccessToken}")),
            request.Headers.Authorization.Parameter);

        var member = Assert.Single(response.Value);
        Assert.True(member.IsTeamAdmin);
        Assert.Equal("Ana", member.Identity.DisplayName);
    }

    [Fact]
    public async Task Busca_propriedades_do_projeto_com_url_versao_preview_e_valores_de_varios_tipos()
    {
        var handler = new CapturingHandler("""
            {"count":8,"value":[
              {"name":"System.CurrentProcessTemplateId","value":"2dc3221a-2d39-4138-a4e1-fc4d20d8912d"},
              {"name":"System.OriginalProcessTemplateId","value":"2dc3221a-2d39-4138-a4e1-fc4d20d8912d"},
              {"name":"System.ProcessTemplateType","value":"adcc42ab-9882-485e-a3ed-7678f01f66bc"},
              {"name":"System.Process Template","value":"Agile"},
              {"name":"System.Microsoft.TeamFoundation.Team.Default","value":"9b7ae5b9-826f-4353-99d6-daaa5cd94ec6"},
              {"name":"System.SourceControlCapabilityFlags","value":2},
              {"name":"System.SourceControlGitEnabled","value":true},
              {"name":"System.SourceControlGitPermissionsInitialized","value":"True"}
            ]}
            """);

        var services = new ServiceCollection();
        services.AddOptions<AzureDevOpsOptions>().Configure(o =>
        {
            o.CoreServer = ApiFactory.CoreServer;
            o.Organization = ApiFactory.Organization;
            o.ApiVersion = ApiFactory.ApiVersion;
            o.AccessToken = ApiFactory.AccessToken;
        });
        services.AddAzureDevOpsClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var api = services.BuildServiceProvider().GetRequiredService<IAzureDevOpsApi>();
        var response = await api.GetProjectPropertiesAsync(
            ApiFactory.Organization, "p1", ApiFactory.ProjectPropertiesApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(
            $"{ApiFactory.CoreServer}/{ApiFactory.Organization}/_apis/projects/p1/properties?api-version={ApiFactory.ProjectPropertiesApiVersion}",
            request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);

        Assert.Equal(8, response.Value.Count);
        Assert.Equal("Agile", response.Value[3].Value.GetString());
        Assert.Equal(2, response.Value[5].Value.GetInt32());
        Assert.True(response.Value[6].Value.GetBoolean());
    }

    private class CapturingHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}

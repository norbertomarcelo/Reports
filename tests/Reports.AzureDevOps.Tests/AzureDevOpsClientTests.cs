using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Reports.AzureDevOps.Interfaces;
using Reports.AzureDevOps.Services;
using Reports.AzureDevOps.Settings;

namespace Reports.AzureDevOps.Tests;

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
            o.CoreServer = TestSettings.CoreServer;
            o.Organization = TestSettings.Organization;
            o.ApiVersion = TestSettings.ApiVersion;
            o.AccessToken = TestSettings.AccessToken;
        });
        services.AddAzureDevOpsClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var api = services.BuildServiceProvider().GetRequiredService<IAzureDevOpsApi>();
        var response = await api.GetTeamMembersAsync(TestSettings.Organization, "p1", "t1", TestSettings.ApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(
            $"{TestSettings.CoreServer}/{TestSettings.Organization}/_apis/projects/p1/teams/t1/members?api-version={TestSettings.ApiVersion}",
            request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{TestSettings.AccessToken}")),
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
            o.CoreServer = TestSettings.CoreServer;
            o.Organization = TestSettings.Organization;
            o.ApiVersion = TestSettings.ApiVersion;
            o.AccessToken = TestSettings.AccessToken;
        });
        services.AddAzureDevOpsClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var api = services.BuildServiceProvider().GetRequiredService<IAzureDevOpsApi>();
        var response = await api.GetProjectPropertiesAsync(
            TestSettings.Organization, "p1", TestSettings.ProjectPropertiesApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(
            $"{TestSettings.CoreServer}/{TestSettings.Organization}/_apis/projects/p1/properties?api-version={TestSettings.ProjectPropertiesApiVersion}",
            request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);

        Assert.Equal(8, response.Value.Count);
        Assert.Equal("Agile", response.Value[3].Value.GetString());
        Assert.Equal(2, response.Value[5].Value.GetInt32());
        Assert.True(response.Value[6].Value.GetBoolean());
    }

    [Fact]
    public async Task Conta_itens_no_analytics_com_host_rota_apply_e_autenticacao_basic()
    {
        var handler = new CapturingHandler("""
            {"@odata.context":"https://analytics.example/$metadata","value":[
              {"WorkItemType":"Bug","StateCategory":"InProgress","Area":{"AreaPath":"Alfa\\Backend"},"Count":2},
              {"WorkItemType":"Task","StateCategory":"Proposed","Area":null,"Count":5}
            ],"@odata.nextLink":"https://analytics.example/proxima"}
            """);

        var services = new ServiceCollection();
        services.AddOptions<AzureDevOpsOptions>().Configure(o =>
        {
            o.CoreServer = TestSettings.CoreServer;
            o.AnalyticsServer = TestSettings.AnalyticsServer;
            o.Organization = TestSettings.Organization;
            o.AccessToken = TestSettings.AccessToken;
        });
        services.AddAzureDevOpsClient();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));

        var api = services.BuildServiceProvider().GetRequiredService<IAzureDevOpsAnalyticsApi>();
        var response = await api.GetWorkItemGroupsAsync(
            TestSettings.Organization, "Projeto Alfa", AzureDevOpsService.WorkItemGroupsQuery);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(TestSettings.AnalyticsServer, $"{request.RequestUri!.Scheme}://{request.RequestUri.Host}");
        Assert.Equal(
            $"/{TestSettings.Organization}/Projeto%20Alfa/_odata/v4.0-preview/WorkItems",
            request.RequestUri.AbsolutePath);
        Assert.Equal(AzureDevOpsService.WorkItemGroupsQuery,
            System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query)["$apply"]);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{TestSettings.AccessToken}")),
            request.Headers.Authorization.Parameter);

        Assert.Equal(2, response.Value.Count);
        Assert.Equal(("Bug", "InProgress", "Alfa\\Backend", 2), (response.Value[0].WorkItemType,
            response.Value[0].StateCategory, response.Value[0].Area!.AreaPath, response.Value[0].Count));
        Assert.Null(response.Value[1].Area);
        Assert.Equal("https://analytics.example/proxima", response.NextLink);
    }

    [Fact]
    public async Task Lista_repositorios_com_rota_versao_e_autenticacao_basic()
    {
        var handler = new CapturingHandler("""
            {"count":2,"value":[
              {"id":"r1","name":"api","defaultBranch":"refs/heads/main","isDisabled":false,"project":{"id":"p1","name":"Alfa"}},
              {"id":"r2","name":"velho","isDisabled":true,"project":{"id":"p1","name":"Alfa"}}
            ]}
            """);
        var api = BuildApi(handler);

        var response = await api.GetRepositoriesAsync(TestSettings.Organization, TestSettings.ApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(TestSettings.CoreServer, $"{request.RequestUri!.Scheme}://{request.RequestUri.Host}");
        Assert.Equal($"/{TestSettings.Organization}/_apis/git/repositories", request.RequestUri.AbsolutePath);
        Assert.Equal(TestSettings.ApiVersion,
            System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query)["api-version"]);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);

        Assert.Equal(2, response.Value.Count);
        Assert.Equal(("api", "refs/heads/main", false, "p1"),
            (response.Value[0].Name, response.Value[0].DefaultBranch, response.Value[0].IsDisabled, response.Value[0].Project!.Id));
        Assert.Null(response.Value[1].DefaultBranch);
        Assert.True(response.Value[1].IsDisabled);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(true, "true")]
    public async Task Busca_commits_com_top_1_e_a_ordem_inversa_so_quando_pedida(bool? oldestFirst, string? esperado)
    {
        var handler = new CapturingHandler("""
            {"count":1,"value":[{"commitId":"abc",
              "author":{"name":"Ana","email":"ana@example.com","date":"2026-01-02T10:00:00Z"},
              "committer":{"name":"Bruno","email":"bruno@example.com","date":"2026-01-02T11:30:00Z"}}]}
            """);
        var api = BuildApi(handler);

        var response = await api.GetCommitsAsync(
            TestSettings.Organization, "p1", "r1", 1, oldestFirst, TestSettings.ApiVersion);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal($"/{TestSettings.Organization}/p1/_apis/git/repositories/r1/commits", request.RequestUri!.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        Assert.Equal("1", query["searchCriteria.$top"]);
        Assert.Equal(esperado, query["searchCriteria.showOldestCommitsFirst"]);
        Assert.Equal(TestSettings.ApiVersion, query["api-version"]);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);

        var commit = Assert.Single(response.Value);
        Assert.Equal(("Ana", "Bruno"), (commit.Author!.Name, commit.Committer!.Name));
        Assert.Equal(new DateTime(2026, 1, 2, 11, 30, 0, DateTimeKind.Utc), commit.Committer.Date.ToUniversalTime());
    }

    private static IAzureDevOpsApi BuildApi(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddOptions<AzureDevOpsOptions>().Configure(o =>
        {
            o.CoreServer = TestSettings.CoreServer;
            o.AnalyticsServer = TestSettings.AnalyticsServer;
            o.Organization = TestSettings.Organization;
            o.AccessToken = TestSettings.AccessToken;
        });
        services.AddAzureDevOpsClient();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler));
        return services.BuildServiceProvider().GetRequiredService<IAzureDevOpsApi>();
    }

    [Fact]
    public void A_consulta_conta_no_servidor_sem_os_removidos_agrupando_por_tipo_categoria_e_area()
    {
        var query = AzureDevOpsService.WorkItemGroupsQuery;

        Assert.Contains("StateCategory ne 'Removed'", query);
        Assert.Contains("groupby((WorkItemType,StateCategory,Area/AreaPath)", query);
        Assert.Contains("aggregate($count as Count)", query);
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

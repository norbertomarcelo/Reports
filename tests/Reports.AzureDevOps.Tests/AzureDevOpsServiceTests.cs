using System.Net;
using Microsoft.Extensions.Options;
using Refit;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Services;
using Reports.AzureDevOps.Settings;
using Reports.AzureDevOps.Tests.Fakes;

namespace Reports.AzureDevOps.Tests;

public class AzureDevOpsServiceTests
{
    private readonly FakeAzureDevOpsApi _api = new();
    private readonly FakeAzureDevOpsAnalyticsApi _analytics = new();
    private readonly AzureDevOpsService _service;

    public AzureDevOpsServiceTests()
    {
        var options = Options.Create(new AzureDevOpsOptions
        {
            CoreServer = TestSettings.CoreServer,
            Organization = TestSettings.Organization,
            ApiVersion = TestSettings.ApiVersion,
            ProjectPropertiesApiVersion = TestSettings.ProjectPropertiesApiVersion,
            AccessToken = TestSettings.AccessToken
        });
        _service = new AzureDevOpsService(_api, _analytics, options);
    }

    [Fact]
    public async Task Lista_projetos_da_organizacao_configurada()
    {
        _api.Projects = [new Project { Name = "Alfa" }];

        var result = await _service.GetAllProjectsAsync();

        Assert.Equal("Alfa", Assert.Single(result).Name);
        Assert.Equal($"projects {TestSettings.Organization} {TestSettings.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Lista_equipes_do_projeto()
    {
        _api.Teams = [new Team { Name = "Time A" }];

        var result = await _service.GetTeamsPerProjectAsync("p1");

        Assert.Equal("Time A", Assert.Single(result).Name);
        Assert.Equal($"teams {TestSettings.Organization} p1 {TestSettings.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Lista_membros_da_equipe()
    {
        _api.Members = [new TeamMember { IsTeamAdmin = true }];

        var result = await _service.GetTeamMembersAsync("p1", "t1");

        Assert.True(Assert.Single(result).IsTeamAdmin);
        Assert.Equal($"members {TestSettings.Organization} p1 t1 {TestSettings.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Lista_propriedades_do_projeto_com_a_versao_de_propriedades()
    {
        _api.Properties = [new ProjectProperty { Name = "System.Process Template" }];

        var result = await _service.GetProjectPropertiesAsync("p1");

        Assert.Equal("System.Process Template", Assert.Single(result).Name);
        Assert.Equal(
            $"properties {TestSettings.Organization} p1 {TestSettings.ProjectPropertiesApiVersion}",
            Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Propriedades_com_value_nulo_voltam_como_lista_vazia()
    {
        _api.Properties = null;

        Assert.Empty(await _service.GetProjectPropertiesAsync("p1"));
    }

    [Fact]
    public async Task Erro_http_nas_propriedades_vira_AzureDevOpsException_em_portugues()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.example/");
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
        _api.Error = await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());

        var ex = await Assert.ThrowsAsync<AzureDevOpsException>(() => _service.GetProjectPropertiesAsync("p1"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.StartsWith("Erro ao obter propriedades do projeto", ex.Message);
        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public async Task Conta_itens_de_trabalho_em_uma_unica_chamada_ao_analytics_pelo_nome_do_projeto()
    {
        _analytics.GroupsByProject["Projeto Alfa"] =
            [new WorkItemGroup { WorkItemType = "Bug", StateCategory = "Completed", Count = 3 }];

        var result = await _service.GetWorkItemGroupsAsync("Projeto Alfa");

        var group = Assert.Single(result.Groups);
        Assert.Equal(("Bug", "Completed", 3), (group.WorkItemType, group.StateCategory, group.Count));
        Assert.False(result.Partial);
        Assert.Equal(
            $"analytics {TestSettings.Organization} Projeto Alfa {AzureDevOpsService.WorkItemGroupsQuery}",
            Assert.Single(_analytics.Calls));
        Assert.Empty(_api.Calls);
    }

    [Fact]
    public async Task Contagem_de_itens_e_parcial_quando_o_analytics_indica_outra_pagina()
    {
        _analytics.ProjectsWithNextPage.Add("Alfa");

        Assert.True((await _service.GetWorkItemGroupsAsync("Alfa")).Partial);
        Assert.False((await _service.GetWorkItemGroupsAsync("Beta")).Partial);
    }

    [Fact]
    public async Task Erro_http_na_contagem_de_itens_vira_AzureDevOpsException_em_portugues()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://analytics.example/");
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request };
        _analytics.Error = await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());

        var ex = await Assert.ThrowsAsync<AzureDevOpsException>(() => _service.GetWorkItemGroupsAsync("Alfa"));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.StartsWith("Erro ao obter contagem de itens de trabalho", ex.Message);
        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task Lista_os_repositorios_da_organizacao_em_uma_chamada()
    {
        _api.Repositories = [new GitRepository { Id = "r1", Name = "api" }];

        var result = await _service.GetAllRepositoriesAsync();

        Assert.Equal("api", Assert.Single(result).Name);
        Assert.Equal($"repositories {TestSettings.Organization} {TestSettings.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Busca_o_commit_mais_recente_sem_pedir_a_ordem_inversa_e_o_mais_antigo_pedindo()
    {
        _api.CommitsByRepository["r1"] =
        [
            new GitCommit { CommitId = "novo" },
            new GitCommit { CommitId = "meio" },
            new GitCommit { CommitId = "antigo" }
        ];

        Assert.Equal("novo", (await _service.GetLatestCommitAsync("p1", "r1"))!.CommitId);
        Assert.Equal("antigo", (await _service.GetFirstCommitAsync("p1", "r1"))!.CommitId);
        Assert.Equal(
            [
                $"commits {TestSettings.Organization} p1 r1 1 - {TestSettings.ApiVersion}",
                $"commits {TestSettings.Organization} p1 r1 1 true {TestSettings.ApiVersion}"
            ],
            _api.Calls);
    }

    [Fact]
    public async Task Repositorio_sem_commits_devolve_null()
    {
        Assert.Null(await _service.GetLatestCommitAsync("p1", "vazio"));
        Assert.Null(await _service.GetFirstCommitAsync("p1", "vazio"));
    }

    [Theory]
    [InlineData(true, "Erro ao obter repositórios")]
    [InlineData(false, "Erro ao obter commits do repositório")]
    public async Task Erro_http_em_repositorios_ou_commits_vira_AzureDevOpsException_em_portugues(
        bool naListagem, string inicio)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.example/");
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request };
        var error = await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
        if (naListagem) _api.RepositoriesError = error; else _api.CommitsError = error;

        var ex = await Assert.ThrowsAsync<AzureDevOpsException>(() => naListagem
            ? _service.GetAllRepositoriesAsync()
            : _service.GetLatestCommitAsync("p1", "r1"));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.StartsWith(inicio, ex.Message);
        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task Retorna_lista_vazia_quando_value_e_nulo()
    {
        _api.Projects = null;

        var result = await _service.GetAllProjectsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Erro_http_vira_AzureDevOpsException_em_portugues()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dev.azure.example/");
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request };
        _api.Error = await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());

        var ex = await Assert.ThrowsAsync<AzureDevOpsException>(() => _service.GetAllProjectsAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Contains("Erro ao obter projetos", ex.Message);
        Assert.Contains("401", ex.Message);
    }
}

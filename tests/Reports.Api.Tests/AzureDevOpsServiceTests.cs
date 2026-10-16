using System.Net;
using Microsoft.Extensions.Options;
using Refit;
using Reports.Api.Models;
using Reports.Api.Services;
using Reports.Api.Settings;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class AzureDevOpsServiceTests
{
    private readonly FakeAzureDevOpsApi _api = new();
    private readonly AzureDevOpsService _service;

    public AzureDevOpsServiceTests()
    {
        var options = Options.Create(new AzureDevOpsOptions
        {
            CoreServer = ApiFactory.CoreServer,
            Organization = ApiFactory.Organization,
            ApiVersion = ApiFactory.ApiVersion,
            AccessToken = ApiFactory.AccessToken
        });
        _service = new AzureDevOpsService(_api, options);
    }

    [Fact]
    public async Task Lista_projetos_da_organizacao_configurada()
    {
        _api.Projects = [new Project { Name = "Alfa" }];

        var result = await _service.GetAllProjectsAsync();

        Assert.Equal("Alfa", Assert.Single(result).Name);
        Assert.Equal($"projects {ApiFactory.Organization} {ApiFactory.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Lista_equipes_do_projeto()
    {
        _api.Teams = [new Team { Name = "Time A" }];

        var result = await _service.GetTeamsPerProjectAsync("p1");

        Assert.Equal("Time A", Assert.Single(result).Name);
        Assert.Equal($"teams {ApiFactory.Organization} p1 {ApiFactory.ApiVersion}", Assert.Single(_api.Calls));
    }

    [Fact]
    public async Task Lista_membros_da_equipe()
    {
        _api.Members = [new TeamMember { IsTeamAdmin = true }];

        var result = await _service.GetTeamMembersAsync("p1", "t1");

        Assert.True(Assert.Single(result).IsTeamAdmin);
        Assert.Equal($"members {ApiFactory.Organization} p1 t1 {ApiFactory.ApiVersion}", Assert.Single(_api.Calls));
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

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

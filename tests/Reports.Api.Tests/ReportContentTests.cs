using System.Net;
using Reports.Api.Models;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class ReportContentTests
{
    private const string Projetos = "<h2>Projetos</h2>";
    private const string Times = "<h2>Times</h2>";
    private const string Usuarios = "<h2>Usuários</h2>";

    private static readonly Identity Ana = new() { DisplayName = "Ana Lima", UniqueName = "ana@example.com" };
    private static readonly Identity Bia = new() { DisplayName = "Bia Souza", UniqueName = "bia@example.com" };

    private static FakeAzureDevOpsApi ApiComDoisTimes()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };
        api.TeamsByProject["p1"] =
        [
            new Team { Id = "t1", Name = "Time A", Description = "Primeiro time" },
            new Team { Id = "t2", Name = "Time B" }
        ];
        api.MembersByTeam["t1"] = [new TeamMember { IsTeamAdmin = true, Identity = Ana }, new TeamMember { Identity = Bia }];
        api.MembersByTeam["t2"] = [new TeamMember { Identity = Ana }];
        return api;
    }

    private static async Task<(string Html, FakeAzureDevOpsApi Api)> GerarAsync(FakeAzureDevOpsApi api, string include)
    {
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json($$"""{ "include": { {{include}} } }"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (WebUtility.HtmlDecode(pdf.Html!), api);
    }

    [Fact]
    public async Task Projetos_nao_consulta_times_nem_membros()
    {
        var (html, api) = await GerarAsync(ApiComDoisTimes(), """ "projects": true """);

        Assert.Contains(Projetos, html);
        Assert.DoesNotContain(Times, html);
        Assert.DoesNotContain(Usuarios, html);
        Assert.All(api.Calls, c => Assert.StartsWith("projects ", c));
    }

    [Fact]
    public async Task Times_lista_os_times_com_o_projeto_sem_consultar_membros()
    {
        var (html, api) = await GerarAsync(ApiComDoisTimes(), """ "teams": true """);

        Assert.Contains(Times, html);
        Assert.DoesNotContain(Projetos, html);
        Assert.DoesNotContain(Usuarios, html);
        Assert.Contains("Projeto Alfa", html);
        Assert.Contains("Time A", html);
        Assert.Contains("Primeiro time", html);
        Assert.Contains("Time B", html);
        Assert.DoesNotContain(api.Calls, c => c.StartsWith("members "));
    }

    [Fact]
    public async Task Usuarios_repete_o_usuario_em_cada_time()
    {
        var (html, api) = await GerarAsync(ApiComDoisTimes(), """ "users": true """);

        Assert.Contains(Usuarios, html);
        Assert.DoesNotContain(Projetos, html);
        Assert.DoesNotContain(Times, html);
        Assert.Equal(2, Contar(html, "ana@example.com"));
        Assert.Equal(1, Contar(html, "bia@example.com"));
        Assert.Contains("Ana Lima", html);
        Assert.Contains("Time A", html);
        Assert.Contains("Time B", html);
        Assert.Equal(2, api.Calls.Count(c => c.StartsWith("members ")));
    }

    [Fact]
    public async Task Usuarios_indica_quem_e_administrador_do_time()
    {
        var (html, _) = await GerarAsync(ApiComDoisTimes(), """ "users": true """);

        var linhaAna = LinhaCom(html, "ana@example.com");
        Assert.Contains("Sim", linhaAna);
        Assert.Contains("Não", LinhaCom(html, "bia@example.com"));
    }

    [Fact]
    public async Task Tres_secoes_aparecem_na_ordem_fixa_independente_da_ordem_das_flags()
    {
        var (html, _) = await GerarAsync(ApiComDoisTimes(), """ "users": true, "teams": true, "projects": true """);

        var projetos = html.IndexOf(Projetos, StringComparison.Ordinal);
        var times = html.IndexOf(Times, StringComparison.Ordinal);
        var usuarios = html.IndexOf(Usuarios, StringComparison.Ordinal);
        Assert.True(projetos >= 0 && projetos < times && times < usuarios);
    }

    [Fact]
    public async Task Projetos_sao_ordenados_sem_diferenciar_maiusculas_nem_acentos()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects =
            [
                new Project { Id = "1", Name = "beta" },
                new Project { Id = "2", Name = "Alfa" },
                new Project { Id = "3", Name = "Ágil" }
            ]
        };

        var (html, _) = await GerarAsync(api, """ "projects": true """);

        AssertOrdem(html, "Ágil", "Alfa", "beta");
    }

    [Fact]
    public async Task Times_e_usuarios_sao_ordenados_por_projeto_time_e_nome()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "pb", Name = "Projeto B" }, new Project { Id = "pa", Name = "Projeto A" }]
        };
        api.TeamsByProject["pb"] = [new Team { Id = "tz", Name = "Time Z" }, new Team { Id = "ta", Name = "Time A" }];
        api.TeamsByProject["pa"] = [new Team { Id = "tm", Name = "Time M" }];
        api.MembersByTeam["tm"] =
        [
            new TeamMember { Identity = new Identity { DisplayName = "Zélia", UniqueName = "zelia@example.com" } },
            new TeamMember { Identity = new Identity { DisplayName = "ana", UniqueName = "ana2@example.com" } }
        ];
        api.MembersByTeam["ta"] = [new TeamMember { Identity = Bia }];
        api.MembersByTeam["tz"] = [new TeamMember { Identity = Ana }];

        var (html, _) = await GerarAsync(api, """ "teams": true, "users": true """);

        var usuarios = html[html.IndexOf(Usuarios, StringComparison.Ordinal)..];
        AssertOrdem(usuarios, "ana2@example.com", "zelia@example.com", "bia@example.com", "ana@example.com");
        var times = html[html.IndexOf(Times, StringComparison.Ordinal)..html.IndexOf(Usuarios, StringComparison.Ordinal)];
        AssertOrdem(times, "Time M", "Time A", "Time Z");
    }

    [Theory]
    [InlineData(""" "projects": true """, "Nenhum projeto encontrado.")]
    [InlineData(""" "teams": true """, "Nenhum time encontrado.")]
    [InlineData(""" "users": true """, "Nenhum usuário encontrado.")]
    public async Task Secao_sem_dados_mostra_mensagem(string include, string mensagem)
    {
        var api = new FakeAzureDevOpsApi { Projects = [] };
        if (include.Contains("projects") == false)
        {
            api.Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }];
            api.TeamsByProject["p1"] = [];
        }

        var (html, _) = await GerarAsync(api, include);

        Assert.Contains(mensagem, html);
    }

    private static int Contar(string texto, string trecho) =>
        texto.Split(trecho).Length - 1;

    private static string LinhaCom(string html, string trecho)
    {
        var meio = html.IndexOf(trecho, StringComparison.Ordinal);
        var inicio = html.LastIndexOf("<tr", meio, StringComparison.Ordinal);
        return html[inicio..html.IndexOf("</tr>", meio, StringComparison.Ordinal)];
    }

    private static void AssertOrdem(string html, params string[] trechos)
    {
        var posicoes = trechos.Select(t => html.IndexOf(t, StringComparison.Ordinal)).ToArray();
        Assert.DoesNotContain(-1, posicoes);
        Assert.Equal(posicoes.Order(), posicoes);
    }
}

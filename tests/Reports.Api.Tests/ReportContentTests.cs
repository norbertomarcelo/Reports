using System.Net;
using System.Text.Json;
using Reports.Api.Models;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

public class ReportContentTests
{
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

        Assert.Equal([(1, "1. Projetos")], Titulos(html));
        Assert.All(api.Calls, c => Assert.True(c.StartsWith("projects ") || c.StartsWith("properties "), c));
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

    private static FakeAzureDevOpsApi ApiComDoisProjetos()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "pb", Name = "Beta" }, new Project { Id = "pa", Name = "Alfa" }]
        };
        api.TeamsByProject["pb"] = [new Team { Id = "tz", Name = "Time Z", Description = "Zeta" }, new Team { Id = "ta", Name = "Time A" }];
        api.TeamsByProject["pa"] = [new Team { Id = "tm", Name = "Time M", Description = "Eme" }];
        api.MembersByTeam["tm"] =
        [
            new TeamMember { Identity = new Identity { DisplayName = "Zélia", UniqueName = "zelia@example.com" } },
            new TeamMember { IsTeamAdmin = true, Identity = new Identity { DisplayName = "ana", UniqueName = "ana2@example.com" } }
        ];
        api.MembersByTeam["ta"] = [new TeamMember { Identity = Bia }];
        api.MembersByTeam["tz"] = [new TeamMember { Identity = Ana }];
        return api;
    }

    private static List<(int Nivel, string Texto)> Titulos(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, "<h([1-3])[^>]*>(.*?)</h\\1>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(m => (int.Parse(m.Groups[1].Value),
                System.Text.RegularExpressions.Regex.Replace(m.Groups[2].Value, "\\s+", " ").Trim()))
            .ToList();

    [Fact]
    public async Task Tres_secoes_formam_a_hierarquia_de_capitulos_completa()
    {
        var (html, _) = await GerarAsync(ApiComDoisProjetos(), """ "users": true, "teams": true, "projects": true """);

        Assert.Equal(
            [
                (1, "1. Projetos"),
                (1, "2. Alfa"), (2, "2.1 Times"), (2, "2.2 Usuários por time"), (3, "2.2.1 Time M"),
                (1, "3. Beta"), (2, "3.1 Times"), (2, "3.2 Usuários por time"), (3, "3.2.1 Time A"), (3, "3.2.2 Time Z")
            ],
            Titulos(html));
    }

    [Fact]
    public async Task So_projetos_nao_cria_capitulos_de_projeto_nem_consulta_times()
    {
        var (html, api) = await GerarAsync(ApiComDoisProjetos(), """ "projects": true """);

        Assert.Equal([(1, "1. Projetos")], Titulos(html));
        Assert.DoesNotContain(api.Calls, c => c.StartsWith("teams ") || c.StartsWith("members "));
    }

    [Fact]
    public async Task So_times_numera_os_capitulos_de_projeto_a_partir_do_1_sem_consultar_membros()
    {
        var (html, api) = await GerarAsync(ApiComDoisProjetos(), """ "teams": true """);

        Assert.Equal([(1, "1. Alfa"), (2, "1.1 Times"), (1, "2. Beta"), (2, "2.1 Times")], Titulos(html));
        Assert.DoesNotContain(api.Calls, c => c.StartsWith("members ") || c.StartsWith("properties "));
    }

    [Fact]
    public async Task So_usuarios_tem_apenas_usuarios_por_time_como_subcapitulo()
    {
        var (html, _) = await GerarAsync(ApiComDoisProjetos(), """ "users": true """);

        Assert.Equal(
            [
                (1, "1. Alfa"), (2, "1.1 Usuários por time"), (3, "1.1.1 Time M"),
                (1, "2. Beta"), (2, "2.1 Usuários por time"), (3, "2.1.1 Time A"), (3, "2.1.2 Time Z")
            ],
            Titulos(html));
    }

    [Fact]
    public async Task Tabela_de_times_tem_so_time_e_descricao_do_proprio_projeto()
    {
        var (html, _) = await GerarAsync(ApiComDoisProjetos(), """ "teams": true """);

        var alfa = html[html.IndexOf("1. Alfa", StringComparison.Ordinal)..html.IndexOf("2. Beta", StringComparison.Ordinal)];
        Assert.Equal(["Time", "Descrição"], Cabecalhos(alfa));
        Assert.Contains("Time M", alfa);
        Assert.Contains("Eme", alfa);
        Assert.DoesNotContain("Time A", alfa);
        Assert.DoesNotContain("Time Z", alfa);
        Assert.DoesNotContain("Projeto", html);
    }

    [Fact]
    public async Task Tabela_de_usuarios_tem_so_nome_email_e_administrador_ordenados_por_nome()
    {
        var (html, _) = await GerarAsync(ApiComDoisProjetos(), """ "users": true """);

        var timeM = html[html.IndexOf("1.1.1 Time M", StringComparison.Ordinal)..html.IndexOf("2. Beta", StringComparison.Ordinal)];
        Assert.Equal(["Nome", "E-mail", "Administrador"], Cabecalhos(timeM));
        AssertOrdem(timeM, "ana2@example.com", "zelia@example.com");
        Assert.Equal(["ana", "ana2@example.com", "Sim"], Celulas(LinhaCom(timeM, "ana2@example.com")));
        Assert.Equal(["Zélia", "zelia@example.com", "Não"], Celulas(LinhaCom(timeM, "zelia@example.com")));
    }

    [Fact]
    public async Task Usuario_em_dois_times_aparece_em_cada_um()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };
        api.TeamsByProject["p1"] = [new Team { Id = "t1", Name = "Time A" }, new Team { Id = "t2", Name = "Time B" }];
        api.MembersByTeam["t1"] = [new TeamMember { Identity = Ana }];
        api.MembersByTeam["t2"] = [new TeamMember { Identity = Ana }];

        var (html, _) = await GerarAsync(api, """ "users": true """);

        var timeA = html[html.IndexOf("1.1.1 Time A", StringComparison.Ordinal)..html.IndexOf("1.1.2 Time B", StringComparison.Ordinal)];
        var timeB = html[html.IndexOf("1.1.2 Time B", StringComparison.Ordinal)..];
        Assert.Contains("ana@example.com", timeA);
        Assert.Contains("ana@example.com", timeB);
        Assert.Equal(2, Contar(html, "ana@example.com"));
    }

    [Fact]
    public async Task Projeto_sem_times_mostra_a_mensagem_nos_dois_subcapitulos()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };

        var (html, _) = await GerarAsync(api, """ "teams": true, "users": true """);

        Assert.Equal([(1, "1. Projeto Alfa"), (2, "1.1 Times"), (2, "1.2 Usuários por time")], Titulos(html));
        Assert.Equal(2, Contar(html, "Nenhum time encontrado."));
    }

    [Fact]
    public async Task Time_sem_membros_mostra_a_mensagem()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };
        api.TeamsByProject["p1"] = [new Team { Id = "t1", Name = "Time A" }];

        var (html, _) = await GerarAsync(api, """ "users": true """);

        Assert.Contains((3, "1.1.1 Time A"), Titulos(html));
        Assert.Contains("Nenhum usuário encontrado.", html);
    }

    [Theory]
    [InlineData(""" "projects": true """, "1. Projetos")]
    [InlineData(""" "teams": true """, null)]
    [InlineData(""" "users": true, "projects": true """, "1. Projetos")]
    public async Task Organizacao_sem_projetos_mostra_a_mensagem(string include, string? capitulo)
    {
        var (html, _) = await GerarAsync(new FakeAzureDevOpsApi(), include);

        Assert.Contains("Nenhum projeto encontrado.", html);
        Assert.Equal(capitulo is null ? [] : [(1, capitulo)], Titulos(html));
    }

    [Fact]
    public async Task Nao_ha_mais_secoes_planas_de_times_e_usuarios()
    {
        var (html, _) = await GerarAsync(ApiComDoisProjetos(), """ "projects": true, "teams": true, "users": true """);

        Assert.DoesNotContain(Titulos(html), t => t.Texto is "Times" or "Usuários");
        Assert.DoesNotContain(">Projeto<", html);
    }

    /// <summary>Cabeçalhos da primeira tabela do trecho.</summary>
    private static string[] Cabecalhos(string trecho)
    {
        var linha = trecho[trecho.IndexOf("<tr", StringComparison.Ordinal)..];
        linha = linha[..linha.IndexOf("</tr>", StringComparison.Ordinal)];
        return System.Text.RegularExpressions.Regex.Matches(linha, "<th[^>]*>(.*?)</th>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value.Trim()).ToArray();
    }

    private static ProjectProperty Prop(string name, string jsonValue) =>
        new() { Name = name, Value = JsonDocument.Parse(jsonValue).RootElement.Clone() };

    [Fact]
    public async Task Propriedades_sao_buscadas_uma_por_vez_na_ordem_da_listagem()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "p1", Name = "Beta" }, new Project { Id = "p2", Name = "Alfa" }]
        };

        await GerarAsync(api, """ "projects": true """);

        Assert.Equal(
            [
                $"projects {ApiFactory.Organization} {ApiFactory.ApiVersion}",
                $"properties {ApiFactory.Organization} p1 {ApiFactory.ProjectPropertiesApiVersion}",
                $"properties {ApiFactory.Organization} p2 {ApiFactory.ProjectPropertiesApiVersion}"
            ],
            api.Calls);
    }

    [Fact]
    public async Task Times_e_usuarios_sem_projetos_nao_buscam_propriedades()
    {
        var (_, api) = await GerarAsync(ApiComDoisTimes(), """ "teams": true, "users": true """);

        Assert.DoesNotContain(api.Calls, c => c.StartsWith("properties "));
    }

    [Fact]
    public async Task Projetos_mostra_as_propriedades_traduzidas_na_ordem_das_colunas()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };
        api.PropertiesByProject["p1"] =
        [
            Prop("System.Process Template", "\"Scrum\""),
            Prop("System.SourceControlCapabilityFlags", "\"2\""),
            Prop("System.SourceControlGitEnabled", "\"True\""),
            Prop("System.SourceControlGitPermissionsInitialized", "\"False\""),
            Prop("System.Microsoft.TeamFoundation.Team.Count", "4")
        ];

        var (html, _) = await GerarAsync(api, """ "projects": true """);

        AssertOrdem(html, "Nome", "Estado", "Visibilidade", "Processo", "Controle de versão", "Git habilitado",
            "Permissões do Git configuradas", "Última Atualização");
        var celulas = Celulas(LinhaCom(html, "Projeto Alfa"));
        Assert.Equal(["Scrum", "Git", "Sim", "Não"], celulas[3..7]);
        Assert.DoesNotContain("Qtd. de times", html);
        Assert.DoesNotContain("Quantidade de times", html);
        Assert.Equal(0, Contar(LinhaCom(html, "Projeto Alfa"), ">4<"));
    }

    [Fact]
    public async Task Projeto_sem_propriedades_continua_no_relatorio_com_traco()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };

        var (html, _) = await GerarAsync(api, """ "projects": true """);

        Assert.Equal(["—", "—", "—", "—"], Celulas(LinhaCom(html, "Projeto Alfa"))[3..7]);
    }

    [Fact]
    public async Task Relatorio_nao_tem_legenda()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };
        var (comProjetos, _) = await GerarAsync(api, """ "projects": true """);
        var (semProjetos, _) = await GerarAsync(new FakeAzureDevOpsApi(), """ "projects": true """);

        foreach (var html in new[] { comProjetos, semProjetos })
        {
            foreach (var texto in new[] { "Processo:", "Controle de versão:", "Git habilitado:", "Permissões do Git configuradas:", "legenda" })
                Assert.DoesNotContain(texto, html);
        }
        Assert.Contains("Nenhum projeto encontrado.", semProjetos);
    }

    [Fact]
    public async Task Nome_do_projeto_e_texto_simples_sem_link()
    {
        const string url = "https://dev.azure.example/org-teste/_apis/projects/p1";
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa", Url = url }] };

        var (html, _) = await GerarAsync(api, """ "projects": true """);

        Assert.Equal("Projeto Alfa", Celulas(LinhaCom(html, "Projeto Alfa"))[0]);
        Assert.DoesNotContain("<a ", html);
        Assert.DoesNotContain(url, html);
    }

    [Fact]
    public async Task Estado_e_visibilidade_aparecem_traduzidos()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects =
            [
                new Project { Id = "p1", Name = "Projeto Alfa", State = "wellFormed", Visibility = "private" },
                new Project { Id = "p2", Name = "Projeto Beta", State = "all", Visibility = "public" }
            ]
        };

        var (html, _) = await GerarAsync(api, """ "projects": true """);

        Assert.Equal(["Ativo", "Privado"], Celulas(LinhaCom(html, "Projeto Alfa"))[1..3]);
        Assert.Equal(["—", "Público"], Celulas(LinhaCom(html, "Projeto Beta"))[1..3]);
        Assert.DoesNotContain("wellFormed", html);
        Assert.DoesNotContain("private", html);
    }

    [Fact]
    public async Task Falha_nas_propriedades_interrompe_o_relatorio_sem_gerar_pdf()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }],
            PropertiesError = new InvalidOperationException("falha simulada")
        };
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "projects": true } }"""));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(pdf.Html);
    }

    private static string[] Celulas(string linha) =>
        System.Text.RegularExpressions.Regex.Matches(linha, "<td[^>]*>(.*?)</td>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(m => System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "<.*?>", "").Trim()).ToArray();

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

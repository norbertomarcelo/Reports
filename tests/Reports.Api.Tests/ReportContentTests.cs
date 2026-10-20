using System.Net;
using System.Text.Json;
using Reports.Api.Models;
using Reports.Api.Services;
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

    private static async Task<(string Html, FakeAzureDevOpsApi Api)> GerarAsync(
        FakeAzureDevOpsApi api, string include, params string[] internalDomains)
    {
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf, internalDomains);

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
                (2, "2.3 Usuários do projeto"),
                (1, "3. Beta"), (2, "3.1 Times"), (2, "3.2 Usuários por time"), (3, "3.2.1 Time A"), (3, "3.2.2 Time Z"),
                (2, "3.3 Usuários do projeto")
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
                (1, "1. Alfa"), (2, "1.1 Usuários por time"), (3, "1.1.1 Time M"), (2, "1.2 Usuários do projeto"),
                (1, "2. Beta"), (2, "2.1 Usuários por time"), (3, "2.1.1 Time A"), (3, "2.1.2 Time Z"),
                (2, "2.2 Usuários do projeto")
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
        var timeB = Trecho(html, "1.1.2 Time B", "1.2 Usuários do projeto");
        Assert.Contains("ana@example.com", timeA);
        Assert.Contains("ana@example.com", timeB);
        Assert.Equal(1, Contar(Trecho(html, "1.2 Usuários do projeto"), "ana@example.com"));
    }

    [Fact]
    public async Task Projeto_sem_times_mostra_a_mensagem_nos_dois_subcapitulos()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Projeto Alfa" }] };

        var (html, _) = await GerarAsync(api, """ "teams": true, "users": true """);

        Assert.Equal(
            [(1, "1. Projeto Alfa"), (2, "1.1 Times"), (2, "1.2 Usuários por time"), (2, "1.3 Usuários do projeto")],
            Titulos(html));
        Assert.Equal(2, Contar(html, "Nenhum time encontrado."));
        Assert.Contains("Nenhum usuário encontrado.", Trecho(html, "1.3 Usuários do projeto"));
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


    // Spec 016: indicadores de times e usuários.

    private static TeamMember Membro(string id, string nome, string email, bool admin = false) =>
        new() { IsTeamAdmin = admin, Identity = new Identity { Id = id, DisplayName = nome, UniqueName = email } };

    private static TeamMember Grupo(string nome) =>
        new() { Identity = new Identity { Id = "g-" + nome, DisplayName = nome, UniqueName = $"[Alfa]\\{nome}" } };

    private static FakeAzureDevOpsApi ApiComUmProjeto(params (string Id, string Nome, List<TeamMember> Membros)[] times)
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "p1", Name = "Alfa" }] };
        api.TeamsByProject["p1"] = times.Select(t => new Team { Id = t.Id, Name = t.Nome }).ToList();
        foreach (var time in times)
            api.MembersByTeam[time.Id] = time.Membros;
        return api;
    }

    [Fact]
    public async Task Times_mostram_membros_e_administradores_sem_contar_grupos()
    {
        var api = ApiComUmProjeto(("t1", "Time A",
        [
            Membro("u1", "Ana", "ana@example.com", admin: true),
            Membro("u2", "Bia", "bia@example.com"),
            Membro("u3", "Caio", "caio@example.com"),
            Grupo("Contributors")
        ]));

        var (html, _) = await GerarAsync(api, """ "teams": true, "users": true """);

        var times = Trecho(html, "1.1 Times", "1.2 Usuários por time");
        Assert.Equal(["Time", "Descrição", "Membros", "Administradores"], Cabecalhos(times));
        Assert.Equal(["3", "1"], Celulas(LinhaCom(times, "Time A"))[2..]);
    }

    [Fact]
    public async Task Time_com_membros_e_sem_administrador_mostra_a_nota()
    {
        var api = ApiComUmProjeto(
            ("t1", "Time A", [Membro("u1", "Ana", "ana@example.com"), Membro("u2", "Bia", "bia@example.com")]),
            ("t2", "Time B", []));

        var (html, _) = await GerarAsync(api, """ "teams": true, "users": true """);

        var times = Trecho(html, "1.1 Times", "1.2 Usuários por time");
        Assert.Equal(["2", "Sem administrador"], Celulas(LinhaCom(times, "Time A"))[2..]);
        Assert.Equal(["0", "0"], Celulas(LinhaCom(times, "Time B"))[2..]);
        Assert.Equal(1, Contar(html, "Sem administrador"));
    }

    [Fact]
    public async Task Grupo_aparece_marcado_sem_email_e_fora_de_usuarios_do_projeto()
    {
        var api = ApiComUmProjeto(("t1", "Time A", [Membro("u1", "Ana", "ana@example.com"), Grupo("Contributors")]));

        var (html, _) = await GerarAsync(api, """ "users": true """);

        var timeA = Trecho(html, "1.1.1 Time A", "1.2 Usuários do projeto");
        Assert.Equal(["Contributors Grupo", "", "Não"], Celulas(LinhaCom(timeA, "Contributors")));
        Assert.DoesNotContain("[Alfa]", html);
        Assert.DoesNotContain("Contributors", Trecho(html, "1.2 Usuários do projeto"));
    }

    private static FakeAzureDevOpsApi ApiComDominios() =>
        ApiComUmProjeto(("t1", "Time A",
        [
            Membro("u1", "Ana", "ana@empresa.com.br", admin: true),
            Membro("u2", "Bia", "bia@EMPRESA.com.br"),
            Membro("u3", "Carlos", "carlos@gmail.com"),
            Membro("u4", "Dani", "dani@ti.empresa.com.br")
        ]));

    [Fact]
    public async Task Usuario_de_dominio_fora_da_lista_e_marcado_como_externo()
    {
        var (html, _) = await GerarAsync(ApiComDominios(), """ "users": true """, "empresa.com.br");

        var timeA = Trecho(html, "1.1.1 Time A", "1.2 Usuários do projeto");
        var projeto = Trecho(html, "1.2 Usuários do projeto");
        foreach (var trecho in new[] { timeA, projeto })
        {
            Assert.Equal("Ana", Celulas(LinhaCom(trecho, "ana@empresa.com.br"))[0]);
            Assert.Equal("Bia", Celulas(LinhaCom(trecho, "bia@EMPRESA.com.br"))[0]);
            Assert.Equal("Carlos Externo", Celulas(LinhaCom(trecho, "carlos@gmail.com"))[0]);
            Assert.Equal("Dani Externo", Celulas(LinhaCom(trecho, "dani@ti.empresa.com.br"))[0]);
        }
    }

    [Fact]
    public async Task Sem_InternalDomains_ninguem_e_externo()
    {
        var (html, _) = await GerarAsync(ApiComDominios(), """ "users": true """);

        Assert.DoesNotContain("Externo", html);
    }

    [Fact]
    public async Task Usuarios_do_projeto_lista_cada_usuario_uma_vez_com_seus_times()
    {
        var ana = new Identity { Id = "u1", DisplayName = "Ana", UniqueName = "ana@example.com" };
        var api = ApiComUmProjeto(
            ("t2", "Time B", [new TeamMember { Identity = ana }, Membro("u2", "Bruno", "bruno@example.com")]),
            ("t1", "Time A", [new TeamMember { IsTeamAdmin = true, Identity = ana }]));

        var (html, _) = await GerarAsync(api, """ "users": true """);

        var projeto = Trecho(html, "1.2 Usuários do projeto");
        Assert.Equal(["Nome", "E-mail", "Times", "Administrador em"], Cabecalhos(projeto));
        Assert.Equal(1, Contar(projeto, "ana@example.com"));
        Assert.Equal(["Ana", "ana@example.com", "Time A, Time B", "Time A"], Celulas(LinhaCom(projeto, "ana@example.com")));
        Assert.Equal(["Bruno", "bruno@example.com", "Time B", ""], Celulas(LinhaCom(projeto, "bruno@example.com")));
    }

    [Fact]
    public async Task Usuarios_do_projeto_sao_ordenados_por_nome()
    {
        var api = ApiComUmProjeto(("t1", "Time A",
            [Membro("u1", "Zélia", "zelia@example.com"), Membro("u2", "ana", "ana@example.com")]));

        var (html, _) = await GerarAsync(api, """ "users": true """);

        AssertOrdem(Trecho(html, "1.2 Usuários do projeto"), "ana@example.com", "zelia@example.com");
    }

    // Spec 017: contagem de itens de trabalho.

    private static WorkItemGroup Grupo(string tipo, string categoria, string? area, long total) =>
        new()
        {
            WorkItemType = tipo,
            StateCategory = categoria,
            Area = area is null ? null : new WorkItemArea { AreaPath = area },
            Count = total
        };

    private static async Task<string> GerarComItensAsync(
        FakeAzureDevOpsApi api, FakeAzureDevOpsAnalyticsApi analytics, string include)
    {
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf, analytics);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json($$"""{ "include": { {{include}} } }"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return WebUtility.HtmlDecode(pdf.Html!);
    }

    private static FakeAzureDevOpsApi ProjetoAlfa() =>
        new() { Projects = [new Project { Id = "pa", Name = "Alfa" }] };

    private static FakeAzureDevOpsAnalyticsApi AnalyticsDeAlfa()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] =
        [
            Grupo("Bug", "InProgress", "Alfa\\Backend", 2),
            Grupo("Bug", "Completed", "Alfa\\Backend", 1),
            Grupo("Task", "Proposed", "Alfa\\Frontend", 3),
            Grupo("Task", "Resolved", "Alfa\\Frontend", 1)
        ];
        return analytics;
    }

    /// <summary>Linhas com células de dados (<c>td</c>) da primeira tabela do trecho.</summary>
    private static string[][] Linhas(string trecho) =>
        System.Text.RegularExpressions.Regex.Matches(trecho, "<tr[^>]*>(.*?)</tr>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(m => Celulas(m.Groups[1].Value))
            .Where(c => c.Length > 0)
            .ToArray();

    [Fact]
    public async Task Sem_a_flag_nao_ha_chamadas_ao_analytics_nem_capitulo_de_itens()
    {
        var analytics = AnalyticsDeAlfa();

        var html = await GerarComItensAsync(ApiComDoisProjetos(), analytics,
            """ "projects": true, "teams": true, "users": true """);

        Assert.Empty(analytics.Calls);
        Assert.DoesNotContain("Itens de trabalho", html);
    }

    [Fact]
    public async Task Faz_uma_chamada_ao_analytics_por_projeto_na_ordem_da_listagem_sem_consultar_times_nem_membros()
    {
        var api = ApiComDoisProjetos();
        var analytics = new FakeAzureDevOpsAnalyticsApi();

        await GerarComItensAsync(api, analytics, """ "workItems": true """);

        Assert.Equal(
            [
                $"analytics {ApiFactory.Organization} Beta {AzureDevOpsService.WorkItemGroupsQuery}",
                $"analytics {ApiFactory.Organization} Alfa {AzureDevOpsService.WorkItemGroupsQuery}"
            ],
            analytics.Calls);
        Assert.Equal([$"projects {ApiFactory.Organization} {ApiFactory.ApiVersion}"], api.Calls);
    }

    [Fact]
    public async Task So_itens_de_trabalho_numera_o_capitulo_geral_e_o_subcapitulo_de_cada_projeto()
    {
        var html = await GerarComItensAsync(ProjetoAlfa(), AnalyticsDeAlfa(), """ "workItems": true """);

        Assert.Equal(
            [(1, "1. Itens de trabalho"), (1, "2. Alfa"), (2, "2.1 Itens de trabalho")],
            Titulos(html));
    }

    [Fact]
    public async Task Todas_as_flags_deixam_itens_de_trabalho_como_capitulo_2_e_ultimo_subcapitulo()
    {
        var html = await GerarComItensAsync(ApiComDoisProjetos(), new FakeAzureDevOpsAnalyticsApi(),
            """ "projects": true, "teams": true, "users": true, "workItems": true """);

        Assert.Equal(
            [
                (1, "1. Projetos"), (1, "2. Itens de trabalho"),
                (1, "3. Alfa"), (2, "3.1 Times"), (2, "3.2 Usuários por time"), (3, "3.2.1 Time M"),
                (2, "3.3 Usuários do projeto"), (2, "3.4 Itens de trabalho"),
                (1, "4. Beta"), (2, "4.1 Times"), (2, "4.2 Usuários por time"), (3, "4.2.1 Time A"), (3, "4.2.2 Time Z"),
                (2, "4.3 Usuários do projeto"), (2, "4.4 Itens de trabalho")
            ],
            Titulos(html));
    }

    [Fact]
    public async Task Subcapitulo_mostra_resumo_e_contagem_por_tipo_e_por_area()
    {
        var html = await GerarComItensAsync(ProjetoAlfa(), AnalyticsDeAlfa(), """ "workItems": true """);

        var projeto = Trecho(html, "2.1 Itens de trabalho");
        Assert.Contains("Total: 7 · Abertos: 6 · Fechados: 1", projeto);

        var porTipo = Trecho(projeto, "Por tipo", "Por área");
        Assert.Equal(["Tipo", "Abertos", "Fechados", "Total"], Cabecalhos(porTipo));
        Assert.Equal(
            [["Bug", "2", "1", "3"], ["Task", "4", "0", "4"], ["Total", "6", "1", "7"]],
            Linhas(porTipo));

        var porArea = Trecho(projeto, "Por área");
        Assert.Equal(["Área", "Abertos", "Fechados", "Total"], Cabecalhos(porArea));
        Assert.Equal(
            [["Alfa\\Backend", "2", "1", "3"], ["Alfa\\Frontend", "4", "0", "4"], ["Total", "6", "1", "7"]],
            Linhas(porArea));
    }

    [Fact]
    public async Task Item_sem_area_aparece_na_linha_sem_area()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] = [Grupo("Bug", "Proposed", null, 2), Grupo("Bug", "Proposed", "Alfa\\Backend", 1)];

        var html = await GerarComItensAsync(ProjetoAlfa(), analytics, """ "workItems": true """);

        var porArea = Trecho(html, "Por área");
        Assert.Equal(
            [["(sem área)", "2", "0", "2"], ["Alfa\\Backend", "1", "0", "1"], ["Total", "3", "0", "3"]],
            Linhas(porArea));
    }

    [Fact]
    public async Task Capitulo_geral_mostra_abertos_fechados_e_total_por_projeto_com_a_linha_total()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Beta"] = [Grupo("Bug", "Proposed", "Beta", 5), Grupo("Bug", "Completed", "Beta", 2)];
        analytics.GroupsByProject["Alfa"] = [Grupo("Task", "InProgress", "Alfa", 6), Grupo("Task", "Completed", "Alfa", 1)];

        var html = await GerarComItensAsync(ApiComDoisProjetos(), analytics, """ "workItems": true """);

        var geral = Trecho(html, "1. Itens de trabalho", "2. Alfa");
        Assert.Equal(["Projeto", "Abertos", "Fechados", "Total"], Cabecalhos(geral));
        Assert.Equal(
            [["Alfa", "6", "1", "7"], ["Beta", "5", "2", "7"], ["Total", "11", "3", "14"]],
            Linhas(geral));
    }

    [Fact]
    public async Task Projeto_sem_itens_tem_linha_com_zeros_e_a_mensagem_no_subcapitulo()
    {
        var html = await GerarComItensAsync(ProjetoAlfa(), new FakeAzureDevOpsAnalyticsApi(), """ "workItems": true """);

        Assert.Equal([["Alfa", "0", "0", "0"], ["Total", "0", "0", "0"]], Linhas(Trecho(html, "1. Itens de trabalho", "2. Alfa")));
        var projeto = Trecho(html, "2.1 Itens de trabalho");
        Assert.Contains("Nenhum item de trabalho encontrado.", projeto);
        Assert.DoesNotContain("<table", projeto);
    }

    [Fact]
    public async Task Resposta_com_mais_de_uma_pagina_mostra_o_aviso_de_contagem_parcial_so_no_projeto_afetado()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.ProjectsWithNextPage.Add("Beta");

        var html = await GerarComItensAsync(ApiComDoisProjetos(), analytics, """ "workItems": true """);

        Assert.Equal(1, Contar(html, "Contagem parcial"));
        Assert.Contains("Contagem parcial", Trecho(html, "3. Beta"));
        Assert.DoesNotContain("Contagem parcial", Trecho(html, "2. Alfa", "3. Beta"));
    }

    [Fact]
    public async Task Organizacao_sem_projetos_com_projetos_pedidos_mostra_o_capitulo_com_a_mensagem()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi();

        var html = await GerarComItensAsync(new FakeAzureDevOpsApi(), analytics, """ "projects": true, "workItems": true """);

        Assert.Equal([(1, "1. Projetos"), (1, "2. Itens de trabalho")], Titulos(html));
        Assert.Contains("Nenhum item de trabalho encontrado.", html);
        Assert.Empty(analytics.Calls);
    }

    [Fact]
    public async Task Organizacao_sem_projetos_e_sem_projects_mostra_so_a_mensagem_de_projetos()
    {
        var html = await GerarComItensAsync(new FakeAzureDevOpsApi(), new FakeAzureDevOpsAnalyticsApi(), """ "workItems": true """);

        Assert.Contains("Nenhum projeto encontrado.", html);
        Assert.Empty(Titulos(html));
    }

    [Fact]
    public async Task Falha_no_analytics_interrompe_o_relatorio_sem_gerar_pdf()
    {
        var analytics = new FakeAzureDevOpsAnalyticsApi { Error = new InvalidOperationException("falha simulada") };
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(ProjetoAlfa(), pdf, analytics);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "workItems": true } }"""));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(pdf.Html);
    }

    private static string Trecho(string html, string de, string? ate = null)
    {
        var inicio = html.IndexOf(de, StringComparison.Ordinal);
        Assert.True(inicio >= 0, de);
        var fim = ate is null ? html.Length : html.IndexOf(ate, inicio, StringComparison.Ordinal);
        Assert.True(fim >= 0, ate);
        return html[inicio..fim];
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

    // Spec 018: repositórios.

    private static GitRepository Repo(string projectId, string id, string name, bool empty = false, bool disabled = false) =>
        new()
        {
            Id = id,
            Name = name,
            DefaultBranch = empty ? null : "refs/heads/main",
            IsDisabled = disabled,
            Project = new GitRepositoryProject { Id = projectId }
        };

    private static GitCommit Commit(string author, DateTime? date = null) =>
        new()
        {
            CommitId = Guid.NewGuid().ToString(),
            Author = new GitUserDate { Name = author },
            Committer = new GitUserDate { Name = author, Date = date ?? default }
        };

    /// <summary>Alfa com <c>api</c>: primeiro commit de Ana; último de Bruno em 15/09/2026 14:30.</summary>
    private static FakeAzureDevOpsApi AlfaComRepositorioApi()
    {
        var api = ProjetoAlfa();
        api.Repositories = [Repo("pa", "r1", "api")];
        api.CommitsByRepository["r1"] =
        [
            Commit("Bruno", new DateTime(2026, 9, 15, 14, 30, 0)),
            Commit("Carla"),
            Commit("Ana")
        ];
        return api;
    }

    [Fact]
    public async Task Sem_a_flag_nao_ha_chamadas_de_repositorios_nem_de_commits()
    {
        var api = AlfaComRepositorioApi();

        var (html, _) = await GerarAsync(api,
            """ "projects": true, "teams": true, "users": true, "workItems": true """);

        Assert.DoesNotContain(api.Calls, c => c.StartsWith("repositories ") || c.StartsWith("commits "));
        Assert.DoesNotContain("Repositórios", html);
    }

    [Fact]
    public async Task Faz_uma_listagem_e_duas_chamadas_de_commits_por_repositorio_na_ordem_da_listagem()
    {
        var api = ProjetoAlfa();
        api.Repositories = [Repo("pa", "r-web", "web"), Repo("pa", "r-api", "api")];
        api.CommitsByRepository["r-web"] = [Commit("A"), Commit("B")];
        api.CommitsByRepository["r-api"] = [Commit("C"), Commit("D")];

        await GerarAsync(api, """ "repositories": true """);

        var v = ApiFactory.ApiVersion;
        var o = ApiFactory.Organization;
        Assert.Equal(
            [
                $"projects {o} {v}",
                $"repositories {o} {v}",
                $"commits {o} pa r-web 1 - {v}",
                $"commits {o} pa r-web 1 true {v}",
                $"commits {o} pa r-api 1 - {v}",
                $"commits {o} pa r-api 1 true {v}"
            ],
            api.Calls);
    }

    [Fact]
    public async Task Repositorio_vazio_ou_desativado_nao_gera_chamada_de_commits()
    {
        var api = ProjetoAlfa();
        api.Repositories = [Repo("pa", "r1", "vazio", empty: true), Repo("pa", "r2", "velho", disabled: true)];

        await GerarAsync(api, """ "repositories": true """);

        Assert.DoesNotContain(api.Calls, c => c.StartsWith("commits "));
    }

    [Fact]
    public async Task Mostra_criador_ultimo_commit_e_autor_na_linha_do_repositorio()
    {
        var (html, _) = await GerarAsync(AlfaComRepositorioApi(), """ "repositories": true """);

        var trecho = Trecho(html, "1.1 Repositórios");
        Assert.Equal(["Repositório", "Criador", "Último commit", "Autor do último commit"], Cabecalhos(trecho));
        Assert.Equal([["api", "Ana", "15/09/2026 14:30", "Bruno"]], Linhas(trecho));
        Assert.Contains("Criador: autor do primeiro commit do branch padrão.", trecho);
    }

    [Fact]
    public async Task Repositorios_sao_ordenados_por_nome_sem_diferenciar_maiusculas()
    {
        var api = ProjetoAlfa();
        api.Repositories = [Repo("pa", "1", "web", empty: true), Repo("pa", "2", "Api", empty: true), Repo("pa", "3", "banco", empty: true)];

        var (html, _) = await GerarAsync(api, """ "repositories": true """);

        Assert.Equal(["Api", "banco", "web"], Linhas(Trecho(html, "1.1 Repositórios")).Select(l => l[0].Split(' ')[0]));
    }

    [Fact]
    public async Task Repositorio_vazio_ou_desativado_recebe_a_marca_e_traco_nas_demais_colunas()
    {
        var api = ProjetoAlfa();
        api.Repositories = [Repo("pa", "1", "vazio", empty: true), Repo("pa", "2", "velho", disabled: true)];

        var (html, _) = await GerarAsync(api, """ "repositories": true """);

        Assert.Equal(
            [["vazio Vazio", "—", "—", "—"], ["velho Desativado", "—", "—", "—"]],
            Linhas(Trecho(html, "1.1 Repositórios")));
    }

    [Fact]
    public async Task Projeto_sem_repositorios_mostra_a_mensagem_de_vazio()
    {
        var (html, _) = await GerarAsync(ProjetoAlfa(), """ "repositories": true """);

        var trecho = Trecho(html, "1.1 Repositórios");
        Assert.Contains("Nenhum repositório encontrado.", trecho);
        Assert.DoesNotContain("<table", trecho);
    }

    [Fact]
    public async Task Cada_repositorio_aparece_so_no_capitulo_do_seu_projeto()
    {
        var api = ApiComDoisProjetos();
        var alfa = api.Projects!.Single(p => p.Name == "Alfa").Id;
        var beta = api.Projects!.Single(p => p.Name == "Beta").Id;
        api.Repositories = [Repo(beta, "r1", "repo-beta", empty: true), Repo(alfa, "r2", "repo-alfa", empty: true)];

        var (html, _) = await GerarAsync(api, """ "repositories": true """);

        var capituloAlfa = Trecho(html, "1. Alfa", "2. Beta");
        var capituloBeta = Trecho(html, "2. Beta");
        Assert.Contains("repo-alfa", capituloAlfa);
        Assert.DoesNotContain("repo-beta", capituloAlfa);
        Assert.Contains("repo-beta", capituloBeta);
        Assert.DoesNotContain("repo-alfa", capituloBeta);
    }

    [Fact]
    public async Task Numeracao_dos_repositorios_so_e_com_todas_as_flags()
    {
        var (soRepositorios, _) = await GerarAsync(AlfaComRepositorioApi(), """ "repositories": true """);
        Assert.Equal([(1, "1. Alfa"), (2, "1.1 Repositórios")], Titulos(soRepositorios));

        var api = AlfaComRepositorioApi();
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf, AnalyticsDeAlfa());
        var response = await factory.CreateClient().PostAsync(ReportHost.Route, ReportHost.Json(
            """{ "include": { "projects": true, "teams": true, "users": true, "workItems": true, "repositories": true } }"""));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var titulos = Titulos(WebUtility.HtmlDecode(pdf.Html!));
        Assert.Equal((2, "3.5 Repositórios"), titulos[^1]);
        Assert.Equal((2, "3.4 Itens de trabalho"), titulos[^2]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Falha_na_listagem_ou_nos_commits_interrompe_o_relatorio_sem_gerar_pdf(bool naListagem)
    {
        var api = AlfaComRepositorioApi();
        var erro = new InvalidOperationException("falha simulada");
        if (naListagem) api.RepositoriesError = erro; else api.CommitsError = erro;
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json("""{ "include": { "repositories": true } }"""));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(pdf.Html);
    }

    private static string[] Celulas(string linha) =>
        System.Text.RegularExpressions.Regex.Matches(linha, "<td[^>]*>(.*?)</td>", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(m => System.Text.RegularExpressions.Regex.Replace(
                System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "<.*?>", ""), "\\s+", " ").Trim())
            .ToArray();

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

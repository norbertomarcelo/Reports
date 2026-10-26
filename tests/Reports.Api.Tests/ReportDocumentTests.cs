using System.Net;
using System.Text.RegularExpressions;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Tests;
using Reports.AzureDevOps.Tests.Fakes;
using Reports.Api.Tests.Fakes;

namespace Reports.Api.Tests;

/// <summary>Spec 020: o relatório como documento do design system (capa, visão geral e visual dos capítulos).</summary>
public class ReportDocumentTests
{
    private const string Todas = """ "projects": true, "teams": true, "users": true, "workItems": true, "repositories": true """;
    private static readonly Dictionary<string, string?> Contoso = new() { ["AzureDevOps:Organization"] = "contoso" };

    private static Identity Pessoa(string id, string nome, string email) =>
        new() { Id = id, DisplayName = nome, UniqueName = email };

    private static GitRepository Repo(string projectId, string id, string nome, bool vazio = false, bool desativado = false) =>
        new()
        {
            Id = id, Name = nome, DefaultBranch = vazio ? null : "refs/heads/main", IsDisabled = desativado,
            Project = new GitRepositoryProject { Id = projectId }
        };

    private static WorkItemGroup Grupo(string tipo, string categoria, string area, long total) =>
        new() { WorkItemType = tipo, StateCategory = categoria, Area = new WorkItemArea { AreaPath = area }, Count = total };

    /// <summary>
    /// Alfa e Beta. Ana está nos três times (conta uma vez); Carlos (gmail) é externo; "Time B" e "Time C" não têm
    /// administrador. Itens: 6 abertos e 1 fechado em Alfa; Beta devolve mais de uma página. Repositórios: um com
    /// commits, um vazio e um desativado.
    /// </summary>
    private static (FakeAzureDevOpsApi Api, FakeAzureDevOpsAnalyticsApi Analytics) Cenario()
    {
        var ana = Pessoa("u1", "Ana", "ana@empresa.com.br");
        var carlos = Pessoa("u2", "Carlos", "carlos@gmail.com");
        var dani = Pessoa("u3", "Dani", "dani@empresa.com.br");

        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "pa", Name = "Alfa" }, new Project { Id = "pb", Name = "Beta" }]
        };
        api.TeamsByProject["pa"] = [new Team { Id = "t1", Name = "Time A" }, new Team { Id = "t2", Name = "Time B" }];
        api.TeamsByProject["pb"] = [new Team { Id = "t3", Name = "Time C" }];
        api.MembersByTeam["t1"] = [new TeamMember { IsTeamAdmin = true, Identity = ana }, new TeamMember { Identity = carlos }];
        api.MembersByTeam["t2"] = [new TeamMember { Identity = ana }];
        api.MembersByTeam["t3"] = [new TeamMember { Identity = ana }, new TeamMember { Identity = dani }];
        api.Repositories = [Repo("pa", "r1", "api"), Repo("pa", "r2", "vazio", vazio: true), Repo("pb", "r3", "antigo", desativado: true)];
        api.CommitsByRepository["r1"] =
        [
            new GitCommit { CommitId = "c2", Author = new GitUserDate { Name = "Bruno" }, Committer = new GitUserDate { Name = "Bruno", Date = new DateTime(2026, 9, 15, 14, 30, 0) } },
            new GitCommit { CommitId = "c1", Author = new GitUserDate { Name = "Ana" }, Committer = new GitUserDate { Name = "Ana" } }
        ];

        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] =
        [
            Grupo("Bug", "InProgress", "Alfa\\Backend", 2), Grupo("Bug", "Completed", "Alfa\\Backend", 1),
            Grupo("Task", "Proposed", "Alfa\\Frontend", 3), Grupo("Task", "Resolved", "Alfa\\Frontend", 1)
        ];
        analytics.ProjectsWithNextPage.Add("Beta");
        return (api, analytics);
    }

    private static async Task<string> GerarAsync(
        FakeAzureDevOpsApi api, FakeAzureDevOpsAnalyticsApi analytics, string include,
        IReadOnlyDictionary<string, string?>? settings = null, params string[] internalDomains)
    {
        var pdf = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, pdf, analytics, settings ?? new Dictionary<string, string?>(), internalDomains);

        var response = await factory.CreateClient().PostAsync(ReportHost.Route,
            ReportHost.Json($$"""{ "include": { {{include}} } }"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return WebUtility.HtmlDecode(pdf.Html!);
    }

    private static Task<string> GerarCenarioAsync(string include = Todas, params string[] internalDomains)
    {
        var (api, analytics) = Cenario();
        return GerarAsync(api, analytics, include, Contoso, internalDomains);
    }

    private static string Trecho(string html, string de, string ate)
    {
        var inicio = html.IndexOf(de, StringComparison.Ordinal);
        Assert.True(inicio >= 0, de);
        var fim = html.IndexOf(ate, inicio, StringComparison.Ordinal);
        Assert.True(fim >= 0, ate);
        return html[inicio..fim];
    }

    private static string Capa(string html) => Trecho(html, "<article class=\"ra-page ra-page--cover\">", "</article>");
    private static string Visao(string html) => Trecho(html, "<section class=\"ra-overview\">", "<main class=\"ra-chapters\">");
    private static string Capitulos(string html) => Trecho(html, "<main class=\"ra-chapters\">", "</main>");

    private static List<string> Textos(string html, string classe) =>
        Regex.Matches(html, $"class=\"{classe}\">(?<t>.*?)</", RegexOptions.Singleline).Select(m => m.Groups["t"].Value.Trim()).ToList();

    private static string[] Alertas(string html) =>
        Regex.Matches(Visao(html), "<b>(?<t>.*?)</b>").Select(m => m.Groups["t"].Value).ToArray();

    // ---------- Documento (RF-03, RF-04) ----------

    [Fact]
    public async Task E_um_documento_completo_em_portugues_com_tema_claro()
    {
        var html = await GerarCenarioAsync();

        Assert.StartsWith("<!doctype html>", html);
        Assert.Contains("<html lang=\"pt-BR\" data-theme=\"light\">", html);
        Assert.Contains("<title>AD-20261026-1542 — contoso</title>", html);
    }

    [Fact]
    public async Task E_autocontido_sem_script_link_nem_recurso_externo()
    {
        var html = await GerarCenarioAsync();

        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<link", html);
        Assert.DoesNotContain("@import", html);
        var alvos = Regex.Matches(html, "url\\(\\s*[\"']?(?<alvo>[^\"')]*)").Select(m => m.Groups["alvo"].Value).ToList();
        Assert.NotEmpty(alvos);
        Assert.All(alvos, alvo => Assert.StartsWith("data:", alvo));
        Assert.DoesNotMatch("(?:src|href)=\"(?!#)", html);
        Assert.DoesNotMatch("https?://(?!www\\.w3\\.org)", html);
    }

    [Fact]
    public async Task Embute_as_oito_fontes_do_design_system()
    {
        var html = await GerarCenarioAsync();

        Assert.Equal(8, Regex.Matches(html, "url\\(\"data:font/woff2;base64,").Count);
        foreach (var familia in new[] { "Sora", "IBM Plex Sans", "IBM Plex Mono" })
            Assert.Contains($"font-family: \"{familia}\"", html);
    }

    [Fact]
    public async Task A_ordem_e_capa_visao_geral_e_capitulos()
    {
        var html = await GerarCenarioAsync();

        var posicoes = new[]
        {
            html.IndexOf("<article class=\"ra-page ra-page--cover\">", StringComparison.Ordinal),
            html.IndexOf("<section class=\"ra-overview\">", StringComparison.Ordinal),
            html.IndexOf("<main class=\"ra-chapters\">", StringComparison.Ordinal)
        };
        Assert.DoesNotContain(-1, posicoes);
        Assert.Equal(posicoes.Order(), posicoes);
        Assert.Equal(1, Regex.Matches(html, "<main class=\"ra-chapters\">").Count);
    }

    [Fact]
    public async Task Os_textos_do_cabecalho_e_do_rodape_vem_antes_do_css_do_design_system()
    {
        // No Chromium, o @page :first (capa sem cabeçalho) só vence se vier depois destes textos.
        var html = await GerarCenarioAsync();

        var texto = html.IndexOf("counter(page)", StringComparison.Ordinal);
        var primeiroFlow = html.IndexOf("@page :first", StringComparison.Ordinal);
        Assert.True(texto > 0 && primeiroFlow > texto);
        Assert.Contains("content: \"AD-20261026-1542 · \" counter(page) \"/\" counter(pages);", html);
        Assert.Contains("@bottom-left { content: \"Relatórios de Ambientes · Azure DevOps\"; }", html);
        Assert.Contains("@bottom-right { content: \"AD-20261026-1542\"; }", html);
    }

    [Fact]
    public async Task O_nome_da_organizacao_nunca_vai_para_dentro_do_css()
    {
        var (api, analytics) = Cenario();
        var html = await GerarAsync(api, analytics, """ "projects": true """,
            new Dictionary<string, string?> { ["AzureDevOps:Organization"] = "evil\"; } body { display:none } /*" });

        var estilo = Trecho(html, "<style>@page", "</style>");
        Assert.DoesNotContain("evil", estilo);
        Assert.DoesNotContain("display:none", estilo);
    }

    // ---------- Capa (RF-06, RF-07) ----------

    [Fact]
    public async Task Capa_traz_codigo_organizacao_secoes_e_hora_da_geracao()
    {
        var html = await GerarCenarioAsync(""" "projects": true, "teams": true, "users": true """);
        var capa = Capa(html);

        Assert.Contains("<span class=\"ra-page__code\">AD-20261026-1542</span>", capa);
        Assert.Contains("<h1 class=\"ra-cover__title\">contoso</h1>", capa);
        Assert.Contains("<p class=\"ra-cover__subtitle\">Projetos, times e usuários</p>", capa);
        Assert.Contains("Azure DevOps · contoso", capa);
        Assert.Equal(["26/10/2026 15:42 UTC", "2", "3 de 5"], Textos(capa, "ra-cover__fact-value"));
    }

    [Fact]
    public async Task Capa_lista_todas_as_secoes_quando_todas_foram_pedidas()
    {
        var capa = Capa(await GerarCenarioAsync());

        Assert.Contains("Projetos, times, usuários, itens de trabalho e repositórios", capa);
        Assert.Contains("5 de 5", capa);
    }

    private static string EstadoGeral(string html) =>
        Regex.Match(Capa(html), "Estado geral</span><span><span class=\"[^\"]*\">(?<estado>[^<]+)</span>").Groups["estado"].Value;

    [Fact]
    public async Task Estado_geral_e_atencao_com_time_sem_administrador()
    {
        Assert.Equal("Atenção", EstadoGeral(await GerarCenarioAsync(""" "teams": true, "users": true """)));
    }

    [Fact]
    public async Task Estado_geral_e_saudavel_so_com_projetos()
    {
        Assert.Equal("Saudável", EstadoGeral(await GerarCenarioAsync(""" "projects": true """)));
    }

    [Fact]
    public async Task Estado_geral_e_sem_dados_quando_a_organizacao_nao_tem_projetos()
    {
        var html = await GerarAsync(new FakeAzureDevOpsApi(), new FakeAzureDevOpsAnalyticsApi(), """ "projects": true """);

        Assert.Equal("Sem dados", EstadoGeral(html));
    }

    // ---------- Visão geral (RF-08 a RF-10) ----------

    [Fact]
    public async Task So_projetos_tem_um_unico_kpi_e_nenhum_alerta()
    {
        var visao = Visao(await GerarCenarioAsync(""" "projects": true """));

        Assert.Equal(["Projetos"], Textos(visao, "ra-kpi__label"));
        Assert.Equal(["2"], Textos(visao, "ra-kpi__value"));
        Assert.Contains("Nenhum ponto de atenção.", visao);
        Assert.DoesNotContain("ra-alert ", visao);
    }

    [Fact]
    public async Task Com_as_cinco_secoes_os_kpis_vem_na_ordem_com_os_valores_do_cenario()
    {
        var visao = Visao(await GerarCenarioAsync(Todas, "empresa.com.br"));

        Assert.Equal(["Projetos", "Times", "Usuários", "Itens abertos", "Repositórios"], Textos(visao, "ra-kpi__label"));
        Assert.Equal(["2", "3", "3", "6", "3"], Textos(visao, "ra-kpi__value"));
        Assert.Equal(
            ["2 sem administrador", "1 externo", "1 fechado", "1 vazio · 1 desativado"],
            Textos(visao, "ra-kpi__delta"));
    }

    [Fact]
    public async Task A_mesma_pessoa_em_varios_times_conta_uma_vez()
    {
        // Ana está em Time A, Time B e Time C (dois projetos): são 4 vagas, mas 3 pessoas.
        var visao = Visao(await GerarCenarioAsync(""" "users": true """));

        Assert.Equal(["Projetos", "Times", "Usuários"], Textos(visao, "ra-kpi__label"));
        Assert.Equal("3", Textos(visao, "ra-kpi__value")[2]);
    }

    [Fact]
    public async Task Sem_InternalDomains_nao_ha_complemento_de_externos_nem_o_alerta()
    {
        var visao = Visao(await GerarCenarioAsync(""" "users": true """));

        Assert.Empty(Textos(visao, "ra-kpi__delta"));
        Assert.DoesNotContain("externo", visao);
    }

    [Fact]
    public async Task Sem_teams_nao_ha_alerta_de_time_sem_administrador()
    {
        var visao = Visao(await GerarCenarioAsync(""" "users": true """));

        Assert.DoesNotContain("sem administrador", visao);
    }

    [Fact]
    public async Task As_quatro_condicoes_geram_os_quatro_alertas_na_ordem()
    {
        var html = await GerarCenarioAsync(Todas, "empresa.com.br");

        Assert.Equal(
            [
                "2 times sem administrador.",
                "Contagem de itens parcial em 1 projeto.",
                "1 usuário externo.",
                "2 repositórios vazios ou desativados."
            ],
            Alertas(html));
        Assert.Equal(["Atenção", "Atenção", "Info", "Info"], Textos(Visao(html), "ra-badge ra-badge--(?:warn|brand)"));
    }

    [Fact]
    public async Task Um_unico_time_sem_administrador_fica_no_singular()
    {
        var (api, analytics) = Cenario();
        api.MembersByTeam["t2"] = [new TeamMember { IsTeamAdmin = true, Identity = Pessoa("u1", "Ana", "ana@empresa.com.br") }];

        var html = await GerarAsync(api, analytics, """ "teams": true, "users": true """, Contoso);

        Assert.Equal(["1 time sem administrador."], Alertas(html));
    }

    [Fact]
    public async Task Capa_e_visao_geral_nao_fazem_chamadas_novas_ao_azure_devops()
    {
        var (api, analytics) = Cenario();

        await GerarAsync(api, analytics, Todas, Contoso);

        Assert.Equal(1, api.Calls.Count(c => c.StartsWith("projects ")));
        Assert.Equal(2, api.Calls.Count(c => c.StartsWith("properties ")));
        Assert.Equal(2, api.Calls.Count(c => c.StartsWith("teams ")));
        Assert.Equal(3, api.Calls.Count(c => c.StartsWith("members ")));
        Assert.Equal(1, api.Calls.Count(c => c.StartsWith("repositories ")));
        Assert.Equal(2, api.Calls.Count(c => c.StartsWith("commits ")));   // só o repositório com branch padrão e ativo
        Assert.Equal(2, analytics.Calls.Count);                             // uma por projeto
        Assert.Equal(api.Calls.Count, 1 + 2 + 2 + 3 + 1 + 2);
    }

    // ---------- Capítulos (RF-11, RF-12) ----------

    [Fact]
    public async Task Toda_tabela_usa_o_componente_do_design_system()
    {
        var capitulos = Capitulos(await GerarCenarioAsync());

        var tabelas = Regex.Matches(capitulos, "<table[^>]*>").Select(m => m.Value).ToList();
        Assert.NotEmpty(tabelas);
        Assert.All(tabelas, t => Assert.Equal("<table class=\"ra-table ra-table--ad\">", t));
        Assert.Equal(tabelas.Count, Regex.Matches(capitulos, "<div class=\"ra-table-wrap\">\\s*<table").Count);
    }

    [Fact]
    public async Task Nenhuma_classe_antiga_do_bootstrap_ou_do_estilo_local_sobrou()
    {
        var capitulos = Capitulos(await GerarCenarioAsync(Todas, "empresa.com.br"));

        foreach (var antiga in new[] { "table-striped", "table-bordered", "thead-dark", "class=\"numero\"", "class=\"marca\"", "class=\"alerta\"", "class=\"rotulo\"", "class=\"total\"", "projetos" })
            Assert.DoesNotContain(antiga, capitulos);
        Assert.DoesNotContain("<style", capitulos);
        Assert.DoesNotContain("style=\"", capitulos);
    }

    [Fact]
    public async Task Sem_administrador_e_um_badge_de_atencao()
    {
        var capitulos = Capitulos(await GerarCenarioAsync(""" "teams": true, "users": true """));

        Assert.Equal(2, Regex.Matches(capitulos, "<span class=\"ra-badge ra-badge--warn\">Sem administrador</span>").Count);
    }

    [Fact]
    public async Task Externo_e_grupo_sao_badges()
    {
        var (api, analytics) = Cenario();
        api.MembersByTeam["t2"] = [new TeamMember { Identity = new Identity { Id = "g1", DisplayName = "Devs", UniqueName = "[Alfa]\\Devs" } }];

        var capitulos = Capitulos(await GerarAsync(api, analytics, """ "users": true """, Contoso, "empresa.com.br"));

        Assert.Contains("<span class=\"ra-badge\">Externo</span>", capitulos);
        Assert.Contains("<span class=\"ra-badge\">Grupo</span>", capitulos);
    }

    [Fact]
    public async Task Repositorio_vazio_e_desativado_sao_badges_e_o_nome_fica_em_mono()
    {
        var capitulos = Capitulos(await GerarCenarioAsync(""" "repositories": true """));

        Assert.Contains("<span class=\"ra-badge\">Vazio</span>", capitulos);
        Assert.Contains("<span class=\"ra-badge\">Desativado</span>", capitulos);
        Assert.Matches("<td class=\"ra-id\">\\s*api\\s*</td>", capitulos);
    }

    [Fact]
    public async Task Contagem_parcial_e_um_alerta_de_atencao_no_capitulo()
    {
        var capitulos = Capitulos(await GerarCenarioAsync(""" "workItems": true """));

        Assert.Matches("<div class=\"ra-alert ra-alert--warn\">\\s*<span class=\"ra-badge ra-badge--warn\">Atenção</span>\\s*<p><b>Contagem parcial", capitulos);
    }

    [Fact]
    public async Task Numeros_e_datas_saem_em_mono_a_direita_e_em_pt_br()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "pa", Name = "Alfa", LastUpdateTime = new DateTime(2026, 9, 15, 14, 30, 0) }] };
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] = [Grupo("Bug", "InProgress", "Alfa\\Backend", 1204)];

        var capitulos = Capitulos(await GerarAsync(api, analytics, """ "projects": true, "workItems": true """));

        Assert.Contains("<td class=\"ra-num\">1.204</td>", capitulos);
        Assert.Contains("<td class=\"ra-num\">15/09/2026 14:30</td>", capitulos);
        Assert.DoesNotContain(">1204<", capitulos);
        Assert.Contains("<th class=\"ra-num\">Abertos</th>", capitulos);
    }

    [Fact]
    public void As_views_do_azure_devops_nao_tem_style_nem_atributo_style()
    {
        // RF-02: o visual vem só do design system.
        var pasta = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ArquivoDestePath())!,
            "..", "..", "src", "Reports.AzureDevOps", "Views"));
        var views = Directory.GetFiles(pasta, "*.cshtml", SearchOption.AllDirectories);

        Assert.NotEmpty(views);
        foreach (var view in views)
        {
            var texto = File.ReadAllText(view);
            Assert.False(texto.Contains("<style", StringComparison.OrdinalIgnoreCase), $"{view} tem <style>");
            Assert.False(Regex.IsMatch(texto, "\\sstyle\\s*="), $"{view} tem o atributo style");
        }
    }

    private static string ArquivoDestePath([System.Runtime.CompilerServices.CallerFilePath] string arquivo = "") => arquivo;
}

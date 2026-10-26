using System.Net;
using System.Text.RegularExpressions;
using Reports.Api.Services.Pdf;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Tests.Fakes;
using Reports.Api.Tests.Fakes;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Reports.Api.Tests;

/// <summary>
/// Spec 020, RF-05 e RF-14: o relatório do design system, de ponta a ponta, convertido pelo Chromium de verdade.
/// Com <c>REPORTS_PDF_DIR</c> definida, o HTML e o PDF ficam nessa pasta para a conferência visual (DESIGN.md, seção 8).
/// </summary>
[Trait("Category", "Integration")]
public class ReportPdfTests
{
    private const int Pessoas = 60;
    private const string Codigo = "AD-20261026-1542";

    /// <summary>Um projeto, um time com 60 pessoas e as cinco seções: o relatório passa de uma folha.</summary>
    private static (FakeAzureDevOpsApi Api, FakeAzureDevOpsAnalyticsApi Analytics) CenarioLongo()
    {
        var api = new FakeAzureDevOpsApi { Projects = [new Project { Id = "pa", Name = "Alfa" }] };
        api.TeamsByProject["pa"] = [new Team { Id = "t1", Name = "Time A", Description = "Time de teste com uma descrição razoavelmente longa" }];
        api.MembersByTeam["t1"] = Enumerable.Range(1, Pessoas).Select(i => new TeamMember
        {
            IsTeamAdmin = i == 1,
            Identity = new Identity { Id = $"u{i}", DisplayName = $"Pessoa {i:00}", UniqueName = $"pessoa{i:00}@empresa.com.br" }
        }).ToList();
        api.Repositories =
        [
            new GitRepository
            {
                Id = "r1", Name = "api", DefaultBranch = "refs/heads/main",
                Project = new GitRepositoryProject { Id = "pa" }
            }
        ];
        api.CommitsByRepository["r1"] =
        [
            new GitCommit { CommitId = "c1", Author = new GitUserDate { Name = "Bruno" }, Committer = new GitUserDate { Name = "Carla", Date = new DateTime(2026, 9, 15, 14, 30, 0) } }
        ];
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] =
        [
            new WorkItemGroup { WorkItemType = "Bug", StateCategory = "InProgress", Area = new WorkItemArea { AreaPath = "Alfa\\Backend" }, Count = 1204 }
        ];
        return (api, analytics);
    }

    /// <summary>Times sem administrador, contagem parcial, usuário externo e repositórios vazio e desativado: os quatro alertas.</summary>
    private static (FakeAzureDevOpsApi Api, FakeAzureDevOpsAnalyticsApi Analytics) CenarioComAlertas()
    {
        var api = new FakeAzureDevOpsApi
        {
            Projects = [new Project { Id = "pa", Name = "Alfa" }, new Project { Id = "pb", Name = "Beta" }]
        };
        api.TeamsByProject["pa"] = [new Team { Id = "t1", Name = "Time A" }, new Team { Id = "t2", Name = "Time B" }];
        api.TeamsByProject["pb"] = [new Team { Id = "t3", Name = "Time C" }];
        api.MembersByTeam["t1"] = [new TeamMember { IsTeamAdmin = true, Identity = new Identity { Id = "u1", DisplayName = "Ana", UniqueName = "ana@empresa.com.br" } }];
        api.MembersByTeam["t2"] = [new TeamMember { Identity = new Identity { Id = "u2", DisplayName = "Carlos", UniqueName = "carlos@gmail.com" } }];
        api.MembersByTeam["t3"] = [new TeamMember { Identity = new Identity { Id = "u1", DisplayName = "Ana", UniqueName = "ana@empresa.com.br" } }];
        api.Repositories =
        [
            new GitRepository { Id = "r1", Name = "vazio", DefaultBranch = null, Project = new GitRepositoryProject { Id = "pa" } },
            new GitRepository { Id = "r2", Name = "antigo", DefaultBranch = "refs/heads/main", IsDisabled = true, Project = new GitRepositoryProject { Id = "pb" } }
        ];
        var analytics = new FakeAzureDevOpsAnalyticsApi();
        analytics.GroupsByProject["Alfa"] =
        [
            new WorkItemGroup { WorkItemType = "Bug", StateCategory = "InProgress", Area = new WorkItemArea { AreaPath = "Alfa\\Backend" }, Count = 2 }
        ];
        analytics.ProjectsWithNextPage.Add("Beta");
        return (api, analytics);
    }

    private static Task<(string Html, byte[] Pdf)> GerarAsync() => GerarAsync(CenarioLongo(), "relatorio");

    private static async Task<(string Html, byte[] Pdf)> GerarAsync(
        (FakeAzureDevOpsApi Api, FakeAzureDevOpsAnalyticsApi Analytics) cenario, string arquivo, params string[] internalDomains)
    {
        var (api, analytics) = cenario;
        var capturado = new FakePdfRenderer();
        using var factory = ReportHost.Create(api, capturado, analytics,
            new Dictionary<string, string?> { ["AzureDevOps:Organization"] = "contoso" }, internalDomains);
        var response = await factory.CreateClient().PostAsync(ReportHost.Route, ReportHost.Json(
            """{ "include": { "projects": true, "teams": true, "users": true, "workItems": true, "repositories": true } }"""));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var renderer = new PlaywrightPdfRenderer();
        var pdf = await renderer.RenderAsync(capturado.Html!);

        var pasta = Environment.GetEnvironmentVariable("REPORTS_PDF_DIR");
        if (!string.IsNullOrEmpty(pasta))
        {
            Directory.CreateDirectory(pasta);
            await File.WriteAllTextAsync(Path.Combine(pasta, arquivo + ".html"), capturado.Html!);
            await File.WriteAllBytesAsync(Path.Combine(pasta, arquivo + ".pdf"), pdf);
        }

        return (capturado.Html!, pdf);
    }

    private static string Texto(Page pagina) => string.Join(" ", pagina.GetWords().Select(w => w.Text));

    [Fact]
    public async Task Todas_as_folhas_sao_a4_a_capa_nao_tem_cabecalho_e_as_demais_numeram_n_de_total()
    {
        var (_, pdf) = await GerarAsync();

        using var documento = PdfDocument.Open(pdf);
        var paginas = documento.GetPages().ToList();
        Assert.True(paginas.Count > 3, $"o relatório de {Pessoas} pessoas devia passar de 3 folhas, tem {paginas.Count}");

        Assert.All(paginas, p =>
        {
            Assert.InRange(p.Width, 594, 596);     // 210 mm
            Assert.InRange(p.Height, 841, 843);    // 297 mm
        });

        // Capa: o código aparece sozinho, sem o "· n/total" do cabeçalho das folhas internas.
        var capa = Texto(paginas[0]);
        Assert.Contains("contoso", capa);
        Assert.Contains(Codigo, capa);
        Assert.DoesNotMatch(Regex.Escape(Codigo) + " · \\d+/\\d+", capa);
        Assert.DoesNotContain("Azure DevOps · contoso Relatórios de Ambientes · Azure DevOps", capa);

        for (var n = 2; n <= paginas.Count; n++)
        {
            var texto = Texto(paginas[n - 1]);
            Assert.Contains($"{Codigo} · {n}/{paginas.Count}", texto);
            Assert.Contains("Relatórios de Ambientes · Azure DevOps", texto);
        }
    }

    [Fact]
    public async Task Visao_geral_mostra_os_quatro_alertas_e_a_capa_mostra_atencao()
    {
        var (_, pdf) = await GerarAsync(CenarioComAlertas(), "relatorio-alertas", "empresa.com.br");

        using var documento = PdfDocument.Open(pdf);
        var capa = Texto(documento.GetPage(1));
        var visaoGeral = Texto(documento.GetPage(2));

        Assert.Contains("Atenção", capa);
        Assert.DoesNotContain("Saudável", capa);
        Assert.Contains("2 times sem administrador.", visaoGeral);
        Assert.Contains("Contagem de itens parcial em 1 projeto.", visaoGeral);
        Assert.Contains("1 usuário externo.", visaoGeral);
        Assert.Contains("2 repositórios vazios ou desativados.", visaoGeral);
        Assert.DoesNotContain("Nenhum ponto de atenção.", visaoGeral);
    }

    [Fact]
    public async Task Nada_e_mais_largo_que_a_folha_e_o_chromium_nao_encolhe_o_documento()
    {
        // Se algum conteúdo passar da largura útil (ex.: cabeçalho de tabela sem quebra de linha), o Chromium
        // reduz o DOCUMENTO INTEIRO para caber. A capa é o sinal mais claro: o título "contoso" tem ~150pt de
        // largura em tamanho natural (48px, ~149,8pt) e cai para ~109pt quando há encolhimento; até 4% já se vê
        // na capa, que deixa de preencher a folha.
        var (_, pdf) = await GerarAsync();

        using var documento = PdfDocument.Open(pdf);
        var titulo = documento.GetPage(1).GetWords().Where(w => w.Text == "contoso").MaxBy(w => w.BoundingBox.Width)!;

        Assert.InRange(titulo.BoundingBox.Width, 148, 152);   // o outro "contoso" da capa é o chip, bem menor
    }

    [Fact]
    public async Task Tabela_longa_nao_perde_linhas_e_repete_o_cabecalho_em_cada_folha()
    {
        var (_, pdf) = await GerarAsync();

        using var documento = PdfDocument.Open(pdf);
        var textos = documento.GetPages().Select(Texto).ToList();

        var todo = string.Join(" ", textos);
        for (var i = 1; i <= Pessoas; i++)
            Assert.Contains($"Pessoa {i:00}", todo);

        var comLinhas = textos.Where(t => Regex.IsMatch(t, "Pessoa \\d\\d")).ToList();
        Assert.True(comLinhas.Count >= 2, "a tabela devia passar de uma folha");
        Assert.All(comLinhas, t => Assert.Matches("\\bNOME\\b", t));
    }

    [Fact]
    public async Task Embute_as_fontes_do_design_system()
    {
        var (_, pdf) = await GerarAsync();

        // O Chromium grava as fontes como Type 3, e o PdfPig só devolve "Type3" como nome das letras;
        // o nome real está no descritor da fonte: /FontName /ABCDEF+Sora-SemiBold.
        var descritores = Regex.Matches(System.Text.Encoding.Latin1.GetString(pdf), "/FontName\\s*/[A-Z]{6}\\+(?<nome>[A-Za-z0-9-]+)")
            .Select(m => m.Groups["nome"].Value).Distinct().ToList();

        Assert.Contains("Sora-SemiBold", descritores);
        Assert.Contains("IBMPlexSans-Regular", descritores);
        Assert.Contains("IBMPlexSans-SemiBold", descritores);
        Assert.Contains("IBMPlexMono-Regular", descritores);
    }
}

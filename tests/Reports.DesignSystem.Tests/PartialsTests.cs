using System.Text.RegularExpressions;
using Razor.Templating.Core;

namespace Reports.DesignSystem.Tests;

public class PartialsTests
{
    private static readonly ReportCode Code =
        ReportCode.Create(Provider.AzureDevOps, new DateTimeOffset(2026, 10, 26, 15, 42, 0, TimeSpan.Zero));

    private static ReportDocument Document(
        string title = "contoso",
        IReadOnlyList<SourceVm>? sources = null,
        IReadOnlyList<KpiVm>? kpis = null,
        IReadOnlyList<AlertVm>? alerts = null,
        Status status = Status.Warn) =>
        new(Code, title, "Projetos, times e usuários",
            sources ?? [new SourceVm(Provider.AzureDevOps, "contoso")],
            [new ReportFact("Gerado em", "26/10/2026 15:42 UTC"), new ReportFact("Projetos", "12"), new ReportFact("Seções", "3 de 5")],
            status,
            kpis ?? [new KpiVm("Projetos", "12")],
            alerts ?? []);

    /// <summary>
    /// O Razor escreve acentos como entidades (<c>&amp;#xE1;</c>), que o navegador decodifica. Aqui só os caracteres
    /// fora do ASCII são decodificados: <c>&amp;lt;</c> e companhia ficam, para o teste de escape.
    /// </summary>
    private static async Task<string> RenderAsync(string view, object model, Dictionary<string, object?>? viewData = null)
    {
        var html = await RazorTemplateEngine.RenderPartialAsync("/Views/DesignSystem/" + view + ".cshtml", model, viewData);
        return Regex.Replace(html, "&#x(?<codigo>[0-9A-Fa-f]+);", m =>
        {
            var codigo = int.Parse(m.Groups["codigo"].Value, NumberStyles.HexNumber);
            return codigo > 127 ? char.ConvertFromUtf32(codigo) : m.Value;
        });
    }

    private static string Texto(string html) =>
        Regex.Replace(Regex.Replace(System.Net.WebUtility.HtmlDecode(html), "<.*?>", " "), "\\s+", " ").Trim();

    // ---------- Capa ----------

    [Fact]
    public async Task Capa_mostra_codigo_titulo_subtitulo_e_eyebrow()
    {
        var html = await RenderAsync("_Cover", Document());

        Assert.Contains("""<span class="ra-page__code">AD-20261026-1542</span>""", html);
        Assert.Contains("""<h1 class="ra-cover__title">contoso</h1>""", html);
        Assert.Contains("""<p class="ra-cover__subtitle">Projetos, times e usuários</p>""", html);
        Assert.Contains("""<p class="ra-cover__eyebrow">Relatório de ambientes</p>""", html);
    }

    [Fact]
    public async Task Capa_e_uma_folha_fixa_sem_cabecalho_nem_rodape_de_folha_interna()
    {
        var html = await RenderAsync("_Cover", Document());

        Assert.Contains("""<article class="ra-page ra-page--cover">""", html);
        Assert.DoesNotContain("ra-page__header", html);
        Assert.DoesNotContain("ra-page__footer", html);
    }

    [Fact]
    public async Task Capa_mostra_o_chip_da_fonte_com_a_organizacao()
    {
        var html = await RenderAsync("_Cover", Document());

        var chip = Regex.Match(html, "<span class=\"ra-source ra-source--on-grad\".*?</span>[^<]*</span>", RegexOptions.Singleline).Value;
        Assert.Contains("""title="Azure DevOps · contoso" """.TrimEnd(), chip);
        Assert.Contains("""<span class="ra-source__mark">AD</span>""", chip);
        Assert.Equal("AD Azure DevOps · contoso", Texto(chip));
    }

    [Fact]
    public async Task Capa_mostra_os_tres_fatos_e_o_estado_geral_como_badge()
    {
        var html = await RenderAsync("_Cover", Document());

        var fatos = Regex.Matches(html, "<span class=\"ra-label\">(?<rotulo>.*?)</span><span class=\"ra-cover__fact-value\">(?<valor>.*?)</span>")
            .Select(m => (m.Groups["rotulo"].Value, m.Groups["valor"].Value)).ToList();
        Assert.Equal(
            [("Gerado em", "26/10/2026 15:42 UTC"), ("Projetos", "12"), ("Seções", "3 de 5")],
            fatos);
        Assert.Matches("Estado geral</span><span><span class=\"ra-badge ra-badge--warn\">Atenção</span>", html);
    }

    [Theory]
    [InlineData(Status.Ok, "ra-badge ra-badge--ok", "Saudável")]
    [InlineData(Status.Neutral, "ra-badge", "Sem dados")]
    public async Task Capa_mostra_o_estado_geral_com_texto(Status status, string classe, string rotulo)
    {
        var html = await RenderAsync("_Cover", Document(status: status));

        Assert.Contains($"<span class=\"{classe}\">{rotulo}</span>", html);
    }

    [Fact]
    public async Task Ilustracao_da_capa_so_tem_as_fontes_do_relatorio()
    {
        var html = await RenderAsync("_Cover", Document());
        var arte = Regex.Match(html, "<div class=\"ra-cover__art\".*?</div>", RegexOptions.Singleline).Value;

        Assert.Equal(["AD"], Regex.Matches(arte, "<text[^>]*>(?<sigla>[A-Z]{2})</text>").Select(m => m.Groups["sigla"].Value));
    }

    [Fact]
    public async Task Ilustracao_da_capa_acompanha_varias_fontes()
    {
        var html = await RenderAsync("_Cover", Document(sources:
            [new SourceVm(Provider.AzureDevOps, "contoso"), new SourceVm(Provider.GitHub, "norberto")]));
        var arte = Regex.Match(html, "<div class=\"ra-cover__art\".*?</div>", RegexOptions.Singleline).Value;

        Assert.Equal(["AD", "GH"], Regex.Matches(arte, "<text[^>]*>(?<sigla>[A-Z]{2})</text>").Select(m => m.Groups["sigla"].Value));
        Assert.Equal(2, Regex.Matches(html, "ra-source--on-grad\"").Count);
    }

    [Fact]
    public async Task Capa_escapa_o_html_do_titulo()
    {
        var html = await RenderAsync("_Cover", Document(title: "<script>alert(1)</script>"));

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    // ---------- KPI ----------

    [Fact]
    public async Task Kpi_mostra_rotulo_valor_e_complemento()
    {
        var html = await RenderAsync("_Kpi", new KpiVm("Times", "1.204", "2 sem administrador", KpiTone.Success));

        Assert.Contains("""<div class="ra-kpi ra-kpi--success">""", html);
        Assert.Contains("""<span class="ra-kpi__label">Times</span>""", html);
        Assert.Contains("""<span class="ra-kpi__value">1.204</span>""", html);
        Assert.Contains("""<span class="ra-kpi__delta">2 sem administrador</span>""", html);
    }

    [Fact]
    public async Task Kpi_sem_complemento_nao_tem_o_elemento_do_complemento()
    {
        var html = await RenderAsync("_Kpi", new KpiVm("Projetos", "12"));

        Assert.Contains("""<div class="ra-kpi ra-kpi--teal">""", html);
        Assert.DoesNotContain("ra-kpi__delta", html);
    }

    // ---------- Alerta ----------

    [Theory]
    [InlineData(AlertLevel.Fail, "ra-alert ra-alert--fail", "ra-badge ra-badge--fail", "Crítico")]
    [InlineData(AlertLevel.Warn, "ra-alert ra-alert--warn", "ra-badge ra-badge--warn", "Atenção")]
    [InlineData(AlertLevel.Info, "ra-alert ra-alert--info", "ra-badge ra-badge--brand", "Info")]
    public async Task Alerta_tem_classe_badge_com_rotulo_e_titulo_em_negrito(
        AlertLevel nivel, string alerta, string badge, string rotulo)
    {
        var html = await RenderAsync("_Alert", new AlertVm(nivel, "2 times sem administrador.", "Veja a tabela de times."));

        Assert.Contains($"<div class=\"{alerta}\">", html);
        Assert.Contains($"<span class=\"{badge}\">{rotulo}</span>", html);
        Assert.Contains("<b>2 times sem administrador.</b>", html);
        Assert.Contains("Veja a tabela de times.", html);
    }

    // ---------- Badge e chip de fonte ----------

    [Fact]
    public async Task Badge_usa_o_rotulo_padrao_ou_o_informado()
    {
        Assert.Equal("""<span class="ra-badge ra-badge--ok">Saudável</span>""", (await RenderAsync("_Badge", Status.Ok)).Trim());
        Assert.Equal("""<span class="ra-badge ra-badge--ok">Aprovado</span>""",
            (await RenderAsync("_Badge", Status.Ok, new() { ["Label"] = "Aprovado" })).Trim());
    }

    [Fact]
    public async Task Chip_so_com_a_sigla_tem_title_com_o_nome_da_fonte()
    {
        var html = await RenderAsync("_SourceChip", new SourceVm(Provider.GitHub), new() { ["IconOnly"] = true });

        Assert.Contains("ra-source ra-source--gh ra-source--icon", html);
        Assert.Contains("""title="GitHub" """.TrimEnd(), html);
        Assert.Equal("GH", Texto(html));
    }

    // ---------- Visão geral ----------

    [Fact]
    public async Task Visao_geral_tem_uma_coluna_por_kpi_e_os_kpis_na_ordem()
    {
        var html = await RenderAsync("_Overview", Document(kpis:
            [new KpiVm("Projetos", "12"), new KpiVm("Times", "30"), new KpiVm("Usuários", "80")]));

        Assert.Contains("""<section class="ra-overview">""", html);
        Assert.Contains("""<h1 class="ra-h1">Visão geral</h1>""", html);
        Assert.Contains("""<div class="ra-grid" style="--cols:3">""", html);
        Assert.Equal(["Projetos", "Times", "Usuários"],
            Regex.Matches(html, "ra-kpi__label\">(?<r>.*?)</span>").Select(m => m.Groups["r"].Value));
    }

    [Fact]
    public async Task Visao_geral_sem_alertas_diz_que_nao_ha_pontos_de_atencao()
    {
        var html = await RenderAsync("_Overview", Document());

        Assert.Contains("Nenhum ponto de atenção.", html);
        Assert.DoesNotContain("ra-alert", html);
    }

    [Fact]
    public async Task Visao_geral_mostra_os_alertas_na_ordem_recebida()
    {
        var html = await RenderAsync("_Overview", Document(alerts:
        [
            new AlertVm(AlertLevel.Warn, "Primeiro.", "a"),
            new AlertVm(AlertLevel.Info, "Segundo.", "b")
        ]));

        Assert.DoesNotContain("Nenhum ponto de atenção.", html);
        Assert.Equal(["Primeiro.", "Segundo."], Regex.Matches(html, "<b>(?<t>.*?)</b>").Select(m => m.Groups["t"].Value));
    }

    // ---------- Marca ----------

    [Fact]
    public async Task Marca_define_o_simbolo_usado_pela_capa()
    {
        var html = await RenderAsync("_Mark", new object());

        Assert.Contains("""<symbol id="ra-mark" """.TrimEnd(), html);
    }
}

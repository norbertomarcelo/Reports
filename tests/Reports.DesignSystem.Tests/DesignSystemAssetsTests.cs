using System.Text.RegularExpressions;

namespace Reports.DesignSystem.Tests;

public class DesignSystemAssetsTests
{
    private static readonly string Styles = DesignSystemAssets.Styles;

    [Fact]
    public void Junta_tokens_fontes_componentes_folha_e_fluxo_nesta_ordem()
    {
        AssertOrdem(Styles, "--surface:", "@font-face", ".ra-btn", ".ra-page--cover", "--ra-strip:");
    }

    [Fact]
    public void Embute_as_oito_fontes_como_data_uri()
    {
        Assert.Equal(8, Regex.Matches(Styles, "url\\(\"data:font/woff2;base64,").Count);
    }

    [Fact]
    public void Nao_aponta_para_nenhum_recurso_externo()
    {
        var alvos = Regex.Matches(Styles, "url\\(\\s*[\"']?(?<alvo>[^\"')]*)").Select(m => m.Groups["alvo"].Value).ToList();
        Assert.NotEmpty(alvos);
        Assert.All(alvos, alvo => Assert.StartsWith("data:", alvo));
        Assert.DoesNotContain("@import", Styles);
        Assert.DoesNotContain("http://", Styles.Replace("http://www.w3.org/2000/svg", ""));
        Assert.DoesNotContain("https://", Styles);
    }

    [Fact]
    public void A_marca_do_cabecalho_usa_a_cor_do_token_teal()
    {
        var match = Regex.Match(Styles, "--ra-mark-teal: url\\(\"data:image/svg\\+xml,(?<svg>[^\"]+)\"\\)");

        Assert.True(match.Success);
        var svg = Uri.UnescapeDataString(match.Groups["svg"].Value);
        Assert.Contains("#0f766e", svg);
        Assert.DoesNotContain("currentColor", svg);
    }

    [Fact]
    public void Devolve_sempre_o_mesmo_texto()
    {
        Assert.Same(Styles, DesignSystemAssets.Styles);
    }

    private static void AssertOrdem(string texto, params string[] trechos)
    {
        var posicoes = trechos.Select(t => texto.IndexOf(t, StringComparison.Ordinal)).ToArray();
        Assert.DoesNotContain(-1, posicoes);
        Assert.Equal(posicoes.Order(), posicoes);
    }
}

using System.Text.RegularExpressions;

namespace Reports.DesignSystem.Tests;

/// <summary>
/// O DS manda usar só tokens: nenhuma cor, família ou tamanho de texto literal em CSS novo (DESIGN.md, seção 3).
/// </summary>
public class ReportFlowCssTests
{
    private static readonly string Css = Regex.Replace(
        File.ReadAllText(Caminho()), "/\\*.*?\\*/", "", RegexOptions.Singleline);

    private static string Caminho([System.Runtime.CompilerServices.CallerFilePath] string arquivo = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(arquivo)!,
            "..", "..", "src", "Reports.DesignSystem", "Styles", "report-flow.css"));

    [Fact]
    public void Nao_tem_cor_literal()
    {
        Assert.Empty(Regex.Matches(Css, "#[0-9a-fA-F]{3,8}\\b|\\b(?:rgba?|hsla?)\\("));
    }

    [Fact]
    public void Familia_de_fonte_so_por_token()
    {
        Assert.DoesNotContain("font-family: \"", Css);
        Assert.All(Valores("font-family"), valor => Assert.StartsWith("var(--font-", valor));
    }

    [Fact]
    public void Texto_so_pelos_atalhos_de_tipografia_do_design_system()
    {
        Assert.DoesNotContain("font-size", Css);
        Assert.All(Valores("font"), valor => Assert.Contains("var(--text-", valor));
    }

    private static IEnumerable<string> Valores(string propriedade) =>
        Regex.Matches(Css, $"(?<![\\w-]){propriedade}\\s*:\\s*(?<valor>[^;}}]+)")
            .Select(m => m.Groups["valor"].Value.Trim());
}

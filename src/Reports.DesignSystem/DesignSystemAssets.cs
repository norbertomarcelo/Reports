using System.Reflection;
using System.Text.RegularExpressions;

namespace Reports.DesignSystem;

/// <summary>
/// CSS do design system pronto para um <c>&lt;style&gt;</c>, com as fontes embutidas (spec 020, RF-03).
/// O PDF nasce de uma string HTML, sem URL base, então nada pode ser servido por arquivo.
/// </summary>
public static class DesignSystemAssets
{
    private static readonly Lazy<string> _styles = new(Build);

    public static string Styles => _styles.Value;

    private static string Build()
    {
        var tokens = Read("ds/tokens.css");

        // fonts.css aponta para ../fonts/X.woff2; cada arquivo vira data URI.
        var fonts = Regex.Replace(Read("ds/fonts.css"), """url\("\.\./fonts/(?<file>[^"]+)"\)""",
            m => $"url(\"data:font/woff2;base64,{Convert.ToBase64String(ReadBytes("ds/fonts/" + m.Groups["file"].Value))}\")");

        return string.Join('\n',
            tokens, fonts, Read("ds/ra.css"), Read("ds/report.css"), Read("ds/report-flow.css"), MarkVariable(tokens));
    }

    /// <summary>
    /// <c>--ra-mark-teal</c>: o mark.svg do pacote na cor do token <c>--teal</c> (tema claro). Um SVG usado como
    /// imagem não enxerga <c>var()</c>, então a cor é montada aqui para o CSS do relatório não ter cor literal.
    /// </summary>
    private static string MarkVariable(string tokens)
    {
        var teal = Regex.Match(tokens, "--teal:\\s*(?<cor>#[0-9a-fA-F]{6})");
        if (!teal.Success)
            throw new InvalidOperationException("O token --teal não foi encontrado em tokens.css.");

        var svg = Regex.Replace(Read("ds/mark.svg").Replace("currentColor", teal.Groups["cor"].Value), "\\s+", " ").Trim();
        return $":root {{ --ra-mark-teal: url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\"); }}";
    }

    private static string Read(string name)
    {
        using var reader = new StreamReader(Open(name));
        return reader.ReadToEnd();
    }

    private static byte[] ReadBytes(string name)
    {
        using var stream = Open(name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static Stream Open(string name) =>
        typeof(DesignSystemAssets).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Recurso do design system não encontrado: {name}.");
}

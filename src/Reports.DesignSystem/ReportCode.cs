using System.Globalization;

namespace Reports.DesignSystem;

/// <summary>
/// Código do relatório: sigla da fonte + data e hora da geração em UTC (ex.: <c>AD-20261026-1542</c>).
/// Só <see cref="Create"/> o cria, então o valor tem apenas letras, dígitos e hífen e pode ir direto para uma
/// string CSS (o <c>content</c> do <c>@page</c>) sem escape.
/// </summary>
public sealed record ReportCode
{
    public string Value { get; }

    private ReportCode(string value) => Value = value;

    public static ReportCode Create(Provider provider, DateTimeOffset generatedAt) =>
        new($"{Ra.Mark(provider)}-{generatedAt.UtcDateTime.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}");

    public override string ToString() => Value;
}

using System.Globalization;

namespace Reports.DesignSystem;

/// <summary>Mapeia os modelos para as classes e os textos do design system (ra.css) e formata números e datas em pt-BR.</summary>
public static class Ra
{
    // Separadores fixos: o formato não pode mudar com a cultura do servidor nem sumir no modo invariante do ICU.
    private static readonly NumberFormatInfo PtBrNumber = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NegativeSign = "-"
    };

    public static string Key(Provider provider) => provider switch
    {
        Provider.AzureDevOps => "ad",
        Provider.GitHub => "gh",
        Provider.Azure => "az",
        Provider.Aws => "aw",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    /// <summary>Sigla do chip de fonte (AD, GH, AZ, AW).</summary>
    public static string Mark(Provider provider) => Key(provider).ToUpperInvariant();

    public static string ProviderName(Provider provider) => provider switch
    {
        Provider.AzureDevOps => "Azure DevOps",
        Provider.GitHub => "GitHub",
        Provider.Azure => "Azure",
        Provider.Aws => "AWS",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    public static string BadgeClass(Status status) => status switch
    {
        Status.Ok => "ra-badge ra-badge--ok",
        Status.Warn => "ra-badge ra-badge--warn",
        Status.Fail => "ra-badge ra-badge--fail",
        _ => "ra-badge"
    };

    public static string StatusLabel(Status status) => status switch
    {
        Status.Ok => "Saudável",
        Status.Warn => "Atenção",
        Status.Fail => "Falhou",
        _ => "Sem dados"
    };

    public static string AlertClass(AlertLevel level) => level switch
    {
        AlertLevel.Fail => "ra-alert ra-alert--fail",
        AlertLevel.Warn => "ra-alert ra-alert--warn",
        _ => "ra-alert ra-alert--info"
    };

    public static string AlertBadgeClass(AlertLevel level) => level switch
    {
        AlertLevel.Fail => "ra-badge ra-badge--fail",
        AlertLevel.Warn => "ra-badge ra-badge--warn",
        _ => "ra-badge ra-badge--brand"
    };

    public static string AlertLabel(AlertLevel level) => level switch
    {
        AlertLevel.Fail => "Crítico",
        AlertLevel.Warn => "Atenção",
        _ => "Info"
    };

    public static string KpiClass(KpiTone tone) => "ra-kpi ra-kpi--" + tone.ToString().ToLowerInvariant();

    /// <summary>Inteiro com ponto nos milhares (1.204).</summary>
    public static string Number(long value) => value.ToString("N0", PtBrNumber);

    /// <summary>Data e hora no formato <c>dd/MM/aaaa HH:mm</c>.</summary>
    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}

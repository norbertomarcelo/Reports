namespace Reports.DesignSystem;

/// <summary>Fonte de dados do relatório; vira o chip <c>ra-source--ad|gh|az|aw</c>.</summary>
public enum Provider { AzureDevOps, GitHub, Azure, Aws }

/// <summary>Estado exibido num <c>ra-badge</c>, sempre com texto.</summary>
public enum Status { Ok, Warn, Fail, Neutral }

public enum AlertLevel { Fail, Warn, Info }

public enum KpiTone { Teal, Success, Indigo, Orange }

/// <param name="Account">Conta ou organização da fonte (ex.: a organização do Azure DevOps).</param>
public sealed record SourceVm(Provider Provider, string? Account = null);

/// <summary>Fato da capa: rótulo em caixa alta e valor em fonte mono.</summary>
public sealed record ReportFact(string Label, string Value);

public sealed record KpiVm(string Label, string Value, string? Delta = null, KpiTone Tone = KpiTone.Teal);

/// <summary>Alerta da visão geral: o título em negrito e, depois, onde ver o detalhe.</summary>
public sealed record AlertVm(AlertLevel Level, string Title, string Body);

/// <summary>Capa e visão geral; o conteúdo dos capítulos vem da view de cada provedor.</summary>
/// <param name="Facts">Os fatos de texto da capa; o estado geral é o último, como badge.</param>
public sealed record ReportDocument(
    ReportCode Code,
    string Title,
    string Subtitle,
    IReadOnlyList<SourceVm> Sources,
    IReadOnlyList<ReportFact> Facts,
    Status OverallStatus,
    IReadOnlyList<KpiVm> Kpis,
    IReadOnlyList<AlertVm> Alerts);

/// <summary>Modelo de qualquer relatório que usa o layout do design system (spec 020).</summary>
public interface IReportModel
{
    ReportDocument Document { get; }
}

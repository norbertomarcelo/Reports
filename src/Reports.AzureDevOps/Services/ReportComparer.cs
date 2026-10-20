using System.Globalization;

namespace Reports.AzureDevOps.Services;

/// <summary>Ordenação do relatório: <c>pt-BR</c>, sem diferenciar maiúsculas de minúsculas.</summary>
internal static class ReportComparer
{
    public static readonly StringComparer PtBr =
        StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);
}

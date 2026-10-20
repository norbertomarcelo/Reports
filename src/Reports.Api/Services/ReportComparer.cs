using System.Globalization;

namespace Reports.Api.Services;

/// <summary>Ordenação do relatório: <c>pt-BR</c>, sem diferenciar maiúsculas de minúsculas.</summary>
public static class ReportComparer
{
    public static readonly StringComparer PtBr =
        StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);
}

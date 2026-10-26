using System.Globalization;
using Reports.AzureDevOps.Dtos.Requests;
using Reports.DesignSystem;

namespace Reports.AzureDevOps.Services;

/// <summary>
/// Números da visão geral, tirados do que o relatório já buscou (spec 020, RF-10: nenhuma chamada nova).
/// <c>null</c> = a seção que traz o dado não foi pedida (ou, em <c>TeamsWithoutAdmin</c> e <c>ExternalUsers</c>,
/// faltou a condição: membros buscados com <c>teams</c> + <c>users</c>, e <c>InternalDomains</c> configurado).
/// </summary>
internal record OverviewData(
    int Projects,
    int? Teams, int? TeamsWithoutAdmin,
    int? Users, int? ExternalUsers,
    long? OpenItems, long? ClosedItems, int? PartialProjects,
    int? Repositories, int? EmptyRepositories, int? DisabledRepositories);

/// <summary>KPIs, alertas e estado geral da capa e da visão geral (spec 020, RF-07 a RF-09).</summary>
internal static class ReportOverview
{
    public static IReadOnlyList<KpiVm> Kpis(OverviewData data)
    {
        var kpis = new List<KpiVm> { new("Projetos", Ra.Number(data.Projects), null, KpiTone.Teal) };

        if (data.Teams is { } teams)
        {
            kpis.Add(new KpiVm("Times", Ra.Number(teams),
                data.TeamsWithoutAdmin is { } noAdmin ? $"{Ra.Number(noAdmin)} sem administrador" : null,
                KpiTone.Indigo));
        }

        if (data.Users is { } users)
        {
            kpis.Add(new KpiVm("Usuários", Ra.Number(users),
                data.ExternalUsers is { } external ? Count(external, "externo", "externos") : null,
                KpiTone.Teal));
        }

        if (data.OpenItems is { } open)
        {
            kpis.Add(new KpiVm("Itens abertos", Ra.Number(open),
                data.ClosedItems is { } closed ? Count(closed, "fechado", "fechados") : null,
                KpiTone.Orange));
        }

        if (data.Repositories is { } repositories)
        {
            kpis.Add(new KpiVm("Repositórios", Ra.Number(repositories),
                $"{Count(data.EmptyRepositories ?? 0, "vazio", "vazios")} · {Count(data.DisabledRepositories ?? 0, "desativado", "desativados")}",
                KpiTone.Indigo));
        }

        return kpis;
    }

    /// <summary>Até quatro alertas, nesta ordem, cada um só quando a condição ocorre. Sem nenhum, a lista é vazia.</summary>
    public static IReadOnlyList<AlertVm> Alerts(OverviewData data)
    {
        var alerts = new List<AlertVm>();

        if (data.TeamsWithoutAdmin is > 0 and var noAdmin)
        {
            alerts.Add(new AlertVm(AlertLevel.Warn,
                $"{Count(noAdmin, "time sem administrador", "times sem administrador")}.",
                "Veja a coluna Administradores na tabela de times de cada projeto."));
        }

        if (data.PartialProjects is > 0 and var partial)
        {
            alerts.Add(new AlertVm(AlertLevel.Warn,
                $"Contagem de itens parcial em {Count(partial, "projeto", "projetos")}.",
                "O Azure DevOps devolveu o resultado em mais de uma página; o capítulo do projeto mostra o aviso."));
        }

        if (data.ExternalUsers is > 0 and var external)
        {
            alerts.Add(new AlertVm(AlertLevel.Info,
                $"{Count(external, "usuário externo", "usuários externos")}.",
                "Usuários com e-mail fora dos domínios internos; veja a marca Externo em Usuários do projeto."));
        }

        var emptyOrDisabled = (data.EmptyRepositories ?? 0) + (data.DisabledRepositories ?? 0);
        if (emptyOrDisabled > 0)
        {
            alerts.Add(new AlertVm(AlertLevel.Info,
                $"{Count(emptyOrDisabled, "repositório vazio ou desativado", "repositórios vazios ou desativados")}.",
                "Sem commits no branch padrão ou desativados; veja as marcas na tabela de repositórios."));
        }

        return alerts;
    }

    /// <summary>Sem projetos, "Sem dados"; com algum alerta de atenção, "Atenção"; nos outros casos, "Saudável".</summary>
    public static Status OverallStatus(OverviewData data, IReadOnlyList<AlertVm> alerts)
    {
        if (data.Projects == 0)
            return Status.Neutral;
        if (alerts.Any(a => a.Level == AlertLevel.Fail))
            return Status.Fail;
        return alerts.Any(a => a.Level == AlertLevel.Warn) ? Status.Warn : Status.Ok;
    }

    /// <summary>Seções pedidas, na ordem fixa do relatório: "Projetos, times e usuários".</summary>
    public static string SectionsText(ReportSections sections)
    {
        var names = new List<string>();
        if (sections.Projects) names.Add("projetos");
        if (sections.Teams) names.Add("times");
        if (sections.Users) names.Add("usuários");
        if (sections.WorkItems) names.Add("itens de trabalho");
        if (sections.Repositories) names.Add("repositórios");

        var text = names.Count <= 1 ? string.Concat(names) : string.Join(", ", names[..^1]) + " e " + names[^1];
        return text.Length == 0 ? text : char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];
    }

    /// <summary>Quantas seções o corpo da requisição aceita; a capa mostra "3 de 5".</summary>
    public const int SectionTotal = 5;

    public static int SectionCount(ReportSections sections) =>
        new[] { sections.Projects, sections.Teams, sections.Users, sections.WorkItems, sections.Repositories }.Count(s => s);

    private static string Count(long n, string singular, string plural) =>
        $"{Ra.Number(n)} {(n == 1 ? singular : plural)}";
}

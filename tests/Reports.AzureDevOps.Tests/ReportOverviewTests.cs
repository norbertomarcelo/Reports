using Reports.AzureDevOps.Dtos.Requests;
using Reports.AzureDevOps.Services;
using Reports.DesignSystem;

namespace Reports.AzureDevOps.Tests;

public class ReportOverviewTests
{
    private static OverviewData Projetos(int projetos = 12) => new(projetos, null, null, null, null, null, null, null, null, null, null);

    /// <summary>Cenário com as cinco seções e <c>InternalDomains</c> configurado.</summary>
    private static OverviewData Completo() => new(
        Projects: 12,
        Teams: 30, TeamsWithoutAdmin: 2,
        Users: 1204, ExternalUsers: 5,
        OpenItems: 9876, ClosedItems: 12345, PartialProjects: 1,
        Repositories: 40, EmptyRepositories: 3, DisabledRepositories: 2);

    private static List<(string Rotulo, string Valor, string? Complemento)> Kpis(OverviewData dados) =>
        ReportOverview.Kpis(dados).Select(k => (k.Label, k.Value, k.Delta)).ToList();

    // ---------- KPIs (RF-08) ----------

    [Fact]
    public void So_projetos_mostra_um_unico_kpi()
    {
        Assert.Equal([("Projetos", "12", (string?)null)], Kpis(Projetos()));
    }

    [Fact]
    public void Com_as_cinco_secoes_os_kpis_seguem_a_ordem_com_numeros_pt_br_e_complementos()
    {
        Assert.Equal(
            [
                ("Projetos", "12", null),
                ("Times", "30", "2 sem administrador"),
                ("Usuários", "1.204", "5 externos"),
                ("Itens abertos", "9.876", "12.345 fechados"),
                ("Repositórios", "40", "3 vazios · 2 desativados")
            ],
            Kpis(Completo()));
    }

    [Fact]
    public void Times_sem_membros_buscados_nao_tem_complemento()
    {
        var dados = Projetos() with { Teams = 30 };

        Assert.Equal([("Projetos", "12", null), ("Times", "30", null)], Kpis(dados));
    }

    [Fact]
    public void Usuarios_sem_InternalDomains_nao_tem_complemento()
    {
        var dados = Projetos() with { Teams = 30, TeamsWithoutAdmin = 0, Users = 80 };

        Assert.Equal(("Usuários", "80", null), Kpis(dados)[2]);
    }

    [Fact]
    public void Complementos_vao_para_o_singular_com_um()
    {
        var dados = Completo() with { ExternalUsers = 1, ClosedItems = 1, EmptyRepositories = 1, DisabledRepositories = 1 };

        var kpis = Kpis(dados);

        Assert.Equal("1 externo", kpis[2].Complemento);
        Assert.Equal("1 fechado", kpis[3].Complemento);
        Assert.Equal("1 vazio · 1 desativado", kpis[4].Complemento);
    }

    [Fact]
    public void Cada_kpi_aparece_so_quando_a_sua_secao_foi_buscada()
    {
        var itens = Projetos() with { OpenItems = 4, ClosedItems = 1, PartialProjects = 0 };
        var repositorios = Projetos() with { Repositories = 7, EmptyRepositories = 0, DisabledRepositories = 0 };

        Assert.Equal(["Projetos", "Itens abertos"], Kpis(itens).Select(k => k.Rotulo));
        Assert.Equal(["Projetos", "Repositórios"], Kpis(repositorios).Select(k => k.Rotulo));
    }

    // ---------- Alertas (RF-09) ----------

    [Fact]
    public void Sem_nenhuma_condicao_nao_ha_alertas()
    {
        Assert.Empty(ReportOverview.Alerts(Projetos()));
        Assert.Empty(ReportOverview.Alerts(Completo() with
        {
            TeamsWithoutAdmin = 0, ExternalUsers = 0, PartialProjects = 0, EmptyRepositories = 0, DisabledRepositories = 0
        }));
    }

    [Fact]
    public void As_quatro_condicoes_geram_quatro_alertas_na_ordem()
    {
        var alertas = ReportOverview.Alerts(Completo());

        Assert.Equal(
            [
                (AlertLevel.Warn, "2 times sem administrador."),
                (AlertLevel.Warn, "Contagem de itens parcial em 1 projeto."),
                (AlertLevel.Info, "5 usuários externos."),
                (AlertLevel.Info, "5 repositórios vazios ou desativados.")
            ],
            alertas.Select(a => (a.Level, a.Title)));
        Assert.All(alertas, a => Assert.False(string.IsNullOrWhiteSpace(a.Body)));
    }

    [Theory]
    [InlineData(1, "1 time sem administrador.")]
    [InlineData(2, "2 times sem administrador.")]
    public void Plural_de_times_sem_administrador(int times, string titulo)
    {
        var alertas = ReportOverview.Alerts(Projetos() with { Teams = 10, TeamsWithoutAdmin = times });

        Assert.Equal(titulo, Assert.Single(alertas).Title);
    }

    [Theory]
    [InlineData(1, "Contagem de itens parcial em 1 projeto.")]
    [InlineData(3, "Contagem de itens parcial em 3 projetos.")]
    public void Plural_de_contagem_parcial(int projetos, string titulo)
    {
        var alertas = ReportOverview.Alerts(Projetos() with { OpenItems = 1, ClosedItems = 1, PartialProjects = projetos });

        Assert.Equal(titulo, Assert.Single(alertas).Title);
    }

    [Theory]
    [InlineData(1, "1 usuário externo.")]
    [InlineData(2, "2 usuários externos.")]
    public void Plural_de_usuarios_externos(int externos, string titulo)
    {
        var alertas = ReportOverview.Alerts(Projetos() with { Users = 10, ExternalUsers = externos });

        Assert.Equal(titulo, Assert.Single(alertas).Title);
    }

    [Theory]
    [InlineData(1, 0, "1 repositório vazio ou desativado.")]
    [InlineData(0, 1, "1 repositório vazio ou desativado.")]
    [InlineData(2, 1, "3 repositórios vazios ou desativados.")]
    public void Repositorios_vazios_e_desativados_somam_no_mesmo_alerta(int vazios, int desativados, string titulo)
    {
        var alertas = ReportOverview.Alerts(Projetos() with
        {
            Repositories = 20, EmptyRepositories = vazios, DisabledRepositories = desativados
        });

        Assert.Equal(titulo, Assert.Single(alertas).Title);
    }

    [Fact]
    public void Sem_teams_e_users_juntos_nao_ha_alerta_de_administrador()
    {
        // TeamsWithoutAdmin só vem preenchido com teams + users (os membros precisam ter sido buscados).
        Assert.Empty(ReportOverview.Alerts(Projetos() with { Teams = 10 }));
        Assert.Empty(ReportOverview.Alerts(Projetos() with { Teams = 10, Users = 50 }));
    }

    [Fact]
    public void Cada_alerta_diz_onde_ver_o_detalhe()
    {
        var corpos = ReportOverview.Alerts(Completo()).Select(a => a.Body).ToList();

        Assert.Contains("tabela de times", corpos[0]);
        Assert.Contains("mais de uma página", corpos[1]);
        Assert.Contains("Externo", corpos[2]);
        Assert.Contains("tabela de repositórios", corpos[3]);
    }

    // ---------- Estado geral (RF-07) ----------

    [Fact]
    public void Sem_projetos_o_estado_e_sem_dados()
    {
        var dados = Projetos(projetos: 0);

        Assert.Equal(Status.Neutral, ReportOverview.OverallStatus(dados, ReportOverview.Alerts(dados)));
    }

    [Fact]
    public void Com_alerta_de_atencao_o_estado_e_atencao()
    {
        var dados = Completo();

        Assert.Equal(Status.Warn, ReportOverview.OverallStatus(dados, ReportOverview.Alerts(dados)));
    }

    [Fact]
    public void So_com_alertas_informativos_o_estado_continua_saudavel()
    {
        var dados = Projetos() with { Users = 10, ExternalUsers = 2, Repositories = 5, EmptyRepositories = 1, DisabledRepositories = 0 };

        var alertas = ReportOverview.Alerts(dados);

        Assert.Equal(2, alertas.Count);
        Assert.Equal(Status.Ok, ReportOverview.OverallStatus(dados, alertas));
    }

    [Fact]
    public void Sem_alertas_o_estado_e_saudavel()
    {
        Assert.Equal(Status.Ok, ReportOverview.OverallStatus(Projetos(), []));
    }

    // ---------- Subtítulo e contagem de seções da capa (RF-06) ----------

    private static ReportSections Sections(
        bool projects = false, bool teams = false, bool users = false, bool workItems = false, bool repositories = false) =>
        new() { Projects = projects, Teams = teams, Users = users, WorkItems = workItems, Repositories = repositories };

    [Fact]
    public void Subtitulo_lista_as_secoes_pedidas_em_texto()
    {
        Assert.Equal("Projetos", ReportOverview.SectionsText(Sections(projects: true)));
        Assert.Equal("Times", ReportOverview.SectionsText(Sections(teams: true)));
        Assert.Equal("Projetos e repositórios", ReportOverview.SectionsText(Sections(projects: true, repositories: true)));
        Assert.Equal("Projetos, times e usuários", ReportOverview.SectionsText(Sections(projects: true, teams: true, users: true)));
        Assert.Equal("Projetos, times, usuários, itens de trabalho e repositórios",
            ReportOverview.SectionsText(Sections(true, true, true, true, true)));
    }

    [Fact]
    public void Subtitulo_nao_depende_da_ordem_em_que_as_flags_foram_enviadas()
    {
        Assert.Equal("Times e itens de trabalho", ReportOverview.SectionsText(Sections(workItems: true, teams: true)));
    }

    [Fact]
    public void Conta_as_secoes_pedidas()
    {
        Assert.Equal(1, ReportOverview.SectionCount(Sections(users: true)));
        Assert.Equal(3, ReportOverview.SectionCount(Sections(projects: true, teams: true, users: true)));
        Assert.Equal(5, ReportOverview.SectionCount(Sections(true, true, true, true, true)));
    }
}

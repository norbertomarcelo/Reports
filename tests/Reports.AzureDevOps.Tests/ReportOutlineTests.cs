using Reports.AzureDevOps.Dtos.Requests;
using Reports.AzureDevOps.Services;

namespace Reports.AzureDevOps.Tests;

public class ReportOutlineTests
{
    private static ReportSections Sections(
        bool projects = false, bool teams = false, bool users = false, bool workItems = false,
        bool repositories = false) =>
        new() { Projects = projects, Teams = teams, Users = users, WorkItems = workItems, Repositories = repositories };

    [Fact]
    public void So_projetos_numera_apenas_o_capitulo_de_projetos()
    {
        var outline = ReportOutline.Build(Sections(projects: true), [2, 1]);

        Assert.Equal("1", outline.Projects);
        Assert.Empty(outline.ProjectChapters);
    }

    [Fact]
    public void So_times_numera_um_capitulo_por_projeto_com_o_subcapitulo_de_times()
    {
        var outline = ReportOutline.Build(Sections(teams: true), [2, 1]);

        Assert.Null(outline.Projects);
        Assert.Collection(outline.ProjectChapters,
            c => { Assert.Equal("1", c.Chapter); Assert.Equal("1.1", c.Teams); Assert.Null(c.UsersByTeam); Assert.Empty(c.TeamNumbers); },
            c => { Assert.Equal("2", c.Chapter); Assert.Equal("2.1", c.Teams); Assert.Null(c.UsersByTeam); });
    }

    [Fact]
    public void So_usuarios_numera_usuarios_por_time_como_primeiro_subcapitulo()
    {
        var outline = ReportOutline.Build(Sections(users: true), [2]);

        var chapter = Assert.Single(outline.ProjectChapters);
        Assert.Equal("1", chapter.Chapter);
        Assert.Null(chapter.Teams);
        Assert.Equal("1.1", chapter.UsersByTeam);
        Assert.Equal(["1.1.1", "1.1.2"], chapter.TeamNumbers);
    }

    [Fact]
    public void As_tres_secoes_comecam_os_capitulos_de_projeto_no_2()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, users: true), [1, 2]);

        Assert.Equal("1", outline.Projects);
        Assert.Collection(outline.ProjectChapters,
            c =>
            {
                Assert.Equal(["2", "2.1", "2.2"], [c.Chapter, c.Teams!, c.UsersByTeam!]);
                Assert.Equal(["2.2.1"], c.TeamNumbers);
            },
            c =>
            {
                Assert.Equal(["3", "3.1", "3.2"], [c.Chapter, c.Teams!, c.UsersByTeam!]);
                Assert.Equal(["3.2.1", "3.2.2"], c.TeamNumbers);
            });
    }

    [Fact]
    public void Usuarios_do_projeto_e_o_ultimo_subcapitulo()
    {
        var completo = ReportOutline.Build(Sections(teams: true, users: true), [2]);
        var soUsuarios = ReportOutline.Build(Sections(users: true), [2]);
        var soTimes = ReportOutline.Build(Sections(teams: true), [2]);

        Assert.Equal("1.3", Assert.Single(completo.ProjectChapters).ProjectUsers);
        Assert.Equal("1.2", Assert.Single(soUsuarios.ProjectChapters).ProjectUsers);
        Assert.Null(Assert.Single(soTimes.ProjectChapters).ProjectUsers);
    }

    [Fact]
    public void Itens_de_trabalho_vem_depois_de_projetos_e_antes_dos_capitulos_de_projeto()
    {
        var outline = ReportOutline.Build(Sections(projects: true, workItems: true), [0, 0]);

        Assert.Equal("1", outline.Projects);
        Assert.Equal("2", outline.WorkItems);
        Assert.Collection(outline.ProjectChapters,
            c => Assert.Equal(["3", "3.1"], [c.Chapter, c.WorkItems!]),
            c => Assert.Equal(["4", "4.1"], [c.Chapter, c.WorkItems!]));
    }

    [Fact]
    public void So_itens_de_trabalho_numera_o_capitulo_geral_como_1_e_o_subcapitulo_como_N_1()
    {
        var outline = ReportOutline.Build(Sections(workItems: true), [3]);

        Assert.Null(outline.Projects);
        Assert.Equal("1", outline.WorkItems);
        var chapter = Assert.Single(outline.ProjectChapters);
        Assert.Equal(["2", "2.1"], [chapter.Chapter, chapter.WorkItems!]);
        Assert.Null(chapter.Teams);
        Assert.Null(chapter.UsersByTeam);
        Assert.Null(chapter.ProjectUsers);
        Assert.Empty(chapter.TeamNumbers);
    }

    [Fact]
    public void Com_todas_as_secoes_itens_de_trabalho_e_o_ultimo_subcapitulo()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, users: true, workItems: true), [1]);

        Assert.Equal(["1", "2"], [outline.Projects!, outline.WorkItems!]);
        var chapter = Assert.Single(outline.ProjectChapters);
        Assert.Equal(["3", "3.1", "3.2", "3.3", "3.4"],
            [chapter.Chapter, chapter.Teams!, chapter.UsersByTeam!, chapter.ProjectUsers!, chapter.WorkItems!]);
    }

    [Fact]
    public void So_repositorios_numera_o_projeto_como_1_e_o_subcapitulo_como_1_1()
    {
        var outline = ReportOutline.Build(Sections(repositories: true), [2]);

        Assert.Null(outline.Projects);
        Assert.Null(outline.WorkItems);
        var chapter = Assert.Single(outline.ProjectChapters);
        Assert.Equal(["1", "1.1"], [chapter.Chapter, chapter.Repositories!]);
        Assert.Null(chapter.Teams);
        Assert.Null(chapter.WorkItems);
    }

    [Fact]
    public void Com_todas_as_secoes_repositorios_e_o_ultimo_subcapitulo_depois_de_itens_de_trabalho()
    {
        var outline = ReportOutline.Build(
            Sections(projects: true, teams: true, users: true, workItems: true, repositories: true), [1]);

        var chapter = Assert.Single(outline.ProjectChapters);
        Assert.Equal(["3", "3.4", "3.5"], [chapter.Chapter, chapter.WorkItems!, chapter.Repositories!]);
    }

    [Fact]
    public void Sem_a_flag_nao_ha_numero_de_repositorios()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, workItems: true), [1]);

        Assert.Null(Assert.Single(outline.ProjectChapters).Repositories);
    }

    [Fact]
    public void Sem_a_flag_nao_ha_numero_de_itens_de_trabalho()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, users: true), [1]);

        Assert.Null(outline.WorkItems);
        Assert.Null(Assert.Single(outline.ProjectChapters).WorkItems);
    }

    [Fact]
    public void Sem_projetos_nao_ha_capitulos_de_projeto()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, users: true), []);

        Assert.Equal("1", outline.Projects);
        Assert.Empty(outline.ProjectChapters);
    }
}

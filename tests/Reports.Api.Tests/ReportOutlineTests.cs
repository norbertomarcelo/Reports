using Reports.Api.Dtos.Requests;
using Reports.Api.Services;

namespace Reports.Api.Tests;

public class ReportOutlineTests
{
    private static ReportSections Sections(bool projects = false, bool teams = false, bool users = false) =>
        new() { Projects = projects, Teams = teams, Users = users };

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
    public void Sem_projetos_nao_ha_capitulos_de_projeto()
    {
        var outline = ReportOutline.Build(Sections(projects: true, teams: true, users: true), []);

        Assert.Equal("1", outline.Projects);
        Assert.Empty(outline.ProjectChapters);
    }
}

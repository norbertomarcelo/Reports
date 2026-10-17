using Reports.Api.Services;

namespace Reports.Api.Tests;

public class ProjectLabelsTests
{
    [Theory]
    [InlineData("wellFormed", "Ativo")]
    [InlineData("new", "Em criação")]
    [InlineData("createPending", "Criação pendente")]
    [InlineData("deleting", "Em exclusão")]
    [InlineData("deleted", "Excluído")]
    [InlineData("unchanged", "Sem alteração")]
    [InlineData("WELLFORMED", "Ativo")]
    [InlineData("WellFormed", "Ativo")]
    [InlineData("all", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Traduz_o_estado_do_projeto(string? valor, string? esperado)
    {
        Assert.Equal(esperado, ProjectLabels.State(valor));
    }

    [Theory]
    [InlineData("private", "Privado")]
    [InlineData("public", "Público")]
    [InlineData("Private", "Privado")]
    [InlineData("PUBLIC", "Público")]
    [InlineData("organization", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Traduz_a_visibilidade_do_projeto(string? valor, string? esperado)
    {
        Assert.Equal(esperado, ProjectLabels.Visibility(valor));
    }
}

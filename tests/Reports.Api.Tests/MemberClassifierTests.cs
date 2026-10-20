using Reports.Api.Services;

namespace Reports.Api.Tests;

public class MemberClassifierTests
{
    [Theory]
    [InlineData("[Alfa]\\Contributors", true)]
    [InlineData("[org-teste]\\Project Collection Administrators", true)]
    [InlineData("ana@empresa.com.br", false)]
    [InlineData("[sem barra]", false)]
    [InlineData(null, false)]
    public void Identifica_grupo_pelo_formato_do_uniqueName(string? uniqueName, bool esperado) =>
        Assert.Equal(esperado, MemberClassifier.IsGroup(uniqueName));

    [Theory]
    [InlineData("ana@empresa.com.br", false)]
    [InlineData("bia@EMPRESA.com.br", false)]
    [InlineData("carlos@gmail.com", true)]
    [InlineData("dani@ti.empresa.com.br", true)]
    [InlineData("sem-arroba", false)]
    [InlineData(null, false)]
    public void Externo_e_quem_tem_dominio_fora_da_lista(string? email, bool esperado) =>
        Assert.Equal(esperado, MemberClassifier.IsExternal(email, ["empresa.com.br"]));

    [Fact]
    public void Lista_vazia_nao_marca_ninguem_como_externo() =>
        Assert.False(MemberClassifier.IsExternal("carlos@gmail.com", []));
}

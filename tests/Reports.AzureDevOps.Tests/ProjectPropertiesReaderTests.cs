using System.Text.Json;
using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Services;

namespace Reports.AzureDevOps.Tests;

public class ProjectPropertiesReaderTests
{
    private const string Flags = "System.SourceControlCapabilityFlags";
    private const string GitEnabled = "System.SourceControlGitEnabled";
    private const string GitPermissions = "System.SourceControlGitPermissionsInitialized";

    private static ProjectProperty Prop(string name, string jsonValue) =>
        new() { Name = name, Value = JsonDocument.Parse(jsonValue).RootElement.Clone() };

    private static ProjectDetails Ler(params ProjectProperty[] properties) => ProjectPropertiesReader.Read(properties);

    [Theory]
    [InlineData("Scrum")]
    [InlineData("Agile")]
    public void Processo_vem_de_System_Process_Template(string processo)
    {
        Assert.Equal(processo, Ler(Prop("System.Process Template", $"\"{processo}\"")).Process);
    }

    [Fact]
    public void Nome_da_propriedade_nao_diferencia_maiusculas()
    {
        Assert.Equal("Scrum", Ler(Prop("system.process template", "\"Scrum\"")).Process);
    }

    [Theory]
    [InlineData("\"1\"", "TFVC")]
    [InlineData("\"2\"", "Git")]
    [InlineData("\"3\"", "Git e TFVC")]
    [InlineData("1", "TFVC")]
    [InlineData("2", "Git")]
    [InlineData("3", "Git e TFVC")]
    [InlineData("0", null)]
    [InlineData("\"x\"", null)]
    [InlineData("null", null)]
    public void Controle_de_versao_traduz_as_flags(string valor, string? esperado)
    {
        Assert.Equal(esperado, Ler(Prop(Flags, valor)).SourceControl);
    }

    [Theory]
    [InlineData("\"True\"", "Sim")]
    [InlineData("\"true\"", "Sim")]
    [InlineData("true", "Sim")]
    [InlineData("\"False\"", "Não")]
    [InlineData("false", "Não")]
    [InlineData("\"talvez\"", null)]
    [InlineData("1", null)]
    [InlineData("null", null)]
    public void Git_habilitado_e_permissoes_traduzem_para_sim_ou_nao(string valor, string? esperado)
    {
        var detalhes = Ler(Prop(GitEnabled, valor), Prop(GitPermissions, valor));

        Assert.Equal(esperado, detalhes.GitEnabled);
        Assert.Equal(esperado, detalhes.GitPermissionsInitialized);
    }

    [Fact]
    public void Sem_propriedades_tudo_fica_sem_valor()
    {
        Assert.Equal(new ProjectDetails(null, null, null, null), Ler());
    }

    [Fact]
    public void Ignora_propriedades_fora_da_lista_como_a_quantidade_de_times()
    {
        var detalhes = Ler(Prop("System.Microsoft.TeamFoundation.Team.Count", "4"));

        Assert.Equal(new ProjectDetails(null, null, null, null), detalhes);
    }

    [Fact]
    public void Processo_vazio_fica_sem_valor()
    {
        Assert.Null(Ler(Prop("System.Process Template", "\"  \"")).Process);
    }

    [Fact]
    public void Em_nome_duplicado_vale_o_primeiro()
    {
        var detalhes = Ler(Prop("System.Process Template", "\"Scrum\""), Prop("SYSTEM.PROCESS TEMPLATE", "\"CMMI\""));

        Assert.Equal("Scrum", detalhes.Process);
    }
}

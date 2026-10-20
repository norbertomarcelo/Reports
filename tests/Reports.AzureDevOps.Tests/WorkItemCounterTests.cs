using Reports.AzureDevOps.Models;
using Reports.AzureDevOps.Services;

namespace Reports.AzureDevOps.Tests;

public class WorkItemCounterTests
{
    private static WorkItemGroup Grupo(string? tipo, string? categoria, string? area, long total) =>
        new() { WorkItemType = tipo, StateCategory = categoria, Area = area is null ? null : new WorkItemArea { AreaPath = area }, Count = total };

    private static WorkItemGroups Origem(bool parcial = false, params WorkItemGroup[] grupos) => new(grupos, parcial);

    private static WorkItemGroups Alfa() => Origem(false,
        Grupo("Bug", "InProgress", "Alfa\\Backend", 2),
        Grupo("Bug", "Completed", "Alfa\\Backend", 1),
        Grupo("Task", "Proposed", "Alfa\\Frontend", 3),
        Grupo("Task", "Resolved", "Alfa\\Frontend", 1));

    [Theory]
    [InlineData("Completed", true)]
    [InlineData("completed", true)]
    [InlineData("Proposed", false)]
    [InlineData("InProgress", false)]
    [InlineData("Resolved", false)]
    [InlineData(null, false)]
    public void Fechado_e_so_a_categoria_Completed(string? categoria, bool fechado) =>
        Assert.Equal(fechado, WorkItemCounter.IsClosed(categoria));

    [Fact]
    public void Resume_abertos_fechados_e_total()
    {
        var resumo = WorkItemCounter.Summarize(Alfa());

        Assert.Equal((6L, 1L, 7L), (resumo.Open, resumo.Closed, resumo.Total));
    }

    [Fact]
    public void Conta_por_tipo_com_abertos_e_fechados()
    {
        var resumo = WorkItemCounter.Summarize(Alfa());

        Assert.Collection(resumo.ByType,
            r => Assert.Equal(("Bug", 2L, 1L, 3L), (r.Name, r.Open, r.Closed, r.Total)),
            r => Assert.Equal(("Task", 4L, 0L, 4L), (r.Name, r.Open, r.Closed, r.Total)));
    }

    [Fact]
    public void Conta_por_area_com_abertos_e_fechados()
    {
        var resumo = WorkItemCounter.Summarize(Alfa());

        Assert.Collection(resumo.ByArea,
            r => Assert.Equal(("Alfa\\Backend", 2L, 1L, 3L), (r.Name, r.Open, r.Closed, r.Total)),
            r => Assert.Equal(("Alfa\\Frontend", 4L, 0L, 4L), (r.Name, r.Open, r.Closed, r.Total)));
    }

    [Fact]
    public void Soma_grupos_do_mesmo_tipo_e_da_mesma_area()
    {
        var resumo = WorkItemCounter.Summarize(Origem(false,
            Grupo("Bug", "Proposed", "A", 1),
            Grupo("Bug", "InProgress", "A", 2),
            Grupo("bug", "Completed", "a", 4)));

        var tipo = Assert.Single(resumo.ByType);
        Assert.Equal((3L, 4L), (tipo.Open, tipo.Closed));
        Assert.Single(resumo.ByArea);
    }

    [Fact]
    public void Item_sem_area_vai_para_a_linha_sem_area_e_sem_tipo_para_sem_tipo()
    {
        var resumo = WorkItemCounter.Summarize(Origem(false,
            Grupo("Bug", "Proposed", null, 2),
            Grupo("Bug", "Proposed", " ", 1),
            Grupo(null, "Proposed", "A", 4)));

        Assert.Equal(("(sem área)", 3L), (resumo.ByArea[0].Name, resumo.ByArea[0].Total));
        Assert.Contains(resumo.ByType, r => r.Name == "(sem tipo)" && r.Total == 4);
    }

    [Fact]
    public void Removidos_nao_entram_na_contagem()
    {
        var resumo = WorkItemCounter.Summarize(Origem(false,
            Grupo("Bug", "Removed", "A", 9),
            Grupo("Bug", "Proposed", "A", 1)));

        Assert.Equal(1L, resumo.Total);
        Assert.Equal(1L, Assert.Single(resumo.ByType).Total);
    }

    [Fact]
    public void Ordena_tipos_e_areas_por_nome_em_pt_BR_sem_diferenciar_maiusculas()
    {
        var resumo = WorkItemCounter.Summarize(Origem(false,
            Grupo("task", "Proposed", "zeta", 1),
            Grupo("Épico", "Proposed", "Ágil", 1),
            Grupo("Bug", "Proposed", "alfa", 1)));

        Assert.Equal(["Bug", "Épico", "task"], resumo.ByType.Select(r => r.Name));
        Assert.Equal(["Ágil", "alfa", "zeta"], resumo.ByArea.Select(r => r.Name));
    }

    [Fact]
    public void Sem_grupos_tudo_e_zero()
    {
        var resumo = WorkItemCounter.Summarize(Origem());

        Assert.Equal((0L, 0L, 0L), (resumo.Open, resumo.Closed, resumo.Total));
        Assert.Empty(resumo.ByType);
        Assert.Empty(resumo.ByArea);
        Assert.False(resumo.Partial);
    }

    [Fact]
    public void Repassa_o_indicador_de_contagem_parcial() =>
        Assert.True(WorkItemCounter.Summarize(Origem(parcial: true)).Partial);
}

namespace Reports.DesignSystem.Tests;

public class RaTests
{
    [Theory]
    [InlineData(Provider.AzureDevOps, "ad", "AD", "Azure DevOps")]
    [InlineData(Provider.GitHub, "gh", "GH", "GitHub")]
    [InlineData(Provider.Azure, "az", "AZ", "Azure")]
    [InlineData(Provider.Aws, "aw", "AW", "AWS")]
    public void Cada_fonte_tem_chave_sigla_e_nome(Provider provider, string chave, string sigla, string nome)
    {
        Assert.Equal(chave, Ra.Key(provider));
        Assert.Equal(sigla, Ra.Mark(provider));
        Assert.Equal(nome, Ra.ProviderName(provider));
    }

    [Theory]
    [InlineData(Status.Ok, "ra-badge ra-badge--ok", "Saudável")]
    [InlineData(Status.Warn, "ra-badge ra-badge--warn", "Atenção")]
    [InlineData(Status.Fail, "ra-badge ra-badge--fail", "Falhou")]
    [InlineData(Status.Neutral, "ra-badge", "Sem dados")]
    public void Estado_tem_classe_e_rotulo(Status status, string classe, string rotulo)
    {
        Assert.Equal(classe, Ra.BadgeClass(status));
        Assert.Equal(rotulo, Ra.StatusLabel(status));
    }

    [Theory]
    [InlineData(AlertLevel.Fail, "ra-alert ra-alert--fail", "ra-badge ra-badge--fail", "Crítico")]
    [InlineData(AlertLevel.Warn, "ra-alert ra-alert--warn", "ra-badge ra-badge--warn", "Atenção")]
    [InlineData(AlertLevel.Info, "ra-alert ra-alert--info", "ra-badge ra-badge--brand", "Info")]
    public void Alerta_tem_classe_badge_e_rotulo(AlertLevel nivel, string alerta, string badge, string rotulo)
    {
        Assert.Equal(alerta, Ra.AlertClass(nivel));
        Assert.Equal(badge, Ra.AlertBadgeClass(nivel));
        Assert.Equal(rotulo, Ra.AlertLabel(nivel));
    }

    [Theory]
    [InlineData(KpiTone.Teal, "ra-kpi ra-kpi--teal")]
    [InlineData(KpiTone.Success, "ra-kpi ra-kpi--success")]
    [InlineData(KpiTone.Indigo, "ra-kpi ra-kpi--indigo")]
    [InlineData(KpiTone.Orange, "ra-kpi ra-kpi--orange")]
    public void Kpi_tem_classe_por_tom(KpiTone tom, string classe)
    {
        Assert.Equal(classe, Ra.KpiClass(tom));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(7, "7")]
    [InlineData(999, "999")]
    [InlineData(1204, "1.204")]
    [InlineData(1234567, "1.234.567")]
    [InlineData(-1204, "-1.204")]
    public void Numero_usa_ponto_nos_milhares(long valor, string esperado)
    {
        Assert.Equal(esperado, Ra.Number(valor));
    }

    [Fact]
    public void Data_sai_como_dia_mes_ano_hora_minuto()
    {
        Assert.Equal("26/10/2026 15:42", Ra.DateTime(new DateTime(2026, 10, 26, 15, 42, 59)));
    }

    [Fact]
    public void Formatacao_nao_depende_da_cultura_do_servidor()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            Assert.Equal("1.204", Ra.Number(1204));
            Assert.Equal("26/10/2026 15:42", Ra.DateTime(new DateTime(2026, 10, 26, 15, 42, 0)));
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }
}

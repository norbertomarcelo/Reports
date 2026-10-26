namespace Reports.DesignSystem.Tests;

public class ReportCodeTests
{
    [Fact]
    public void Junta_a_sigla_da_fonte_com_a_data_e_a_hora_em_utc()
    {
        var codigo = ReportCode.Create(Provider.AzureDevOps, new DateTimeOffset(2026, 10, 26, 15, 42, 7, TimeSpan.Zero));

        Assert.Equal("AD-20261026-1542", codigo.Value);
        Assert.Equal("AD-20261026-1542", codigo.ToString());
    }

    [Fact]
    public void Converte_um_horario_com_fuso_para_utc()
    {
        // 23:30 em UTC-3 já é 02:30 do dia seguinte em UTC.
        var codigo = ReportCode.Create(Provider.GitHub, new DateTimeOffset(2026, 10, 26, 23, 30, 0, TimeSpan.FromHours(-3)));

        Assert.Equal("GH-20261027-0230", codigo.Value);
    }

    [Theory]
    [InlineData(Provider.AzureDevOps)]
    [InlineData(Provider.GitHub)]
    [InlineData(Provider.Azure)]
    [InlineData(Provider.Aws)]
    public void So_tem_letras_digitos_e_hifen_para_ir_ao_css_sem_escape(Provider fonte)
    {
        var codigo = ReportCode.Create(fonte, new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero));

        Assert.Matches("^[A-Z]{2}-[0-9]{8}-[0-9]{4}$", codigo.Value);
    }

    [Fact]
    public void Nao_depende_da_cultura_do_servidor()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");

            var codigo = ReportCode.Create(Provider.AzureDevOps, new DateTimeOffset(2026, 10, 26, 15, 42, 0, TimeSpan.Zero));

            Assert.Equal("AD-20261026-1542", codigo.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }
}

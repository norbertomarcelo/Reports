using System.Text;
using Reports.Api.Services.Pdf;

namespace Reports.Api.Tests;

[Trait("Category", "Integration")]
public class PlaywrightPdfRendererTests
{
    [Fact]
    public async Task Gera_pdf_a_partir_de_html()
    {
        await using var renderer = new PlaywrightPdfRenderer();

        var bytes = await renderer.RenderAsync("<h1>Relatório</h1>");

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}

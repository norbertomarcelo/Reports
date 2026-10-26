using Microsoft.Playwright;

namespace Reports.Api.Services.Pdf;

/// <summary>
/// Gera PDF com Chromium headless. O navegador é aberto na primeira chamada e
/// reaproveitado até o encerramento da aplicação.
/// </summary>
public sealed class PlaywrightPdfRenderer : IPdfRenderer, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task<byte[]> RenderAsync(string html)
    {
        var browser = await GetBrowserAsync();
        var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html);

            // Sora e IBM Plex vêm embutidas no HTML (data URI); sem esperar, o PDF sairia com a fonte de reserva.
            await page.EvaluateAsync("() => document.fonts.ready");

            // PreferCSSPageSize: o @page do relatório (A4, margens, capa sem margem) vale mais que o formato daqui.
            // PrintBackground: sem ele o degradê e os fundos do design system somem.
            return await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                PrintBackground = true,
                PreferCSSPageSize = true
            });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null)
            return _browser;

        await _lock.WaitAsync();
        try
        {
            if (_browser is null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync();
            }
            return _browser;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
        _lock.Dispose();
    }
}

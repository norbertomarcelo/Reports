using Razor.Templating.Core;
using Reports.Api.Services.Pdf;

namespace Reports.Api.Services;

public class ReportService
{
    private readonly AzureDevOpsService _azureDevOps;
    private readonly IPdfRenderer _pdfRenderer;

    public ReportService(AzureDevOpsService azureDevOps, IPdfRenderer pdfRenderer)
    {
        _azureDevOps = azureDevOps;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<byte[]> GenerateReportAsync()
    {
        var projects = await _azureDevOps.GetAllProjectsAsync();
        var html = await RazorTemplateEngine.RenderAsync("Views/Index.cshtml", projects);
        return await _pdfRenderer.RenderAsync(html);
    }
}

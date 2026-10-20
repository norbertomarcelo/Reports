using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reports.Api.Dtos.Requests;
using Reports.Api.Services.Pdf;
using Reports.Api.Settings;
using Reports.AzureDevOps.Services;

namespace Reports.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AzureDevOpsController : ControllerBase
{
    private readonly AzureDevOpsReportService _reportService;
    private readonly IPdfRenderer _pdfRenderer;

    public AzureDevOpsController(AzureDevOpsReportService reportService, IPdfRenderer pdfRenderer)
    {
        _reportService = reportService;
        _pdfRenderer = pdfRenderer;
    }

    [HttpPost("/azure-devops")]
    public async Task<IActionResult> GenerateReportAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReportRequest? request)
    {
        if (request is null)
        {
            ModelState.AddModelError("body", "O corpo da requisição é obrigatório.");
            return ValidationProblem(title: ApiBehaviorExtensions.ValidationTitle, modelStateDictionary: ModelState);
        }

        var html = await _reportService.GenerateHtmlAsync(request.Include!);
        var pdf = await _pdfRenderer.RenderAsync(html);
        return File(pdf, "application/pdf", "index.pdf");
    }
}

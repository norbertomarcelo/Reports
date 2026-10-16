using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reports.Api.Dtos.Requests;
using Reports.Api.Services;
using Reports.Api.Settings;

namespace Reports.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AzureDevOpsController : ControllerBase
{
    private readonly ReportService _reportService;

    public AzureDevOpsController(ReportService reportService)
    {
        _reportService = reportService;
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

        var result = await _reportService.GenerateReportAsync(request.Include!);
        return File(result, "application/pdf", "index.pdf");
    }
}

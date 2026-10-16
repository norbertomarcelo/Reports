using Microsoft.AspNetCore.Mvc;
using Reports.Api.Services;

namespace Reports.Api.Controllers;

[Route("[controller]")]
public class AzureDevOpsController : ControllerBase
{
    private readonly ReportService _reportService;

    public AzureDevOpsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("/report")]
    public async Task<IActionResult> GenerateReportAsync()
    {
        var result = await _reportService.GenerateReportAsync();
        return File(result, "application/pdf", "index.pdf");
    }
}

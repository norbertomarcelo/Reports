using Microsoft.AspNetCore.Mvc;
using Reports.Api.Services;

namespace Reports.Api.Controllers;

[Route("[controller]")]
public class AzureDevOpsController : ControllerBase
{
    private readonly AzureDevOpsService _service;

    public AzureDevOpsController(AzureDevOpsService service)
    {
        _service = service;
    }

    [HttpGet("/project")]
    public async Task<IActionResult> ListAllProjectsAsync()
    {
        var result = await _service.GenerateIndexAsync();
        return File(result, "application/pdf", "index.pdf");
    }
}

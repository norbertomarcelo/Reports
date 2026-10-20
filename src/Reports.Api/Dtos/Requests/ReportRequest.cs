using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Reports.AzureDevOps.Dtos.Requests;

namespace Reports.Api.Dtos.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReportRequest
{
    [Required(ErrorMessage = "Informe as seções do relatório em 'include'.")]
    public ReportSections? Include { get; set; }
}

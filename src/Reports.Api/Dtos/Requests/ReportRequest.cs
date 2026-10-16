using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reports.Api.Dtos.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReportRequest
{
    [Required(ErrorMessage = "Informe as seções do relatório em 'include'.")]
    public ReportSections? Include { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReportSections : IValidatableObject
{
    public bool Projects { get; set; }
    public bool Teams { get; set; }
    public bool Users { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Projects && !Teams && !Users)
            yield return new ValidationResult("Selecione ao menos uma seção do relatório.");
    }
}

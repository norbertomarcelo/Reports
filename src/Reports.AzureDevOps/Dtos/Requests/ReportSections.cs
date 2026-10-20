using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reports.AzureDevOps.Dtos.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReportSections : IValidatableObject
{
    public bool Projects { get; set; }
    public bool Teams { get; set; }
    public bool Users { get; set; }
    public bool WorkItems { get; set; }
    public bool Repositories { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Projects && !Teams && !Users && !WorkItems && !Repositories)
            yield return new ValidationResult("Selecione ao menos uma seção do relatório.");
    }
}

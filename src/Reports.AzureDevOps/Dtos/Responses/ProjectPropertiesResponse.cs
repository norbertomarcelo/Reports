using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class ProjectPropertiesResponse
{
    public int Count { get; set; }
    public List<ProjectProperty> Value { get; set; }
}

using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class ProjectsResponse
{
    public int Count { get; set; }
    public List<Project> Value { get; set; }
}

using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class ProjectsResponse
{
    public int Count { get; set; }
    public List<Project> Value { get; set; }
}

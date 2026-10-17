using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class ProjectPropertiesResponse
{
    public int Count { get; set; }
    public List<ProjectProperty> Value { get; set; }
}

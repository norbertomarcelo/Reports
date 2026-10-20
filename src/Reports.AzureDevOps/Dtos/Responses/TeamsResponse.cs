using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class TeamsResponse
{
    public int Count { get; set; }
    public List<Team> Value { get; set; }
}

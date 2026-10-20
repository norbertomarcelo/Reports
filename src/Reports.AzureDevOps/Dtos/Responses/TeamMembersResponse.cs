using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class TeamMembersResponse
{
    public int Count { get; set; }
    public List<TeamMember> Value { get; set; }
}

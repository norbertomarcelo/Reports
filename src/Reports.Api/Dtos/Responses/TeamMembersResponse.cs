using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class TeamMembersResponse
{
    public int Count { get; set; }
    public List<TeamMember> Value { get; set; }
}

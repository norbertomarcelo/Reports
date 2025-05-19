using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class TeamsResponse
{
    public int Count { get; set; }
    public List<Team> Value { get; set; }
}

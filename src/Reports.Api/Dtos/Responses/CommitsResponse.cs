using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class CommitsResponse
{
    public int Count { get; set; }
    public List<GitCommit> Value { get; set; } = [];
}

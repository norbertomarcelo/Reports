using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class CommitsResponse
{
    public int Count { get; set; }
    public List<GitCommit> Value { get; set; } = [];
}

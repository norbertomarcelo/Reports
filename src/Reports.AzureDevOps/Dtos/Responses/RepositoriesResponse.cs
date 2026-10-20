using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class RepositoriesResponse
{
    public int Count { get; set; }
    public List<GitRepository> Value { get; set; } = [];
}

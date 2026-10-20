using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class RepositoriesResponse
{
    public int Count { get; set; }
    public List<GitRepository> Value { get; set; } = [];
}

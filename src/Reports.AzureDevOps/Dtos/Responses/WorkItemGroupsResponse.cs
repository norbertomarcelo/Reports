using System.Text.Json.Serialization;
using Reports.AzureDevOps.Models;

namespace Reports.AzureDevOps.Dtos.Responses;

public class WorkItemGroupsResponse
{
    public List<WorkItemGroup> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

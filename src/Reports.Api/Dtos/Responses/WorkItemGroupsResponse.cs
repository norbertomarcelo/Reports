using System.Text.Json.Serialization;
using Reports.Api.Models;

namespace Reports.Api.Dtos.Responses;

public class WorkItemGroupsResponse
{
    public List<WorkItemGroup> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

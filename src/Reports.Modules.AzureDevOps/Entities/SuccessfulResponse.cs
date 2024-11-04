namespace Reports.Modules.AzureDevOps.Entities;

public class SuccessfulResponse
{
    public int Count { get; set; }
    public List<Project> Value { get; set; }
}
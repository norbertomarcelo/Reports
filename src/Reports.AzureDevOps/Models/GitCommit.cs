namespace Reports.AzureDevOps.Models;

public class GitCommit
{
    public string CommitId { get; set; } = string.Empty;
    public GitUserDate? Author { get; set; }
    public GitUserDate? Committer { get; set; }
}

public class GitUserDate
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime Date { get; set; }
}

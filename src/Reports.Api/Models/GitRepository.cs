namespace Reports.Api.Models;

public class GitRepository
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Ausente em repositório vazio (ex.: <c>refs/heads/main</c>).</summary>
    public string? DefaultBranch { get; set; }

    public bool IsDisabled { get; set; }
    public GitRepositoryProject? Project { get; set; }
}

public class GitRepositoryProject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

namespace Reports.Modules.AzureDevOps.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string State { get; set; }
    public int Revision { get; set; }
    public string Visibility { get; set; }
    public DateTime LastUpdateTime { get; set; }

    public Project(
        Guid id,
        string name,
        string url,
        string state,
        int revision,
        string visibility,
        DateTime lastUpdateTime)
    {
        Id = id;
        Name = name;
        Url = url;
        State = state;
        Revision = revision;
        Visibility = visibility;
        LastUpdateTime = lastUpdateTime;
    }
}
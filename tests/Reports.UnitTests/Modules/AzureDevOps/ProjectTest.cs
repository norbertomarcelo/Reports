using FluentAssertions;
using Reports.Modules.AzureDevOps.Entities;

namespace Reports.UnitTests.Modules.AzureDevOps;

public class ProjectTest
{
    [Fact(DisplayName = nameof(Instantiate))]
    [Trait("Azure DevOps", "Project")]
    public void Instantiate()
    {
        var validObject = new
        {
            Id = Guid.NewGuid(),
            Name = "Project Name",
            Url = "https://dev.azure.com/MNS-Tech/_apis/projects/8872fec3-3b30-4921-8cfe-63c1ae22a6d0",
            State = "wellFormed",
            Revision = 40,
            Visibility = "public",
            LastUpdateDate = DateTime.Now
        };

        var project = new Project(
            validObject.Id,
            validObject.Name,
            validObject.Url,
            validObject.State,
            validObject.Revision,
            validObject.Visibility,
            validObject.LastUpdateDate);

        project.Id.Should().Be(validObject.Id);
        project.Name.Should().Be(validObject.Name);
        project.Url.Should().Be(validObject.Url);
        project.State.Should().Be(validObject.State);
        project.Revision.Should().Be(validObject.Revision);
        project.Visibility.Should().Be(validObject.Visibility);
        project.LastUpdateTime.Should().Be(validObject.LastUpdateDate);
    }
}
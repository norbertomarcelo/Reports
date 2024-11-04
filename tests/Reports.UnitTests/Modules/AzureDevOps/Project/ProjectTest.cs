using FluentAssertions;
using AzureDevOpsEntities = Reports.Modules.AzureDevOps.Entities;

namespace Reports.UnitTests.Modules.AzureDevOps.Project;

[Collection(nameof(ProjectTestFixture))]
public class ProjectTest
{
    private readonly ProjectTestFixture _fixture;

    public ProjectTest(ProjectTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = nameof(Instantiate_ShouldCreateProjectWithValidProperties))]
    [Trait("Azure DevOps", "Project")]
    public void Instantiate_ShouldCreateProjectWithValidProperties()
    {
        var validObject = new
        {
            Id = _fixture.GetValidId(),
            Name = _fixture.GetValidName(),
            Url = _fixture.GetValidUrl(),
            State = _fixture.GetValidState(),
            Revision = _fixture.GetValidRevision(),
            Visibility = _fixture.GetValidVisibility(),
            LastUpdateDate = _fixture.GetValidDate(),
        };

        var project = new AzureDevOpsEntities.Project(
            validObject.Id,
            validObject.Name,
            validObject.Url,
            validObject.State,
            validObject.Revision,
            validObject.Visibility,
            validObject.LastUpdateDate);

        project.Should().NotBeNull();
        project.Id.Should().Be(validObject.Id);
        project.Name.Should().Be(validObject.Name);
        project.Url.Should().Be(validObject.Url);
        project.State.Should().Be(validObject.State);
        project.Revision.Should().Be(validObject.Revision);
        project.Visibility.Should().Be(validObject.Visibility);
        project.LastUpdateTime.Should().Be(validObject.LastUpdateDate);
    }
    
    [Fact(DisplayName = nameof(Instantiate_ShouldCreateProjectWithValidTypes))]
    [Trait("Azure DevOps", "Project")]
    public void Instantiate_ShouldCreateProjectWithValidTypes()
    {
        var validObject = new
        {
            Id = _fixture.GetValidId(),
            Name = _fixture.GetValidName(),
            Url = _fixture.GetValidUrl(),
            State = _fixture.GetValidState(),
            Revision = _fixture.GetValidRevision(),
            Visibility = _fixture.GetValidVisibility(),
            LastUpdateDate = _fixture.GetValidDate(),
        };

        var project = new AzureDevOpsEntities.Project(
            validObject.Id,
            validObject.Name,
            validObject.Url,
            validObject.State,
            validObject.Revision,
            validObject.Visibility,
            validObject.LastUpdateDate);
        
        project.Should().BeOfType<AzureDevOpsEntities.Project>();
        project.Id.GetType().Should().BeAssignableTo<Guid>();
        project.Name.Should().BeOfType<string>();
        project.Url.Should().BeOfType<string>();
        project.State.Should().BeOfType<string>();
        project.Revision.GetType().Should().BeAssignableTo<int>();
        project.LastUpdateTime.GetType().Should().BeAssignableTo<DateTime>();
    }
}
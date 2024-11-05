using FluentAssertions;
using Reports.Modules.AzureDevOps.Entities;
using AzureDevOpsServices = Reports.Modules.AzureDevOps.Services;

namespace Reports.IntegrationTests.Modules.AzureDevOps.ProjectService;

[Collection(nameof(ProjectServiceTestFixture))]
public class ProjectServiceTest
{
    private readonly AzureDevOpsServices.ProjectService _projectService;
    private readonly ProjectServiceTestFixture _fixture;

    public ProjectServiceTest(ProjectServiceTestFixture fixture)
    {
        _fixture = fixture;

        _projectService = new AzureDevOpsServices.ProjectService(
            "",
            "",
            "");
    }

    [Fact(DisplayName = nameof(GetRequest_ShouldReturnSuccess))]
    [Trait("Azure DevOps", "ProjectService")]
    public async Task GetRequest_ShouldReturnSuccess()
    {
        var response = await _projectService.Request();

        response.GetType().Should().BeAssignableTo<SuccessfulResponse>();
        response.Value.GetType().Should().BeAssignableTo<List<Project>>();
    }

    [Fact(DisplayName = nameof(GetRequest_ProjectList_ReturnsProjectList))]
    [Trait("Azure DevOps", "ProjectService")]
    public async Task GetRequest_ProjectList_ReturnsProjectList()
    {
        var response = await _projectService.Request();
        var projects = response.Value.ToList();
        var project = projects.FirstOrDefault();

        projects.GetType().Should().BeAssignableTo<List<Project>>();
        project.Should().BeOfType<Project>();
        project?.Id.GetType().Should().BeAssignableTo<Guid>();
        project?.Name.Should().BeOfType<string>();
        project?.Url.Should().BeOfType<string>();
        project?.State.Should().BeOfType<string>();
        project?.Revision.GetType().Should().BeAssignableTo<int>();
        project?.LastUpdateTime.GetType().Should().BeAssignableTo<DateTime>();
    }

    [Fact(DisplayName = nameof(ToCsv_ShouldCreateCsvString))]
    [Trait("Azure DevOps", "ProjectService")]
    public void ToCsv_ShouldCreateCsvString()
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

        var projectList = new List<Project>();

        projectList.Add(new Project(
            validObject.Id,
            validObject.Name,
            validObject.Url,
            validObject.State,
            validObject.Revision,
            validObject.Visibility,
            validObject.LastUpdateDate));

        var expectedCsv =
            "Id,Name,Url,State,Revision,Visibility,LastUpdateTime\n" +
            $"{validObject.Id},{validObject.Name},{validObject.Url},{validObject.State},{validObject.Revision},{validObject.Visibility},{validObject.LastUpdateDate}";

        var csvContent = _projectService.ToCsvString(projectList);

        csvContent.Should().Be(expectedCsv);
    }
}

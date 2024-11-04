using FluentAssertions;
using Reports.Modules.AzureDevOps.Entities;
using AzureDevOpsServices = Reports.Modules.AzureDevOps.Services;

namespace Reports.IntegrationTests.Modules.AzureDevOps.ProjectService;

public class ProjectServiceTest
{
    private readonly AzureDevOpsServices.ProjectService _projectService;

    public ProjectServiceTest()
    {
        _projectService = new AzureDevOpsServices.ProjectService(
            "",
            "",
            "");
    }

    [Fact(DisplayName = nameof(GetRequest_ShouldReturnSuccess))]
    [Trait("Azure DevOps", "ProjectService")]
    public async Task GetRequest_ShouldReturnSuccess()
    {
        var response = await _projectService.GetProjectsList();
        
        response.GetType().Should().BeAssignableTo<SuccessfulResponse>();
        response.Value.GetType().Should().BeAssignableTo<List<Project>>();
    }
    
    [Fact(DisplayName = nameof(GetRequest_ProjectList_ReturnsProjectList))]
    [Trait("Azure DevOps", "ProjectService")]
    public async Task GetRequest_ProjectList_ReturnsProjectList()
    {
        var response = await _projectService.GetProjectsList();
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
}
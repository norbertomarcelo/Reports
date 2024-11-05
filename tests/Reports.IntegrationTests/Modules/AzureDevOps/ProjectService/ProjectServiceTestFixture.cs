using Reports.IntegrationTests.Common;

namespace Reports.IntegrationTests.Modules.AzureDevOps.ProjectService;

public class ProjectServiceTestFixture : BaseFixture
{
    public ProjectServiceTestFixture() : base()
    {
    }
    
    public Guid GetValidId() => Guid.NewGuid();
    public string GetValidName() => Faker.Lorem.Letter(10);
    public string GetValidUrl() => Faker.Internet.Url();
    public string GetValidState() => Faker.Lorem.Letter(10);
    public int GetValidRevision() => Faker.Random.Number(99);
    public string GetValidVisibility() => Faker.Lorem.Letter(10);

    public DateTime GetValidDate()
    {
        return  DateTime.ParseExact(
            DateTime.Now.ToString("yyyyMMdd"), 
            "yyyyMMdd", 
            System.Globalization.CultureInfo.InvariantCulture
        );
    }
}

[CollectionDefinition(nameof(ProjectServiceTestFixture))]
public class ProjectServiceTestFixtureCollection
    : ICollectionFixture<ProjectServiceTestFixture>
{
}
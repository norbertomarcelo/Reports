using Reports.UnitTests.Common;

namespace Reports.UnitTests.Modules.AzureDevOps.Project;

public class ProjectTestFixture : BaseFixture
{
    public ProjectTestFixture() : base()
    {
    }

    public Guid GetValidId() => Guid.NewGuid();
    public string GetValidName() => Faker.Commerce.ProductName();
    public string GetValidUrl() => Faker.Internet.Url();
    public string GetValidState() => Faker.Lorem.Letter(20);
    public int GetValidRevision() => Faker.Random.Number(99);
    public string GetValidVisibility() => Faker.Lorem.Letter(20);
    public DateTime GetValidDate() => DateTime.Now;
}

[CollectionDefinition(nameof(ProjectTestFixture))]
public class ProjectTestFixtureCollection
    : ICollectionFixture<ProjectTestFixture>
{
}
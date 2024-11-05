using Bogus;

namespace Reports.IntegrationTests.Common;

public class BaseFixture
{
    public Faker Faker { get; set; }

    protected BaseFixture()
    {
        Faker = new Faker("pt_BR");
    }
}
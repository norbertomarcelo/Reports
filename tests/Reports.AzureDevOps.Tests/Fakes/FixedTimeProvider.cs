namespace Reports.AzureDevOps.Tests.Fakes;

/// <summary>Relógio parado, para o código e a data da capa do relatório não mudarem entre execuções.</summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

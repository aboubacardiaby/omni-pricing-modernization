namespace Pricing.UnitTests;

using Xunit;

public sealed class BaselineTests
{
    [Fact]
    public void DomainAssemblyIsAvailable() => Assert.NotNull(typeof(Domain.AssemblyMarker).Assembly);
}

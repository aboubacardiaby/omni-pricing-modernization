namespace Pricing.ParityTests;

using Xunit;

public sealed class BaselineTests
{
    [Fact]
    public void ApplicationAssemblyIsAvailable() => Assert.NotNull(typeof(Application.AssemblyMarker).Assembly);
}

namespace Pricing.CharacterizationTests;

using Xunit;

public sealed class BaselineTests
{
    [Fact]
    public void CompatibilityAssemblyIsAvailable() => Assert.NotNull(typeof(Compatibility.AssemblyMarker).Assembly);
}

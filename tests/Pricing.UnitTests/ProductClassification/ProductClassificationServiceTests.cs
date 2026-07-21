namespace Pricing.UnitTests.ProductClassification;

using Pricing.Application.ProductClassification;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class ProductClassificationServiceTests
{
    [Fact]
    public async Task MissingVendorReturns61001BeforeRepositoryCall()
    {
        var repository = new StubRepository("O");

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync(" ", "P0000001", CancellationToken.None);

        ValidationPricingError error = Assert.IsType<ValidationPricingError>(result.Error);
        Assert.Equal("61001", error.LegacyErrorCode);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task MissingProductReturns61004BeforeRepositoryCall()
    {
        var repository = new StubRepository("O");

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", null, CancellationToken.None);

        ValidationPricingError error = Assert.IsType<ValidationPricingError>(result.Error);
        Assert.Equal("61004", error.LegacyErrorCode);
        Assert.Equal(0, repository.CallCount);
    }

    [Theory]
    [InlineData("R")]
    [InlineData(" ")]
    [InlineData("X")]
    public async Task OtherLegacyTypesNormalizeToRegular(string legacyType)
    {
        var repository = new StubRepository(legacyType);

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", "P0000001", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductType.Regular, result.ProductType);
        Assert.Equal("R", result.LegacyProductTypeCode);
    }

    [Fact]
    public async Task OwensKitTypeOIsPreserved()
    {
        var repository = new StubRepository("O");

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", "P0000001", CancellationToken.None);

        Assert.Equal(ProductType.Kit, result.ProductType);
        Assert.Equal("O", result.LegacyProductTypeCode);
    }

    [Fact]
    public async Task SupplierKitTypeSIsPreserved()
    {
        var repository = new StubRepository("S");

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", "P0000001", CancellationToken.None);

        Assert.Equal(ProductType.SupplierKit, result.ProductType);
        Assert.Equal("S", result.LegacyProductTypeCode);
    }

    [Fact]
    public async Task NotFoundReturns61603()
    {
        var repository = new StubRepository((string?)null);

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", "P0000001", CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(result.Error);
        Assert.Equal("61603", error.LegacyErrorCode);
    }

    [Fact]
    public async Task DatabaseFailureReturns61602AndRetainsTransientClassification()
    {
        var repository = new StubRepository(new ProductClassificationRepositoryException("DB2", true));

        ProductClassificationResult result = await new ProductClassificationService(repository)
            .ClassifyAsync("V001", "P0000001", CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(result.Error);
        Assert.Equal("61602", error.LegacyErrorCode);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository("O");

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new ProductClassificationService(repository)
                .ClassifyAsync("V001", "P0000001", cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private sealed class StubRepository : IProductClassificationRepository
    {
        private readonly string? result;
        private readonly Exception? exception;

        internal StubRepository(string? result) => this.result = result;
        internal StubRepository(Exception exception) => this.exception = exception;

        internal int CallCount { get; private set; }

        public ValueTask<string?> FindLegacyProductTypeAsync(
            VendorId vendor,
            ProductId product,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(result)
                : ValueTask.FromException<string?>(exception);
        }
    }
}

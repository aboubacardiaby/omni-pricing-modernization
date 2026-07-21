namespace Pricing.UnitTests.ProductInformation;

using Pricing.Application.ProductInformation;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class ProductInformationServiceTests
{
    [Fact]
    public async Task BaseUomUsesFactorOneWithoutAlternateLookupResult()
    {
        var repository = new StubRepository(CreateData(alternateFactor: null, alternateFound: false));

        ProductInformationResult result = await CreateService(repository)
            .GetAsync("V001", "P0000001", "EA", "01", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1m, result.Product!.AlternateConversionFactor);
        Assert.Null(result.Product.AlternateUnitOfMeasure);
    }

    [Fact]
    public async Task AlternateUomReturnsConfirmedConversionAndDates()
    {
        var repository = new StubRepository(CreateData(12.50000000m));

        ProductInformationResult result = await CreateService(repository)
            .GetAsync("V001", "P0000001", "CS", "01", CancellationToken.None);

        Assert.Equal("CS", result.Product!.AlternateUnitOfMeasure?.Value);
        Assert.Equal(12.50000000m, result.Product.AlternateConversionFactor);
        Assert.Equal(1234, result.Product.ProductCategory);
        Assert.Equal("A", result.Product.InventoryClass);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Product.ProductCategoryEffectiveDate);
        Assert.Equal(new DateOnly(2026, 12, 31), result.Product.ProductCategoryExpirationDate);
    }

    [Fact]
    public async Task BlankBranchSkipsInventoryClassOutput()
    {
        var repository = new StubRepository(CreateData(12m));

        ProductInformationResult result = await CreateService(repository)
            .GetAsync("V001", "P0000001", "CS", null, CancellationToken.None);

        Assert.Null(result.Product!.InventoryClass);
        Assert.Null(repository.LastDivision);
    }

    [Fact]
    public async Task MissingCategoryIsWarningAndProcessingContinues()
    {
        ProductInformationData data = CreateData(12m) with { ProductCategoryFound = false, ProductCategory = null };

        ProductInformationResult result = await CreateService(new StubRepository(data))
            .GetAsync("V001", "P0000001", "CS", "01", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PRODUCT_CATEGORY_NOT_FOUND", Assert.Single(result.Warnings).Code);
    }

    [Fact]
    public async Task MissingAlternateUomReturns61606()
    {
        var repository = new StubRepository(CreateData(null, false));

        ProductInformationResult result = await CreateService(repository)
            .GetAsync("V001", "P0000001", "CS", "01", CancellationToken.None);

        Assert.Equal("61606", Assert.IsType<MissingDataPricingError>(result.Error).LegacyErrorCode);
    }

    [Fact]
    public async Task ProductNotFoundReturns61603()
    {
        var repository = new StubRepository((ProductInformationData?)null);

        ProductInformationResult result = await CreateService(repository)
            .GetAsync("V001", "P0000001", null, null, CancellationToken.None);

        Assert.Equal("61603", Assert.IsType<MissingDataPricingError>(result.Error).LegacyErrorCode);
    }

    [Fact]
    public async Task RepositoryFailurePreservesConfirmedLegacyCode()
    {
        var failure = new ProductInformationRepositoryException("61607", "SQL ERROR IN SELCT VNG05", true);

        ProductInformationResult result = await CreateService(new StubRepository(failure))
            .GetAsync("V001", "P0000001", "CS", "01", CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(result.Error);
        Assert.Equal("61607", error.LegacyErrorCode);
        Assert.True(error.IsTransient);
    }

    [Theory]
    [InlineData(null, "P0000001", "61301")]
    [InlineData("V001", " ", "61304")]
    public async Task ValidatesRequiredKeysBeforeRepository(string? vendor, string? product, string legacyCode)
    {
        var repository = new StubRepository(CreateData(null));

        ProductInformationResult result = await CreateService(repository)
            .GetAsync(vendor, product, null, null, CancellationToken.None);

        Assert.Equal(legacyCode, Assert.IsType<ValidationPricingError>(result.Error).LegacyErrorCode);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeRepositoryCall()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository(CreateData(null));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await CreateService(repository).GetAsync("V001", "P0000001", null, null, cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static ProductInformationService CreateService(IProductInformationRepository repository) => new(repository);

    private static ProductInformationData CreateData(decimal? alternateFactor, bool alternateFound = true) =>
        new(
            "R",
            new UnitOfMeasure("EA"),
            1234,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "A",
            alternateFactor,
            true,
            alternateFound);

    private sealed class StubRepository : IProductInformationRepository
    {
        private readonly ProductInformationData? data;
        private readonly Exception? exception;

        internal StubRepository(ProductInformationData? data) => this.data = data;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }
        internal DivisionId? LastDivision { get; private set; }

        public ValueTask<ProductInformationData?> FindAsync(
            VendorId vendor,
            ProductId product,
            UnitOfMeasure? requestedUnitOfMeasure,
            DivisionId? division,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastDivision = division;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(data)
                : ValueTask.FromException<ProductInformationData?>(exception);
        }
    }
}

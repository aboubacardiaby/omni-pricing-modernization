namespace Pricing.UnitTests.Models;

using System.Collections.Immutable;
using System.Text.Json;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PricingModelsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RequestContextAndResultRemainSeparateModels()
    {
        var request = CreateRequest();
        var product = CreateProduct();
        var customer = CreateCustomer();
        var context = new PricingContext(request, product, customer, null, null);
        var result = new PricingResult(
            product.ProductType,
            null,
            null,
            null,
            null,
            null,
            [],
            [],
            [],
            []);

        Assert.Same(request, context.Request);
        Assert.Same(product, context.Product);
        Assert.Same(customer, context.Customer);
        Assert.Null(context.ContractSelection);
        Assert.Empty(result.Components);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void SelectionsAndComponentsRetainProvenance()
    {
        var dates = new PricingDateRange(new DateOnly(2026, 7, 20), new DateOnly(2026, 12, 31));
        var provenance = new RuleProvenance(
            "Individual contract selection",
            "A6U01.CBL/0195-PRO-COST-CONT-010",
            "Individual",
            dates);
        var contract = new ContractSelection(
            new ContractId("CONTRACT-123"),
            "Individual",
            new Money(12.34567891m),
            new UnitOfMeasure("EA"),
            provenance);
        var sell = new SellArrangementSelection("SELL-1", "Stated", provenance);
        var component = new PriceComponent("Base cost", PriceComponentType.BaseCost, contract.UnitCost, provenance);

        Assert.Same(provenance, contract.Provenance);
        Assert.Same(provenance, sell.Provenance);
        Assert.Same(provenance, component.Provenance);
        Assert.Equal("A6U01.CBL/0195-PRO-COST-CONT-010", component.Provenance.Source);
    }

    [Fact]
    public void CompleteModelGraphRoundTripsThroughJson()
    {
        var dates = new PricingDateRange(new DateOnly(2026, 7, 20), new DateOnly(2026, 12, 31));
        var provenance = new RuleProvenance("Rule", "Source", "Account", dates);
        var contract = new ContractSelection(
            new ContractId("CONTRACT-123"),
            "Individual",
            new Money(12.34567891m),
            new UnitOfMeasure("EA"),
            provenance);
        var sell = new SellArrangementSelection("SELL-1", "Stated", provenance);
        var result = new PricingResult(
            ProductType.Regular,
            contract.UnitCost,
            new Money(15.00000000m),
            dates.ExpirationDate,
            contract,
            sell,
            [new PriceComponent("Base cost", PriceComponentType.BaseCost, contract.UnitCost, provenance)],
            [provenance],
            [new PricingWarning("WARNING", "Test warning")],
            [new MissingDataPricingError("MISSING", "Missing dependency", "70", "Product")]);

        var roundTripped = JsonSerializer.Deserialize<PricingResult>(
            JsonSerializer.Serialize(result, JsonOptions),
            JsonOptions);

        Assert.NotNull(roundTripped);
        Assert.Equal(result.ProductType, roundTripped.ProductType);
        Assert.Equal(result.Cost, roundTripped.Cost);
        Assert.Equal(result.SellPrice, roundTripped.SellPrice);
        Assert.Equal(result.ExpirationDate, roundTripped.ExpirationDate);
        Assert.Equal(result.ContractSelection, roundTripped.ContractSelection);
        Assert.Equal(result.SellArrangementSelection, roundTripped.SellArrangementSelection);
        Assert.True(result.Components.SequenceEqual(roundTripped.Components));
        Assert.True(result.Provenance.SequenceEqual(roundTripped.Provenance));
        Assert.True(result.Warnings.SequenceEqual(roundTripped.Warnings));
        Assert.True(result.Errors.SequenceEqual(roundTripped.Errors));
        Assert.IsType<MissingDataPricingError>(roundTripped!.Errors.Single());
    }

    [Fact]
    public void PricingContextRoundTripsThroughJson()
    {
        var expected = new PricingContext(CreateRequest(), CreateProduct(), CreateCustomer(), null, null);

        var actual = JsonSerializer.Deserialize<PricingContext>(
            JsonSerializer.Serialize(expected, JsonOptions),
            JsonOptions);

        Assert.NotNull(actual);
        Assert.Equal(expected.Request, actual.Request);
        Assert.Equal(expected.Product, actual.Product);
        Assert.Equal(expected.Customer.Account, actual.Customer.Account);
        Assert.Equal(expected.Customer.Customer, actual.Customer.Customer);
        Assert.True(expected.Customer.BuyingGroupMemberships.SequenceEqual(actual.Customer.BuyingGroupMemberships));
        Assert.Equal(expected.ContractSelection, actual.ContractSelection);
        Assert.Equal(expected.SellArrangementSelection, actual.SellArrangementSelection);
    }

    [Fact]
    public void ErrorKindsRemainDistinctAndCarryLegacyCodes()
    {
        ImmutableArray<PricingError> errors =
        [
            new ValidationPricingError("REQUIRED", "Division is required", "8", "division"),
            new MissingDataPricingError("NOT_FOUND", "Product not found", "6", "product"),
            new DependencyPricingError("DB2", "Database unavailable", "602", true, "70"),
            new UnsupportedBehaviorPricingError("BLOCKED", "Behavior is not confirmed", null, "A6O013U")
        ];

        var roundTripped = JsonSerializer.Deserialize<ImmutableArray<PricingError>>(
            JsonSerializer.Serialize(errors, JsonOptions),
            JsonOptions);

        Assert.Collection(
            roundTripped,
            error => Assert.IsType<ValidationPricingError>(error),
            error => Assert.IsType<MissingDataPricingError>(error),
            error => Assert.IsType<DependencyPricingError>(error),
            error => Assert.IsType<UnsupportedBehaviorPricingError>(error));
        Assert.Equal("8", roundTripped[0].LegacyErrorCode);
        Assert.True(Assert.IsType<DependencyPricingError>(roundTripped[2]).IsTransient);
        Assert.Equal("70", roundTripped[2].LegacySeverityCode);
    }

    private static PricingRequest CreateRequest() => new(
        new DivisionId("01"),
        new AccountNumber("123456"),
        new VendorId("1234"),
        new ProductId("ABC123"),
        new Quantity(2m),
        new UnitOfMeasure("EA"),
        "001",
        "001",
        new DateOnly(2026, 7, 20),
        PricingRequestType.Full);

    private static ProductInformation CreateProduct() => new(
        new VendorId("1234"),
        new ProductId("ABC123"),
        ProductType.Regular,
        new UnitOfMeasure("EA"),
        new UnitOfMeasure("CS"),
        12m,
        100,
        "A");

    private static CustomerInformation CreateCustomer() => new(
        new AccountNumber("123456"),
        new CustomerNumber(1234567890),
        [
            new BuyingGroupMembership(
                100,
                null,
                1,
                new PricingDateRange(new DateOnly(2026, 1, 1)),
                "CUP100")
        ]);
}

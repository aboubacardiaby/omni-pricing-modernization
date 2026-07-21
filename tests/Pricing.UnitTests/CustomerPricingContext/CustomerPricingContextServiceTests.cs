namespace Pricing.UnitTests.CustomerPricingContext;

using System.Collections.Immutable;
using Pricing.Application.CustomerPricingContext;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class CustomerPricingContextServiceTests
{
    [Fact]
    public async Task ReturnsCompleteCustomerAggregateWithoutApplyingRules()
    {
        var dates = new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        BuyingGroupMembership[] memberships =
        [
            new(100, 200, 1, dates, "CUG11"),
            new(200, null, 2, dates, "BGG03"),
        ];
        ContractExclusion[] exclusions = [new(true, "account", "CCG25")];
        CustomerFeeConfiguration[] fees =
        [
            new("PN", "PANDAC", CustomerFeeType.PercentageOfSales, 1.25m, new Money(2m),
                "SKU1", "DL", null, dates, new Money(0m), 0m, "CUTFEE_PRICE"),
        ];
        var lowUom = new LowUnitOfMeasureConfiguration(true, 100, 1, 2.5m, false, dates.EffectiveDate, "BGG23");
        var freight = new CustomerFreightConfiguration(true, true, false, false, false, false, 100, "CUG53");
        CustomerPriceComponentConfiguration[] components =
        [
            new("SF", "FREIGHT", "SKU2", "MA", "PO1", dates, "CUTPRICE_COMPONENT"),
        ];
        var data = new CustomerPricingContextData(
            new CustomerNumber(12345),
            [.. memberships],
            [.. exclusions],
            [.. fees],
            lowUom,
            freight,
            [.. components]);
        var repository = new StubRepository(data);

        CustomerPricingContextResult result = await new CustomerPricingContextService(repository)
            .GetAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        CustomerInformation customer = result.Customer!;
        Assert.Equal("123456", customer.Account.Value);
        Assert.Equal(12345, customer.Customer?.Value);
        Assert.Equal(memberships, customer.BuyingGroupMemberships);
        Assert.Equal(200, customer.BuyingGroupMemberships[0].ParentBuyingGroupId);
        Assert.Equal(1, customer.BuyingGroupMemberships[0].Priority);
        Assert.Equal(exclusions, customer.ContractExclusions);
        Assert.Equal(fees, customer.Fees);
        Assert.Same(lowUom, customer.LowUnitOfMeasure);
        Assert.Same(freight, customer.Freight);
        Assert.Equal(components, customer.PriceComponents);
        Assert.Equal("001", repository.LastShipTo);
        Assert.Equal("002", repository.LastBillTo);
        Assert.Equal(new DateOnly(2026, 7, 21), repository.LastPricingDate);
    }

    [Fact]
    public async Task DefaultCollectionsAreNormalizedToEmpty()
    {
        var data = new CustomerPricingContextData(
            new CustomerNumber(1), default, default, default, null, null, default);

        CustomerPricingContextResult result = await new CustomerPricingContextService(new StubRepository(data))
            .GetAsync(CreateRequest(), CancellationToken.None);

        Assert.Empty(result.Customer!.BuyingGroupMemberships);
        Assert.Empty(result.Customer.ContractExclusions);
        Assert.Empty(result.Customer.Fees);
        Assert.Empty(result.Customer.PriceComponents);
    }

    [Fact]
    public async Task MissingAccountReturnsConfirmedCup100Error()
    {
        CustomerPricingContextResult result = await new CustomerPricingContextService(
                new StubRepository((CustomerPricingContextData?)null))
            .GetAsync(CreateRequest(), CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(result.Error);
        Assert.Equal("106", error.LegacyErrorCode);
        Assert.Equal("2", error.LegacySeverityCode);
    }

    [Fact]
    public async Task MissingActiveCustomerReturnsConfirmedCup100Error()
    {
        var data = new CustomerPricingContextData(
            null, [], [], [], null, null, [], ActiveCustomerFound: false);

        CustomerPricingContextResult result = await new CustomerPricingContextService(new StubRepository(data))
            .GetAsync(CreateRequest(), CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(result.Error);
        Assert.Equal("108", error.LegacyErrorCode);
        Assert.Equal("3", error.LegacySeverityCode);
    }

    [Fact]
    public async Task RepositoryFailurePreservesIndependentLegacyErrorFields()
    {
        var exception = new CustomerPricingContextRepositoryException("DB ERROR", "10", "90", true);

        CustomerPricingContextResult result = await new CustomerPricingContextService(new StubRepository(exception))
            .GetAsync(CreateRequest(), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(result.Error);
        Assert.Equal("10", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeRepositoryCall()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository((CustomerPricingContextData?)null);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new CustomerPricingContextService(repository).GetAsync(CreateRequest(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static PricingRequest CreateRequest() => new(
        new DivisionId("01"),
        new AccountNumber("123456"),
        new VendorId("V001"),
        new ProductId("P0000001"),
        new Quantity(1m),
        new UnitOfMeasure("EA"),
        "001",
        "002",
        new DateOnly(2026, 7, 21),
        PricingRequestType.Full);

    private sealed class StubRepository : ICustomerPricingContextRepository
    {
        private readonly CustomerPricingContextData? data;
        private readonly Exception? exception;

        internal StubRepository(CustomerPricingContextData? data) => this.data = data;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }
        internal string? LastShipTo { get; private set; }
        internal string? LastBillTo { get; private set; }
        internal DateOnly LastPricingDate { get; private set; }

        public ValueTask<CustomerPricingContextData?> FindAsync(
            DivisionId division,
            AccountNumber account,
            string? shipTo,
            string? billTo,
            DateOnly pricingDate,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastShipTo = shipTo;
            LastBillTo = billTo;
            LastPricingDate = pricingDate;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(data)
                : ValueTask.FromException<CustomerPricingContextData?>(exception);
        }
    }
}

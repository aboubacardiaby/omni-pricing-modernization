namespace Pricing.UnitTests.ExpirationDates;

using Pricing.Application.ExpirationDates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class ExpirationDateCollectorTests
{
    private static readonly DateOnly PricingDate = new(2028, 2, 29);

    [Fact]
    public void NullDatesProduceNoExpiration()
    {
        ExpirationDateCollectionResult result = Collect(Source("Open ended", null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.ExpirationDate);
        Assert.Empty(result.ValidSources);
        Assert.Empty(result.SelectedSources);
    }

    [Fact]
    public void DuplicateClosestDatesRetainEverySourceProvenance()
    {
        DateOnly closest = new(2028, 3, 1);

        ExpirationDateCollectionResult result = Collect(
            Source("Cost contract", closest, "CCG03"),
            Source("Freight", new DateOnly(2028, 12, 31), "VNG31"),
            Source("Sell arrangement", closest, "SAG10"));

        Assert.Equal(closest, result.ExpirationDate);
        Assert.Equal(3, result.ValidSources.Length);
        Assert.Collection(
            result.SelectedSources,
            source => Assert.Equal("CCG03", source.Provenance.Source),
            source => Assert.Equal("SAG10", source.Provenance.Source));
    }

    [Fact]
    public void ExpiredDatesAreExcluded()
    {
        ExpirationDateCollectionResult result = Collect(
            Source("Expired", PricingDate.AddDays(-1)),
            Source("Current", PricingDate.AddDays(1)));

        Assert.Equal(PricingDate.AddDays(1), result.ExpirationDate);
        Assert.Single(result.ValidSources);
        Assert.Equal("Current", result.SelectedSources.Single().Component);
    }

    [Fact]
    public void SameDayExpirationIsValidBecauseCobolBoundaryIsInclusive()
    {
        ExpirationDateCollectionResult result = Collect(Source("Same day", PricingDate));

        Assert.Equal(PricingDate, result.ExpirationDate);
        Assert.Single(result.SelectedSources);
    }

    [Fact]
    public void LeapDayParticipatesInChronologicalSelection()
    {
        ExpirationDateCollectionResult result = Collect(
            Source("Leap day", PricingDate),
            Source("March", new DateOnly(2028, 3, 1)));

        Assert.Equal(new DateOnly(2028, 2, 29), result.ExpirationDate);
    }

    [Fact]
    public void EarliestCompetingComponentExpirationWinsRegardlessOfInputOrder()
    {
        ExpirationDateCollectionResult result = Collect(
            Source("Surcharge", new DateOnly(2029, 1, 1)),
            Source("Rebate", new DateOnly(2028, 7, 25)),
            Source("Freight", new DateOnly(2028, 9, 1)));

        Assert.Equal(new DateOnly(2028, 7, 25), result.ExpirationDate);
        Assert.Equal("Rebate", result.SelectedSources.Single().Component);
        Assert.Equal("TEST-Rebate", result.SelectedSources.Single().Provenance.RuleName);
    }

    [Fact]
    public void FiftyCandidatesAreSupported()
    {
        ExpirationDateSource[] sources = Enumerable.Range(0, 50)
            .Select(index => Source($"Component {index}", PricingDate.AddDays(index)))
            .ToArray();

        ExpirationDateCollectionResult result = Collect(sources);

        Assert.True(result.IsSuccess);
        Assert.Equal(PricingDate, result.ExpirationDate);
        Assert.Equal(50, result.ValidSources.Length);
    }

    [Fact]
    public void FiftyFirstValidCandidateReturnsTypedLegacy145Error()
    {
        ExpirationDateSource[] sources = Enumerable.Range(0, 51)
            .Select(index => Source($"Component {index}", PricingDate.AddDays(index)))
            .ToArray();

        ExpirationDateCollectionResult result = Collect(sources);

        ValidationPricingError error = Assert.IsType<ValidationPricingError>(result.Error);
        Assert.Equal("EXPIRATION_DATE_LIMIT_EXCEEDED", error.Code);
        Assert.Equal("145", error.LegacyErrorCode);
        Assert.Null(result.ExpirationDate);
        Assert.Equal(50, result.ValidSources.Length);
        Assert.Empty(result.SelectedSources);
    }

    [Fact]
    public void NullAndExpiredSourcesDoNotConsumeCobolCandidateCapacity()
    {
        ExpirationDateSource[] valid = Enumerable.Range(0, 50)
            .Select(index => Source($"Valid {index}", PricingDate.AddDays(index)))
            .ToArray();

        ExpirationDateCollectionResult result = Collect(
            [Source("Null", null), Source("Expired", PricingDate.AddDays(-1)), .. valid]);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.ValidSources.Length);
    }

    [Fact]
    public void MissingComponentNameIsRejected()
    {
        RuleProvenance provenance = Provenance("Test", "SOURCE");

        Assert.Throws<ArgumentException>(() => new ExpirationDateSource(" ", PricingDate, provenance));
    }

    private static ExpirationDateCollectionResult Collect(params ExpirationDateSource[] sources) =>
        ExpirationDateCollector.Collect(PricingDate, sources);

    private static ExpirationDateSource Source(
        string component,
        DateOnly? expiration,
        string source = "TEST") =>
        new(component, expiration, Provenance($"TEST-{component}", source));

    private static RuleProvenance Provenance(string ruleName, string source) =>
        new(ruleName, source, "Component", new PricingDateRange(new DateOnly(2020, 1, 1), null));
}

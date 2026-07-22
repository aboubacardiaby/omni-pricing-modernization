namespace Pricing.ParityTests;

using System.Text.Json;
using ParityRunner;
using Xunit;

public sealed class PricingDifferenceClassifierTests
{
    [Fact]
    public void IdenticalSemanticPayloadsAreExactDespiteObjectPropertyOrder()
    {
        ClassifiedParityCase result = Classify(
            """{"cost":10,"sellPrice":12,"components":[]}""",
            """{"components":[],"sellPrice":12.0,"cost":10.00}""");

        Assert.True(result.IsExact);
        PricingDifference difference = Assert.Single(result.Differences);
        Assert.Equal(DifferenceCategory.Exact, difference.Category);
    }

    [Fact]
    public void ContractAndRuleMismatchesAreCriticalEvenWhenTotalsMatch()
    {
        ClassifiedParityCase result = Classify(
            Result(contract: "COBOL-CONT", rule: "Fixed", source: "COBOL-RULE"),
            Result(contract: "CSHARP-CONT", rule: "Fixed", source: "CSHARP-RULE"));

        Assert.Contains(result.Differences, difference =>
            difference.Category == DifferenceCategory.Contract
            && difference.Severity == DifferenceSeverity.Critical);
        Assert.Contains(result.Differences, difference =>
            difference.Category == DifferenceCategory.Rule
            && difference.Severity == DifferenceSeverity.Critical);
        Assert.DoesNotContain(result.Differences, difference => difference.Category == DifferenceCategory.Rounding);
    }

    [Fact]
    public void DistinguishesMissingAndAdditionalFeesFromCobolAuthority()
    {
        ClassifiedParityCase result = Classify(
            Result(components: """[{"name":"Freight","amount":1},{"name":"JIT service","amount":2}]"""),
            Result(components: """[{"name":"Freight","amount":1},{"name":"SurgiTrak","amount":3}]"""));

        Assert.Contains(result.Differences, difference =>
            difference.Category == DifferenceCategory.MissingFee
            && difference.CobolValue == "JIT service");
        Assert.Contains(result.Differences, difference =>
            difference.Category == DifferenceCategory.AdditionalFee
            && difference.CSharpValue == "SurgiTrak");
    }

    [Fact]
    public void SeparatesReviewableAndCriticalRoundingDifferences()
    {
        ClassifiedParityCase result = Classify(
            Result(cost: "10.000", sell: "12.00", components: """[{"name":"Freight","amount":1.00}]"""),
            Result(cost: "10.009", sell: "12.02", components: """[{"name":"Freight","amount":1.005}]"""));

        PricingDifference cost = Assert.Single(result.Differences, difference => difference.Path == "$.cost");
        Assert.Equal(DifferenceCategory.Rounding, cost.Category);
        Assert.Equal(DifferenceSeverity.Review, cost.Severity);
        Assert.True(cost.WithinRoundingTolerance);
        PricingDifference sell = Assert.Single(result.Differences, difference => difference.Path == "$.sellPrice");
        Assert.Equal(DifferenceSeverity.Critical, sell.Severity);
        Assert.False(sell.WithinRoundingTolerance);
    }

    [Fact]
    public void ClassifiesExpirationMismatchAsCriticalDateDifference()
    {
        ClassifiedParityCase result = Classify(
            Result(expiration: "2026-07-21"),
            Result(expiration: "2026-07-22"));

        PricingDifference difference = Assert.Single(result.Differences, item => item.Category == DifferenceCategory.Date);
        Assert.Equal(DifferenceSeverity.Critical, difference.Severity);
    }

    [Fact]
    public void ClassifiesStatusAndNotFoundMismatchAsErrorAndMissingData()
    {
        ClassifiedParityCase result = Classify(
            """{"errorCode":"PRODUCT_NOT_FOUND","legacyErrorCode":"61603"}""",
            Result(),
            cobolStatus: 404,
            csharpStatus: 200);

        Assert.Contains(result.Differences, difference => difference.Category == DifferenceCategory.Error);
        Assert.Contains(result.Differences, difference => difference.Category == DifferenceCategory.MissingData);
    }

    [Fact]
    public void MissingCriticalValueIsMissingDataRatherThanRounding()
    {
        ClassifiedParityCase result = Classify(Result(cost: "10"), Result(cost: "null"));

        PricingDifference difference = Assert.Single(result.Differences, item => item.Path == "$.cost");
        Assert.Equal(DifferenceCategory.MissingData, difference.Category);
        Assert.Equal(DifferenceSeverity.Critical, difference.Severity);
    }

    [Fact]
    public void ProblemErrorCodeMismatchIsCriticalError()
    {
        ClassifiedParityCase result = Classify(
            """{"errorCode":"PRODUCT_NOT_FOUND","legacyErrorCode":"61603"}""",
            """{"errorCode":"UOM_NOT_FOUND","legacyErrorCode":"61606"}""",
            404,
            404);

        Assert.All(result.Differences, difference => Assert.Equal(DifferenceSeverity.Critical, difference.Severity));
        Assert.Contains(result.Differences, difference =>
            difference.Category == DifferenceCategory.Error && difference.Path == "$.errorCode");
    }

    private static ClassifiedParityCase Classify(
        string cobol,
        string csharp,
        int cobolStatus = 200,
        int csharpStatus = 200)
    {
        var result = new ParityCaseResult(
            "CASE",
            Element("{}"),
            Observation(cobolStatus, cobol),
            Observation(csharpStatus, csharp));
        return new PricingDifferenceClassifier().Classify(result);
    }

    private static EngineObservation Observation(int status, string body) => new(
        status,
        Element(body),
        null,
        1,
        null);

    private static JsonElement Element(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string Result(
        string contract = "CONT-1",
        string rule = "Fixed",
        string source = "RULE-1",
        string cost = "10",
        string sell = "12",
        string expiration = "2026-07-21",
        string components = "[]") => $$"""
        {
          "productType":"Regular",
          "cost":{{cost}},
          "sellPrice":{{sell}},
          "expirationDate":"{{expiration}}",
          "contractIdentifier":"{{contract}}",
          "buyingGroupIdentifier":null,
          "ruleType":"{{rule}}",
          "components":{{components}},
          "provenance":[{"ruleName":"{{rule}}","source":"{{source}}"}],
          "warnings":[]
        }
        """;
}

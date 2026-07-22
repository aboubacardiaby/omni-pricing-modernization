namespace ParityRunner;

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

public enum DifferenceCategory
{
    Exact,
    Rounding,
    Rule,
    MissingFee,
    AdditionalFee,
    Date,
    Contract,
    Error,
    MissingData,
}

public enum DifferenceSeverity
{
    Informational,
    Review,
    Critical,
}

public sealed record PricingDifference(
    DifferenceCategory Category,
    DifferenceSeverity Severity,
    string Path,
    string? CobolValue,
    string? CSharpValue,
    decimal? NumericDelta = null,
    bool? WithinRoundingTolerance = null);

public sealed record ClassifiedParityCase(
    string Id,
    bool IsExact,
    ImmutableArray<PricingDifference> Differences);

public sealed record ClassifiedParityReport(
    int SchemaVersion,
    DateTimeOffset ClassifiedAtUtc,
    decimal RoundingTolerance,
    ImmutableArray<ClassifiedParityCase> Cases);

public sealed class PricingDifferenceClassifier(decimal roundingTolerance = 0.01m)
{
    public decimal RoundingTolerance { get; } = roundingTolerance >= 0m
        ? roundingTolerance
        : throw new ArgumentOutOfRangeException(nameof(roundingTolerance));

    public ClassifiedParityCase Classify(ParityCaseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (Equivalent(result.Cobol, result.CSharp))
        {
            return new(result.Id, true, [new(
                DifferenceCategory.Exact,
                DifferenceSeverity.Informational,
                "$",
                null,
                null)]);
        }

        var differences = ImmutableArray.CreateBuilder<PricingDifference>();
        CompareErrors(result.Cobol, result.CSharp, differences);
        if (result.Cobol.JsonBody is not { } cobol || result.CSharp.JsonBody is not { } csharp)
        {
            if (differences.Count == 0)
            {
                differences.Add(Critical(
                    DifferenceCategory.MissingData,
                    "$",
                    Body(result.Cobol),
                    Body(result.CSharp)));
            }

            return new(result.Id, false, differences.ToImmutable());
        }

        CompareScalar(cobol, csharp, "contractIdentifier", DifferenceCategory.Contract, differences);
        CompareScalar(cobol, csharp, "buyingGroupIdentifier", DifferenceCategory.Contract, differences);
        CompareScalar(cobol, csharp, "ruleType", DifferenceCategory.Rule, differences);
        CompareScalar(cobol, csharp, "productType", DifferenceCategory.Rule, differences);
        CompareScalar(cobol, csharp, "expirationDate", DifferenceCategory.Date, differences);
        CompareRulePath(cobol, csharp, differences);
        CompareMoney(cobol, csharp, "cost", differences);
        CompareMoney(cobol, csharp, "sellPrice", differences);
        CompareComponents(cobol, csharp, differences);
        CompareScalar(cobol, csharp, "errorCode", DifferenceCategory.Error, differences);
        CompareScalar(cobol, csharp, "legacyErrorCode", DifferenceCategory.Error, differences);
        CompareStructured(cobol, csharp, "warnings", DifferenceCategory.Error, differences);

        if (differences.Count == 0)
        {
            differences.Add(Critical(
                DifferenceCategory.MissingData,
                "$",
                cobol.GetRawText(),
                csharp.GetRawText()));
        }

        return new(result.Id, false, differences.ToImmutable());
    }

    private static void CompareErrors(
        EngineObservation cobol,
        EngineObservation csharp,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        if (cobol.StatusCode != csharp.StatusCode)
        {
            differences.Add(Critical(
                DifferenceCategory.Error,
                "$.statusCode",
                cobol.StatusCode.ToString(CultureInfo.InvariantCulture),
                csharp.StatusCode.ToString(CultureInfo.InvariantCulture)));
            if (cobol.StatusCode == 404 || csharp.StatusCode == 404)
            {
                differences.Add(Critical(
                    DifferenceCategory.MissingData,
                    "$",
                    cobol.StatusCode == 404 ? "not found" : "present",
                    csharp.StatusCode == 404 ? "not found" : "present"));
            }
        }
    }

    private static void CompareScalar(
        JsonElement cobol,
        JsonElement csharp,
        string property,
        DifferenceCategory category,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        bool hasCobol = cobol.TryGetProperty(property, out JsonElement cobolValue)
            && cobolValue.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined;
        bool hasCSharp = csharp.TryGetProperty(property, out JsonElement csharpValue)
            && csharpValue.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined;
        if (!hasCobol && !hasCSharp)
        {
            return;
        }

        string? left = hasCobol ? Display(cobolValue) : null;
        string? right = hasCSharp ? Display(csharpValue) : null;
        if (!string.Equals(left, right, StringComparison.Ordinal))
        {
            differences.Add(Critical(
                !hasCobol || !hasCSharp ? DifferenceCategory.MissingData : category,
                "$." + property,
                left,
                right));
        }
    }

    private void CompareMoney(
        JsonElement cobol,
        JsonElement csharp,
        string property,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        bool hasCobol = TryDecimal(cobol, property, out decimal left);
        bool hasCSharp = TryDecimal(csharp, property, out decimal right);
        if (!hasCobol && !hasCSharp)
        {
            return;
        }

        if (!hasCobol || !hasCSharp)
        {
            differences.Add(Critical(
                DifferenceCategory.MissingData,
                "$." + property,
                hasCobol ? Format(left) : null,
                hasCSharp ? Format(right) : null));
            return;
        }

        AddRoundingDifference("$." + property, left, right, differences);
    }

    private void CompareComponents(
        JsonElement cobol,
        JsonElement csharp,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        Dictionary<string, Queue<JsonElement>> left = Components(cobol);
        Dictionary<string, Queue<JsonElement>> right = Components(csharp);
        foreach ((string name, Queue<JsonElement> values) in left)
        {
            while (values.Count > 0)
            {
                if (!right.TryGetValue(name, out Queue<JsonElement>? candidates) || candidates.Count == 0)
                {
                    differences.Add(Critical(DifferenceCategory.MissingFee, "$.components", name, null));
                    values.Dequeue();
                    continue;
                }

                JsonElement leftComponent = values.Dequeue();
                JsonElement rightComponent = candidates.Dequeue();
                if (TryDecimal(leftComponent, "amount", out decimal leftAmount)
                    && TryDecimal(rightComponent, "amount", out decimal rightAmount))
                {
                    AddRoundingDifference(
                        $"$.components[{JsonSerializer.Serialize(name)}].amount",
                        leftAmount,
                        rightAmount,
                        differences);
                }

                CompareStructured(
                    leftComponent,
                    rightComponent,
                    "provenance",
                    DifferenceCategory.Rule,
                    differences,
                    $"$.components[{JsonSerializer.Serialize(name)}].provenance");
            }
        }

        foreach ((string name, Queue<JsonElement> values) in right)
        {
            while (values.Count > 0)
            {
                differences.Add(Critical(DifferenceCategory.AdditionalFee, "$.components", null, name));
                values.Dequeue();
            }
        }
    }

    private static void CompareRulePath(
        JsonElement cobol,
        JsonElement csharp,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        CompareStructured(cobol, csharp, "provenance", DifferenceCategory.Rule, differences);
    }

    private static void CompareStructured(
        JsonElement cobol,
        JsonElement csharp,
        string property,
        DifferenceCategory category,
        ImmutableArray<PricingDifference>.Builder differences,
        string? path = null)
    {
        bool hasCobol = cobol.TryGetProperty(property, out JsonElement left);
        bool hasCSharp = csharp.TryGetProperty(property, out JsonElement right);
        if (!hasCobol && !hasCSharp)
        {
            return;
        }

        if (!hasCobol || !hasCSharp || !JsonEquivalent(left, right))
        {
            differences.Add(Critical(
                !hasCobol || !hasCSharp ? DifferenceCategory.MissingData : category,
                path ?? "$." + property,
                hasCobol ? left.GetRawText() : null,
                hasCSharp ? right.GetRawText() : null));
        }
    }

    private void AddRoundingDifference(
        string path,
        decimal left,
        decimal right,
        ImmutableArray<PricingDifference>.Builder differences)
    {
        decimal delta = right - left;
        if (delta == 0m)
        {
            return;
        }

        bool withinTolerance = decimal.Abs(delta) <= RoundingTolerance;
        differences.Add(new PricingDifference(
            DifferenceCategory.Rounding,
            withinTolerance ? DifferenceSeverity.Review : DifferenceSeverity.Critical,
            path,
            Format(left),
            Format(right),
            delta,
            withinTolerance));
    }

    private static Dictionary<string, Queue<JsonElement>> Components(JsonElement root)
    {
        var result = new Dictionary<string, Queue<JsonElement>>(StringComparer.Ordinal);
        if (!root.TryGetProperty("components", out JsonElement components)
            || components.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (JsonElement component in components.EnumerateArray())
        {
            if (!component.TryGetProperty("name", out JsonElement nameElement)
                || nameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string name = nameElement.GetString()!;
            if (!result.TryGetValue(name, out Queue<JsonElement>? values))
            {
                values = new Queue<JsonElement>();
                result.Add(name, values);
            }

            values.Enqueue(component);
        }

        return result;
    }

    private static bool Equivalent(EngineObservation left, EngineObservation right) =>
        left.StatusCode == right.StatusCode
        && string.Equals(left.NonJsonBody, right.NonJsonBody, StringComparison.Ordinal)
        && ((left.JsonBody is null && right.JsonBody is null)
            || (left.JsonBody is { } leftJson && right.JsonBody is { } rightJson
                && JsonEquivalent(leftJson, rightJson)));

    private static bool JsonEquivalent(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.Object => left.EnumerateObject().All(property =>
                right.TryGetProperty(property.Name, out JsonElement value)
                && JsonEquivalent(property.Value, value))
                && left.EnumerateObject().Count() == right.EnumerateObject().Count(),
            JsonValueKind.Array => left.GetArrayLength() == right.GetArrayLength()
                && left.EnumerateArray().Zip(right.EnumerateArray()).All(pair => JsonEquivalent(pair.First, pair.Second)),
            JsonValueKind.Number => left.TryGetDecimal(out decimal leftNumber)
                && right.TryGetDecimal(out decimal rightNumber)
                && leftNumber == rightNumber,
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            _ => left.GetRawText() == right.GetRawText(),
        };
    }

    private static bool TryDecimal(JsonElement root, string property, out decimal value)
    {
        value = default;
        return root.TryGetProperty(property, out JsonElement element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetDecimal(out value);
    }

    private static string? Body(EngineObservation observation) =>
        observation.JsonBody?.GetRawText() ?? observation.NonJsonBody;

    private static string Display(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString()!
        : value.GetRawText();

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static PricingDifference Critical(
        DifferenceCategory category,
        string path,
        string? cobolValue,
        string? csharpValue) => new(
            category,
            DifferenceSeverity.Critical,
            path,
            cobolValue,
            csharpValue);
}

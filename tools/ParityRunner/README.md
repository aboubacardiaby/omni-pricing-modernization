# ParityRunner

## In-process T056 runner

`InProcessParityRunner` invokes an `ICobolPricingAdapter` and the real C# `IPricingOrchestrator`
with the same immutable `PricingOperation`. Its schema-versioned report retains both complete
`PricingResult` values plus leaf-level field pairs, including selections, hierarchy, components,
expiration, errors, and provenance, so later difference classification does not have to reconstruct
either decision path.

`FixtureBackedCobolPricingAdapter` is the development/test adapter for T055. It accepts only fixture
IDs found in `docs/characterization/pricing-scenario-catalog.md` and labels every observation
`FixtureBackedDocumentedExpectation`. Those values are hand-derived documented expectations, not
live or captured COBOL results and therefore not independent parity evidence. A future live adapter
implements the same interface without changing the runner. T055 provides prose conditions rather
than executable `PricingRequest` and repository datasets, so fixtures must be registered explicitly;
the runner does not invent the missing inputs.

Reports are written atomically through `InProcessParityReportFile.SaveAsync`.

## HTTP capture runner

`ParityRunner` submits each JSON pricing input unchanged to a configured COBOL adapter and the
C# pricing API, then persists both observations. It captures evidence only; difference
classification belongs to T057.

Input files use this shape:

```json
[
  {
    "id": "SCN-COST-001",
    "input": {
      "division": "01",
      "account": "123456",
      "vendor": "1234",
      "product": "ABC123",
      "quantity": 1,
      "unitOfMeasure": "EA",
      "pricingDate": "2026-07-21",
      "requestType": "Full"
    }
  }
]
```

Run it with explicit endpoints and an output path:

```powershell
dotnet run --project tools/ParityRunner/ParityRunner.csproj -- `
  --cases path/to/cases.json `
  --cobol-url https://cobol-adapter.example/prices `
  --csharp-url https://csharp-pricing.example/api/v1/prices/calculate `
  --output artifacts/parity/run.json
```

The COBOL adapter is expected to accept the same API request JSON as the C# endpoint. Endpoint
authentication is an environment/deployment concern; credentials must not be placed in case or
report files. T055's catalog at Claude commit `24b098c` contains hand-derived scenario
definitions, not live COBOL captures or machine-readable requests. Those definitions must be
materialized as approved case JSON, and a real adapter or COMMAREA capture must be available,
before a run can constitute independent COBOL/C# parity evidence.

Classify an existing capture report separately:

```powershell
dotnet run --project tools/ParityRunner/ParityRunner.csproj -- classify `
  --report artifacts/parity/run.json `
  --output artifacts/parity/classified.json `
  --rounding-tolerance 0.01
```

Classification is COBOL-authoritative. Contract or decision-path differences are always
critical even when cost and sell totals match. Monetary differences are categorized as
rounding differences and retain their exact delta; differences outside the configured tolerance
are critical. The classifier exits `3` when any critical difference is present.

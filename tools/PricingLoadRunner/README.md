# PricingLoadRunner

This tool executes the T058 workload matrix against the C# pricing endpoint and writes a
schema-versioned JSON report containing per-workload DB calls, p50/p95/p99 latency, throughput,
server working-set observations, error rate, and kit-size scaling.

The load-plan JSON contains an endpoint, warmup/count/concurrency controls, and explicitly named
dimension levels. Each workload request uses the T053 API contract. Kit workloads identify their
component count and depth so the report can calculate p95 scaling relative to the smallest kit.

```json
{
  "schemaVersion": 1,
  "endpoint": "https://pricing.example/api/v1/prices/calculate",
  "warmupRequests": 5,
  "requestsPerWorkload": 100,
  "concurrency": 8,
  "workloads": [
    {
      "id": "cost-individual-fast-path",
      "dimension": "cost-cascade-depth",
      "level": "individual-single-row",
      "kitComponentCount": null,
      "kitDepth": null,
      "orderLineCount": 1,
      "request": {
        "division": "01",
        "account": "123456",
        "vendor": "1234",
        "product": "ABC123",
        "quantity": 1,
        "unitOfMeasure": "EA",
        "pricingDate": "2026-07-22",
        "requestType": "Full"
      }
    }
  ]
}
```

Run:

```powershell
dotnet run --project tools/PricingLoadRunner/PricingLoadRunner.csproj -- `
  --plan path/to/load-plan.json `
  --output artifacts/performance/csharp-load-report.json
```

For a controlled performance environment, set `Performance:ExposeMetricsHeaders=true` on
`Pricing.Api`. This enables `X-DB-Call-Count` and `X-Process-Working-Set-Bytes`. The headers are
disabled by default and should not be enabled on an internet-facing deployment. If unavailable,
the report retains null measurements and lists them under `missingMeasurements`; it never
fabricates zero DB calls or memory usage.

Actual thresholds and comparison with COBOL remain blocked until T058's legacy COBOL baseline is
captured. A run against mocks or a developer machine validates the harness, not production
capacity.

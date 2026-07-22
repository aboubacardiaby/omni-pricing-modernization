# C# Pricing Load-Test Report

**Task:** T059  
**Measurement date:** 2026-07-22  
**Status:** Harness verified; production-like measurements not yet captured

## Result

`PricingLoadRunner` is a repeatable HTTP load harness for the T053 pricing endpoint. It writes a
schema-versioned JSON report for every workload level defined by the T058 method. The report
contains:

- request count, error count, and error rate;
- p50, p95, and p99 response latency;
- elapsed-run throughput in requests per second;
- total and average DB calls per request;
- minimum, peak, and growth of server process working set; and
- kit p95 and throughput scaling, including the p95 ratio to the smallest kit.

The performance headers are disabled by default. Setting
`Performance:ExposeMetricsHeaders=true` enables request-scoped `X-DB-Call-Count` and
`X-Process-Working-Set-Bytes` headers. Every call through `DapperDb2QueryExecutor` increments the
request counter. If either header is absent or incomplete, the report emits `null` and records the
metric under `missingMeasurements`; it does not report an invented zero.

## Verification Run

The automated harness tests use deterministic observations and are verification of metric
calculation and concurrency control, not a pricing capacity baseline.

| Check | Result |
|---|---:|
| Performance test cases | 4 passed, 0 failed |
| Percentile aggregation | Passed |
| Throughput/error aggregation | Passed |
| DB-call and working-set aggregation | Passed |
| Missing-telemetry handling | Passed |
| Kit-size relative effect | Passed |
| Configured concurrency ceiling | Passed |
| Solution build | 0 warnings, 0 errors |

## Measurement Status

| Metric | Production-like C# result | Reason |
|---|---:|---|
| DB calls/request | Not measured | No configured DB2 test environment or representative data supplied |
| p50/p95/p99 latency | Not measured | No production-like pricing endpoint supplied |
| Throughput | Not measured | No production-like pricing endpoint supplied |
| Working-set memory | Not measured | No production-like pricing process supplied |
| Error rate | Not measured | No production-like workload execution performed |
| Kit-size/depth effects | Not measured | Representative kit data and endpoint are not available |
| C# versus COBOL comparison | Not measured | T058 records a method but no captured COBOL numeric baseline |

These omissions are explicit deployment evidence gaps, not zero-valued results. Run the tool in a
controlled environment using the T058 workload dimensions: cost-cascade depth, fee breadth, kit
component count/depth, rebate/vendor adjustment, special versus normal pricing, and order-line
count. Preserve the generated JSON artifact with the environment and commit identifiers before
using its numbers for capacity or cutover decisions.

## Command

```powershell
dotnet run --project tools/PricingLoadRunner/PricingLoadRunner.csproj -- `
  --plan path/to/load-plan.json `
  --output artifacts/performance/csharp-load-report.json
```

The load-plan format and a representative workload example are documented in
`tools/PricingLoadRunner/README.md`.

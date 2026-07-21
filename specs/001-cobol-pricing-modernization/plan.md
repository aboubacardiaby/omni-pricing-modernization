# Implementation Plan: COBOL Pricing Modernization

## Summary

Build an incremental .NET pricing platform beside the COBOL subsystem. First document and characterize legacy behavior; then implement compatibility and domain services; then migrate cost, sell, fee, expiration, and kit rules; finally run shadow comparison and controlled cutover.

## Technical Context

- **Language/runtime**: C# / .NET 8 LTS
- **API**: ASP.NET Core
- **Database**: existing DB2 initially; provider confirmed during discovery
- **Data access**: Dapper for parity-sensitive SQL; EF Core only with an ADR
- **Testing**: xUnit, integration fixtures, snapshot/test-vector data, parity runner
- **Observability**: OpenTelemetry, structured logging, health checks
- **Deployment**: containerized service; target platform to be decided
- **Legacy interface**: CICS programs and 1,789-byte OMGPR communication structure

## Constitution Check

| Principle | Plan compliance |
|---|---|
| COBOL source of truth | Discovery artifacts and rule traceability precede implementation |
| Parity before replacement | Characterization, parity runner, shadow execution, gated cutover |
| Decimal/date/layout fidelity | Compatibility project, value objects, explicit rounding/date services |
| Modular domain | Separate application/domain/infrastructure/compatibility projects |
| Explainability | Itemized price components and rule provenance |
| Test-first migration | Rule-specific cases and parity fixtures required per task |
| Reversible delivery | COBOL authoritative through shadow; scoped feature flags and rollback |
| Agent isolation | Claude analysis, Codex implementation, file-scoped task ownership |

## Project Structure

```text
src/
├── Pricing.Api/
├── Pricing.Application/
├── Pricing.Domain/
├── Pricing.Infrastructure/
└── Pricing.Compatibility/
tests/
├── Pricing.UnitTests/
├── Pricing.IntegrationTests/
├── Pricing.CharacterizationTests/
└── Pricing.ParityTests/
tools/
├── OmgprDecoder/
└── ParityRunner/
docs/
├── cobol-analysis/
├── mappings/
├── rules/
└── decisions/
```

## Phase Strategy

### Phase A — Discovery

Claude produces program, field, SQL, error, rule, date, and rounding inventories. Codex reviews implementability and identifies unresolved technical dependencies.

### Phase B — Foundation and compatibility

Codex creates the .NET solution, domain types, API foundation, and OMGPR codec. Claude supplies data dictionaries and test vectors, then reviews fidelity.

### Phase C — Context and regular-item pricing

Implement product and customer context followed by ordered cost, rebate, sell, adjustment, fee, expiration, and finalization pipelines.

### Phase D — Kit pricing

Wrap or reimplement explosion, reuse regular pricing for each component, prevent recursion, roll up components, convert UOM, and choose earliest expiration.

### Phase E — Parity and cutover

Run both engines, classify differences, establish performance baselines, shadow production-like traffic, then release via scoped flags with rollback.

## Architecture Decisions

1. Rules are ordered strategies, not a single conditional service.
2. PricingRequest, PricingContext, and PricingResult replace the mixed OMGPR domain representation.
3. Legacy binary concerns remain inside Pricing.Compatibility.
4. Each repository exposes business queries rather than raw table CRUD.
5. A price includes provenance and components, not only totals.
6. Missing legacy behavior remains an explicit blocker.

## Parallel Work Boundaries

| Workstream | Claude writes | Codex writes |
|---|---|---|
| Discovery | `docs/cobol-analysis/**`, `docs/mappings/**`, `docs/rules/**` | Reviews and ADR comments only |
| Foundation | Review notes | `src/**`, solution/build files |
| Test design | Scenario/test-vector documents | Executable tests and fixtures |
| Rule migration | Rule decision tables | Domain rules and repositories |
| Parity | Difference interpretation | Runner, comparators, reports |

Agents MUST NOT edit the other workstream's files unless a task explicitly transfers ownership.

## Risk Controls

- Use rule provenance to detect accidental priority changes.
- Compare decision path as well as final price.
- Keep rounding and expiration as first-class services.
- Gate inferred behavior behind unresolved questions.
- Use production-like, sanitized characterization data.
- Avoid caching until correctness is established; later keys must include all pricing determinants.


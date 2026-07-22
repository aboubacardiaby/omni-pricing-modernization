# Codex Work Session History

**Project:** OMNI COBOL Pricing Modernization  
**Workspace:** `C:\Users\Lenovo\Desktop\owensminor\omni-codex`  
**Recorded:** 2026-07-22  
**Scope:** Consolidated history of the visible user/Codex/Claude handoffs in this conversation.

> This is a reconstructed work log, not a byte-for-byte chat export. The assistant does not have
> a transcript-export API, and earlier conversation turns may have been compacted. Repository task
> records, review documents, commits, and test output remain the authoritative implementation
> evidence.

## Operating instructions

The user required Codex to read `AGENTS.md`, the constitution, specification, implementation plan,
task list, and relevant contracts/COBOL sources before working. The standing constraints included:

- COBOL behavior remains authoritative.
- Do not invent missing behavior or implement rules labeled inferred without approval.
- Claim only tasks with complete dependencies and record ownership in `tasks.md`.
- Do not create a monolithic `A6U01.cs` or modify COBOL attachments.
- Preserve decimal, date, binary-layout, hierarchy, priority, error, and rounding fidelity.
- Keep Domain independent of API and Infrastructure.
- Run focused and solution-level verification and report commands/results.

## Foundation and compatibility sequence

1. The user initially requested T011 only: create the .NET 8 solution and the API, Application,
   Domain, Infrastructure, Compatibility, UnitTests, IntegrationTests, CharacterizationTests, and
   ParityTests projects with central package management, nullable reference types, analyzers,
   references, and baseline tests.
2. The user reported that `specs/001-cobol-pricing-modernization/contracts/pricing-api.yaml` had
   never been created and requested its creation.
3. The user then requested T012, T013, T014, and T015 sequentially.
4. Claude discovery locations were supplied for T003 artifacts, including:
   `docs/cobol-analysis/characterization-scenarios/contract-cost-method.md` and the separate
   `omni-claude/docs/mappings` worktree.
5. The user asked Codex to verify implementation against Claude's requirements.

## Gate G1 and discovery synchronization

1. The user requested G1 and then a second G1 review.
2. Claude reported discovery deliverables on `agent/claude-discovery`, initially citing commit
   `f52b64d`, and asked Codex to reassess T007-T010 against current documents.
3. A later handoff clarified that expected commits/refs had not actually appeared locally and that
   process completion did not mean push success.
4. The user subsequently stated Phase 1 discovery T001-T010 was complete and G1 re-review remained.

## Context, compatibility, and Gate G2

1. The user requested T017, T018, and T019, including repeated T018/T019 execution after Claude
   updates.
2. G2 was requested. Claude's first review recorded commit `472e5c1` and returned NOT APPROVED.
   Findings included:
   - `OMGPR-Q-ERROR-CODE` and `OMGPR-Q-ERROR-NBR` were conflated.
   - The OMGPR vector file was not loaded by tests.
   - Contract number, unit cost, and pricing percentage were not wired.
   - Three-byte COMP decoding was unsupported.
   - Encoding-profile and COMP-3 validation design notes remained.
3. Claude re-reviewed at `f71c7e0`, independently ran 84/84 tests, and confirmed the original six
   findings fixed. One new issue remained: the error path wrote the error number but not the
   independent severity code.
4. Claude approved G2 at `f845649` after verifying independent `LegacySeverityCode`, paired writes,
   range validation, missing-severity rejection, and 85/85 passing tests. Mainframe encoding and
   byte order remained deployment assumptions pending a live COMMAREA capture.

## Regular pricing implementation

The user requested T020 through T031 sequentially, followed by G3.

G3 was pushed as `08f5a0d` and was NOT APPROVED. Both reviewers concluded that T055 fixtures and the
T056 parity runner did not exist, so no genuine COBOL/C# comparison had run. Claude merged Codex's
local T011-T031 evidence into the branch task record while preserving the fuller discovery/G1/G2
history. T055 was identified as the concrete prerequisite for a real G3 run.

## Sell, fees, dates, and finalization

The user requested T032 through T047 sequentially. This covered customer hierarchy and sell rules,
sell-price methods, price locks, adjustments, freight, JIT, low-UOM/break-bulk, PANDAC/SurgiTrak,
fees, surcharge/markup composition, expiration, rounding, and final orchestration. The detailed
COBOL references, rule behavior, and cumulative test evidence are retained under each task in
`specs/001-cobol-pricing-modernization/tasks.md`.

## Kit pricing and Gate G4

1. The user requested T048, T049, T050, T051, and T052, with several repeated T048/T049 requests.
2. G4 was requested and ultimately recorded as NOT APPROVED in
   `docs/reviews/g4-kit-parity-review.md`.
3. Recorded blockers included no authoritative sanitized COBOL kit fixtures, no executable kit
   comparison, no kit parity runner, and unresolved pack-level sell, component-quantity staging,
   and component-expiration differences.

## API, parity operations, and fixtures

1. The user requested T053 and T054.
2. T055 and T056 were requested multiple times. The user later clarified Claude had already
   completed T055.
3. T056 created an in-process parity boundary and reports that retain observation provenance.
   Fixture-backed documented expectations are explicitly not represented as live COBOL evidence.
4. T057 implemented difference classification for exact, rounding, rule, fee, date, contract,
   error, and missing-data differences.
5. Claude later reported these changes pushed as commit `353b051` and independently verified:
   - T056 fixture validation and `CobolObservationSource` tagging.
   - T057 comparison implementation.
   - 460/460 solution tests.

## T059 performance/load harness

The user requested T059. Codex:

- Verified T058 at Claude commit `df411db`.
- Added `PricingLoadRunner`, a repeatable HTTP load harness.
- Added request-scoped DB-call counting through `DapperDb2QueryExecutor`.
- Added opt-in `X-DB-Call-Count` and `X-Process-Working-Set-Bytes` response headers.
- Reported request/error counts, error rate, throughput, p50/p95/p99 latency, DB calls, working-set
  measurements, and kit-size scaling.
- Ensured missing instrumentation remains `null` and is listed under `missingMeasurements` rather
  than represented as zero.
- Added `Pricing.PerformanceTests` with tests for aggregation, missing telemetry, kit scaling, and
  concurrency bounds.
- Added `docs/performance/csharp-load-test-report.md`.

Verification commands included:

```powershell
$env:DOTNET_ROLL_FORWARD='Major'
dotnet restore Pricing.sln --nologo
dotnet build Pricing.sln --no-restore --nologo
dotnet test Pricing.sln --no-build --no-restore --nologo
git diff --check
```

Reported result: build succeeded with zero warnings/errors and 464 tests passed in that local state
(410 unit, 33 integration, 1 characterization, 16 parity, and 4 performance). Claude's merged-state
independent verification later reported 460/460 solution tests plus 4/4 performance tests. The
difference reflects solution/test-project counting at the respective repository states, not a
reported test failure.

No production-like headline performance values were claimed. The report explicitly records DB
calls, latency, throughput, memory, error rate, kit effects, and COBOL comparison as not measured
because no configured DB2 environment, representative data, production-like endpoint, or numeric
COBOL baseline was available.

## T060 optimization decision

The user requested T060. Codex inspected the performance evidence and implementation paths and
declined to introduce speculative optimization:

- T059 contained no production-like measurements or verified C# hotspot.
- T058 defined a measurement method but contained no numeric COBOL baseline.
- `Pricing.Infrastructure` had no concrete pricing DB2 adapters for the cost, sell, context, and fee
  repository interfaces; it contained generic Dapper plumbing and the legacy kit transport.
- There was therefore no concrete pricing SQL, index, or batching path to optimize.
- Adding request caching without measured benefit and live parity evidence would violate the plan's
  instruction to avoid caching until correctness is established.

T060 was marked `[!]` with unblock criteria. The parity project was run unchanged: 16/16 tests
passed. Claude independently verified and endorsed the blocked decision in the `353b051` handoff.

### T060 unblock requirements

1. Provide a runnable, DB2-backed pricing API environment and required drivers/configuration.
2. Implement or supply concrete DB2 pricing repository adapters.
3. Supply sanitized representative requests for cost cascade, fee breadth, kit size/depth,
   rebate/vendor adjustment, special-contract, and order-line-count workloads.
4. Run `PricingLoadRunner` and capture real DB-call, latency, throughput, memory, error-rate, and
   kit-scaling results.
5. Preferably capture matching COBOL/CICS/DB2 measurements for the same workloads.
6. Identify a specific hotspot, record the before measurement, optimize only that path, record the
   after measurement, and rerun the complete parity suite.
7. If caching is selected, its key must include every pricing determinant and pricing date.

## T061 shadow execution decision

The user requested T061. Codex verified that T061 explicitly depends on T060 and that T060 remained
`[!]`. Per `AGENTS.md`, an agent must not start a task whose dependencies are incomplete. T061 was
therefore marked `[!]` dependency-blocked without runtime changes.

T061 can begin after T060 has a measured, evidence-backed optimization and its unchanged-parity
acceptance criterion passes. T062 and T063 remain transitively dependent on that sequence.

## Current recorded state

- T056: complete and independently reviewed.
- T057: complete and independently reviewed.
- T059: complete and independently reviewed.
- T060: blocked pending measurements and concrete repository/query paths.
- T061: blocked because T060 is incomplete.
- T062: not started; depends on T061.
- T063: not started; depends on T061 and T062.
- COBOL remains authoritative.
- No speculative cache, query optimization, shadow authority, or cutover behavior has been added.

## Authoritative references

- `specs/001-cobol-pricing-modernization/tasks.md`
- `docs/performance/cobol-baseline-method.md` on Claude commit `df411db`
- `docs/performance/csharp-load-test-report.md`
- `docs/reviews/g4-kit-parity-review.md`
- Source and tests under `src/`, `tests/`, and `tools/`

# COBOL Performance Baseline — Method and Representative Workload Dimensions (T058)

**Scope:** per `NFR-005` ("Performance targets are established from a measured COBOL baseline
before cutover") and `spec.md`'s open question #4 ("What production performance and parity
thresholds are required for cutover?"), this document defines **how** a COBOL performance baseline
should be measured and **what workload dimensions** are representative enough to make that baseline
meaningful — grounded directly in evidence already established by T001/T002/T005/T007/T008/T009 and
`kit-processing.md`, not generic load-testing boilerplate. It feeds T059 (Codex's C#-side
performance/load tests), which must measure the same dimensions for the comparison to mean anything.

**What this document does NOT provide:** actual captured baseline numbers. No DB2/CICS/COBOL
execution environment has been available to this project at any point — the same standing blocker
recorded in `program-inventory.md`, `db2-table-inventory.md`, every G1–G4 review, and every
ASSUMPTION/BLOCKED item across this document set. This is the method and dimension matrix for
whoever *does* have access to the live or QA COBOL/CICS/DB2 system to run against; it should not be
mistaken for a measured baseline itself, exactly the same distinction `pricing-scenario-catalog.md`
(T055) draws for its own expected-output values.

---

## 1. Baseline measurement method

### 1.1 What to instrument on the COBOL side

| Layer | What to capture | Confirmed source of the need |
|---|---|---|
| CICS transaction | Response-time percentiles (p50/p95/p99), CPU time, per transaction ID — `A6X01` (the online proxy/router, `program-inventory.md` §1.1), `A6O011U` (kit orchestrator), `CUP100` (customer-context gatherer) — via CICS Performance Class records (SMF 110) or the site's existing APM/monitor tooling, whichever is already collected in production. | `program-inventory.md` §1.1–§1.9 establishes these as the actual entry points a request passes through; percentiles per transaction ID let a later comparison isolate "context gathering" time from "core pricing" time from "kit orchestration" time rather than one opaque end-to-end number. |
| DB2 | SQL elapsed time and call counts, ideally per program/paragraph, not just aggregate — via DB2 Accounting trace (SMF 101) or `EXPLAIN`-derived cost estimates if live tracing isn't feasible in the target environment. | `db2-table-inventory.md` mechanically inventoried 417 `EXEC SQL` blocks across the 9 supplied programs (180 `INCLUDE`, 187 `SELECT`, 13 `DECLARE CURSOR`, 12 each `OPEN`/`FETCH`/`CLOSE`) — this is a read-heavy, DB2-round-trip-dominated workload by construction (confirmed: no `UPDATE`/`INSERT`/`DELETE` anywhere in this program set), so DB2 call count and elapsed time, not CPU-bound compute, is very likely the dominant cost driver and the metric most worth getting right. |
| Correlation | A way to tie **one** end-to-end pricing request (one `A6X01` invocation) to its **full** DB2 call fan-out for that request specifically, not aggregate volume across all concurrent traffic. | Needed so "DB calls per request" (T059's own required metric) can be computed on the COBOL side at all — without per-request correlation, only an aggregate rate is available, which cannot be compared apples-to-apples against the C# side's per-request instrumentation. |

### 1.2 Correlating COBOL-side and C#-side measurement

For T059's eventual comparison to be meaningful, both sides must measure the **same** workload
dimension under the **same** definition of "one request." The C# side already has the plumbing for
this — `Pricing.Api`'s `CorrelationIdMiddleware` (T012) and the `OpenTelemetry`
instrumentation configured alongside it. The COBOL side has no equivalent already confirmed from
the supplied source; a real baseline run would need an analogous correlation mechanism on that
side — CICS's own `EIBTRNID`/`EIBTASKN` fields plus a DB2 accounting correlation token are the
standard mechanism for this, but their actual availability/configuration in the target CICS region
is unconfirmed and **BLOCKED** the same way every other "what does the live environment actually do"
question in this project is blocked.

### 1.3 Fair-comparison caveat: COBOL's own no-cross-request-caching baseline

`A6U01` and its callers were not observed anywhere in this project's evidence to cache pricing data
*across* separate CICS transactions (each pseudo-conversational task re-does its own DB2 lookups
from scratch) — nothing in `program-inventory.md`/`db2-table-inventory.md` documents a
cross-request cache layer. **This matters directly for T060's later acceptance criterion** ("cache
keys include every pricing determinant and pricing date") — the COBOL baseline this document
describes should be captured under COBOL's own real behavior (no cross-request cache), so that any
C#-side caching introduced later by T060 is measured as a genuine *optimization* against a
cache-free baseline, not silently baked into the baseline itself and hidden from the "results
unchanged" comparison. This is INFERRED from absence (no cache mechanism was found in the read
programs), not confirmed by a comment stating "no caching occurs" — worth a targeted recheck
against the live environment before being relied on for a strict apples-to-apples claim.

---

## 2. Representative workload dimensions

Each dimension below is grounded in a specific piece of already-CONFIRMED evidence from this
project's own rule-extraction work, not a generic assumption about what "representative" load
testing should cover.

| # | Dimension | Representative levels | Why this dimension matters (evidence) |
|---|---|---|---|
| 1 | **Cost-cascade depth** | (a) individual contract, single-row match; (b) individual contract, duplicate-resolution cursor loop; (c) buy-group priority walk, child-level match only; (d) buy-group priority walk with full ancestor climb (multiple parent levels); (e) healthcare override (account-then-CID, 4 date windows checked); (f) acquisition/dealer-cost terminal fallback. | These are architecturally different SQL shapes with very different DB2 round-trip counts (`cost-selection-rules.md` R-COST-001 through R-ACQ-002) — (a) is a single indexed lookup, (d) is an unbounded-depth cursor-driven ancestor walk. **The original COBOL authors already flagged this class of cursor as performance-sensitive**: `A6Z01_C_CUG07`/`A6Z01_C_CUG11`/`A6Z01_S_CUG07`/`A6Z01_S_CUG11` (buy-group tier/priority resolution), `ACT_PRD_CAT_CSR`/`ACT_VEN_CSR`/`CUS_PRD_CAT_CSR`/`CUS_VEN_CSR` (buy-group override chase), and `MIN_GRP`/`MIN_GRP_CONT`/`MIN_INDV` (group/individual contract-line resolution) all carry an explicit `OPTIMIZE FOR 1 ROW` DB2 optimizer hint (`program-inventory.md` §4.3, CONFIRMED, `A6U01` lines 2104–2397) — a hint that only makes sense on a cursor the authors expected to be opened frequently and wanted the optimizer to favor fast-first-row access over full-result-set throughput. This is direct historical evidence that this exact dimension was already a known cost center. |
| 2 | **Fee-stacking breadth** | (a) a bare line with no applicable fees; (b) freight only (up to 5-level source cascade, R-FREIGHT-001..005); (c) JIT + freight; (d) JIT + freight + LUOM/break-bulk (up to 5-rule chain, R-LUOM-001..005) + PANDAC + surcharge, all simultaneously applicable. | `fees-and-adjustments.md` documents 37 distinct rules across 10 fee areas; a line that triggers several of them stacks multiple independent lookup cascades on top of the base cost/sell computation, each with its own SQL. Level (d) is the realistic worst case for a single order line, not a contrived one — every one of those areas is independently triggerable by ordinary data (JIT-eligible ship-to, a freight-priced vendor, a LUOM-flagged product, a surcharge-matched category), so they can co-occur on a real line. |
| 3 | **Kit size and depth** | (a) small kit, 1 level, <10 components; (b) medium kit, 1-2 levels, ~50 components; (c) large kit, 2 levels, approaching the 300-sub-pack boundary; (d) extreme kit, approaching the 699-item effective boundary (`kit-processing.md` R-KIT-004, corrected during the G4 review — the true effective limit, not the naively-read `OCCURS 700`). | These boundaries are not arbitrary round numbers chosen for this document — they are the actual, CONFIRMED capacity limits `A6O012U` enforces (`WS-MAX-ITEMS-IN-PACK`, `WS-MAX-SUB-PACKS`), so a workload matrix that stops well short of them would miss exactly the region where COBOL's own (and, per the current C# implementation, the recursive per-sub-pack re-explosion strategy's own) behavior is least linear. **Open architectural question, not resolved by this document:** the current C# `KitComponentPricingService` (T050) re-explodes every sub-pack as its own fresh top-level request and prices every returned component through a full `PricingOrchestrator.PriceAsync` call — for a kit with many sub-packs each containing many components, this call tree can grow multiplicatively with depth × breadth. Whether COBOL's own call volume through `A6O015U` (BLOCKED, not supplied) scales the same way is unknown; this dimension is exactly where a mismatch in *scaling behavior*, not just per-call latency, would first become visible, so it deserves explicit coverage rather than being inferred from small-kit numbers. |
| 4 | **Rebate/vendor-adjustment presence** | (a) no cost contract found (rebate paragraph never entered — `WS-COST-CONT-FND` gate); (b) cost contract found, rebate options computed (`R-REBATE-000..006`); (c) (b) plus a vendor cost adjustment also applying. | `R-REBATE-000`'s own precondition is `WS-COST-CONT-FND='Y'` — the entire rebate computation (and its own upstream `VNG03`/`7105-SEL-PRC-LST-010` lookup for the rebate base cost) is skipped outright whenever no contract was found, so this is a real, data-driven fork in DB2 work volume, not a uniform cost every line pays. |
| 5 | **Special-contract vs. normal request** | (a) normal; (b) `OMGPR-F-SPECIAL-CONTRACT='Y'`. | The special-contract path uses a *narrower* query (bypass-flagged rows only, `R-COST-001` item 5) and skips the account-level rounding stage and normal adjustment processing (`R-SPECIAL-001`) — likely cheaper per-line than the normal path, but a distinct enough code shape that it should be measured separately rather than assumed to scale the same as (1)'s levels. |
| 6 | **Order-line count per order** | 1, 10, 50+ lines. | A linear scaling factor orthogonal to every dimension above — needed to distinguish "this workload dimension is expensive per line" from "this order just has a lot of lines," and to validate that per-line cost doesn't degrade non-linearly as line count grows (e.g. from an unbounded working-storage array, connection-pool contention, or similar cross-line resource pressure that wouldn't show up in single-line measurements at all). |

---

## 3. What T059 should measure, matched dimension-for-dimension

T059's own task text already names the right metric categories (DB calls, latency percentiles,
throughput, memory, error rate, kit-size effects) — this section exists only to tie those metrics
explicitly back to §2's dimensions, so the two tasks don't independently invent incompatible
workload definitions:

- **DB calls** should be reported per request, broken out by which of §2's dimension-1 cost-cascade
  levels the request actually resolved to (a request that hits the individual-contract fast path and
  one that walks a full buy-group ancestor chain are not the same "DB calls" number, and averaging
  them together would hide exactly the comparison this document exists to enable).
- **Kit-size effects** (T059's own explicit metric) should be measured across all four levels of §2
  dimension 3, specifically including the region near the 699-item/300-sub-pack boundaries, not only
  small/typical kits — that boundary region is where a scaling mismatch between the C# recursive
  traversal strategy and whatever COBOL/`A6O015U` actually does would first appear.
- **Error rate** should be measured separately for the genuinely-fatal-error-heavy paths this
  project's error-catalog.md already documents (182 of 183 catalogued `OMGPR-Q-ERROR-NBR` paths are
  fatal-abend paths, not soft-continue) — a representative workload should include a deliberate
  fraction of requests that exercise known fatal paths (e.g. special-contract-not-found, `#601`),
  not only the happy path, since fatal-path latency/resource cost can differ from the happy path.

---

## 4. Open items

1. **Actual COBOL baseline numbers — BLOCKED.** No DB2/CICS/COBOL execution environment has been
   available to this project. This document supplies the method and dimension matrix only; running
   it against the live or QA COBOL system to produce real numbers is a separate, environment-dependent
   next step this project cannot take on its own.
2. **Specific performance/parity thresholds required for cutover — unresolved, a business decision.**
   `spec.md`'s own open question #4 asks this directly; this document does not answer it, since
   "how much slower/faster is acceptable" is a product/business tradeoff, not something derivable
   from the COBOL source itself.
3. **Whether CICS-side per-transaction (not cross-request) working-storage reuse exists** that
   could make a single, very large CICS pseudo-conversational task cheaper per-DB2-call than an
   equivalent sequence of independent C# calls — not confirmed from the supplied source, flagged for
   verification against the live environment before §1.3's "no cross-request caching" fair-comparison
   assumption is treated as fully settled.
4. **`A6O015U`'s own call-volume scaling behavior for large kits — BLOCKED**, same standing blocker
   as every other characterization of `A6O015U`'s internals in this project (`kit-processing.md`,
   `kit-decision-table.md`). §2 dimension 3's "open architectural question" cannot be closed without
   either `A6O015U`'s source or a live/captured trace of its actual behavior at scale.

# G1 Discovery Implementability Review

- Gate: G1
- Owner: Codex
- Reviewer: Codex
- Initial review date: 2026-07-20
- Re-review date: 2026-07-20
- Final remote handoff reviewed: `ff0c9f0e31e0dd85055c2ef8a62033fa2ffb21e6`
- Decision: **APPROVED WITH SCOPED DOWNSTREAM BLOCKERS**

Discovery is sufficiently complete for implementation planning and confirmed-scope work. Behavior marked `INFERRED` or `BLOCKED` remains prohibited from implementation until separately resolved.

## Evidence reviewed

- Repository constitution, specification, implementation plan, and task list
- COBOL and copybook sources currently available in this repository
- Claude discovery commit `f52b64d` on `origin/agent/claude-discovery`
- Existing .NET foundation and domain model work in this repository

The discovery artifacts are committed and available as durable shared-project evidence on `origin/agent/claude-discovery`. T001/T003/T005/T007–T010 were reviewed from `f52b64d`; the completed T002/T004/T006 handoff and full Phase 1 state were reviewed from `ff0c9f0`, without merging the branch into the dirty Codex worktree.

`git fetch origin` was attempted first as required, but the sandbox could not write the shared
worktree's `FETCH_HEAD`. A subsequent read-only `git ls-remote` was blocked by network policy. The
existing `origin/agent/claude-discovery` ref resolves to `ff0c9f0e31e0dd85055c2ef8a62033fa2ffb21e6`
(2026-07-20 16:32:47 -0500), and every assessment below was made by reading objects from that ref,
not by assuming that files missing from the current worktree were unavailable.

## Task review

| Task | Prior gap status | Implementability assessment |
|---|---|---|
| T001 | **Resolved** | The durable program inventory covers all 26 supplied source files plus the reference workbook, uses confidence labels, inventories major interfaces/paragraphs/errors, and explicitly lists missing dependencies. The prior missing-deliverable gap is closed. |
| T002 | **Resolved** | The standalone call graph documents seven verified edges, COMMAREA structures, routing conditions, response handling, and a consolidated unresolved-target table. Unsupplied callees are explicitly `BLOCKED`, which is the required treatment rather than a documentation gap. |
| T003 | **Partially resolved; downstream compatibility blocker remains** | The missing dictionary gap is resolved: 287 leaf fields, 22 groups, and 135 level-88 values are inventoried. The 1,789-byte contract is not reconciled: the mechanical layout is 1,773 bytes with mainframe COMP sizing or 1,771 with minimal sizing, leaving 16–18 bytes unexplained. This is documented and blocks T018/G2 byte compatibility, but it does not block G1 approval. |
| T004 | **Resolved; explicitly blocked internals accepted** | The OMGEXPL dictionary reconciles exactly to the 23,367-byte COMMAREA, and kit processing covers validation, routing, capacity, nested quantity, UOM, rollup forwarding, errors, and expiration. `OMGPK.CPY` and `A6O015U` internals are absent from `upload/` and explicitly `BLOCKED`; that is acceptable discovery treatment. |
| T005 | **Resolved** | The SQL CSV contains 417 entries and captures location, statement kind, tables, host variables, filters/order, SQLCODE handling, cursor attributes, and raw SQL. The DB2 inventory identifies unavailable DCLGEN layouts as `BLOCKED` instead of guessing them. |
| T006 | **Resolved; explicitly blocked internals accepted** | The catalog accounts for 213 A6U01 assignment sites and 183 distinct numeric codes, distinguishes error number from severity, classifies stop/continue behavior, and covers validation, SQL, CICS, kit, component, overflow, and surcharge paths. Missing `A6P001WB` NDP semantics are explicitly `BLOCKED`. |
| T007 | **Resolved for discovery; conditional implementation** | The ordered cost document covers 11 rules across individual/account/group/special/healthcare/acquisition paths, with source paragraphs and confidence per rule. Previously incomplete rule evidence is now captured; DCLGEN-backed details and unavailable behavior remain individually labeled `INFERRED` or `BLOCKED` and cannot be implemented yet. |
| T008 | **Resolved for discovery; conditional implementation** | The sell document covers eight sell/price-lock rules, including group-contract and acquisition cascades and post-calculation lock reconciliation. Previously missing hierarchy and lock evidence is documented; exact unavailable DCLGEN fields, percentage provenance, and helper internals remain scoped rather than invented. |
| T009 | **Resolved for discovery; conditional implementation** | The fee document contains 37 ordered rules across all requested fee areas, including negative findings such as label/application being subsumed by JIT. Missing `CUS120` eligibility/override logic, unavailable field layouts, and specifically deferred formulas are explicitly labeled, so they are accepted blockers rather than reasons to reject discovery. |
| T010 | **Resolved for discovery; downstream parity blocker remains** | Date selection, rounding, and a 17-scenario catalog document inclusive boundaries, expiration accumulation, rounding stages, and scenario confidence. Default COBOL rounding-tie behavior and inferred/blocked expected outcomes still require runtime evidence before T046/parity acceptance, but the uncertainty is clearly bounded. |

## Scoped downstream blockers

1. Preserve the explicit blockers for missing programs and libraries, including A6O015U, CUS120, A6P001WB, CUR120, OMGPK, and referenced DCLGENs; downstream work must not implement those behaviors by inference.
2. Reconcile the OMGPR 1789-byte statement with an authoritative live compiler layout and COMMAREA capture before compatibility decoding begins.
3. Confirm the platform rounding-tie mode and replace inferred/illustrative scenarios with authoritative expected outputs before using them for parity acceptance.
4. Keep OMGPK field layout, A6O015U explosion/rollup internals, CUS120 fee eligibility, and A6P001WB NDP behavior blocked until authoritative sources or observed fixtures are available.

## Required corrections to existing implementation assumptions

- Do not implement behavior labeled inferred or blocked without an approved resolution.
- `OMGPR-I-CONTRACT` is `S9(8) COMP`; it must not be modeled as the `X(20)` group-contract number. `OMGPR-I-GRP-CNT-NBR` is the separate `X(20)` field.
- Buying-group identifiers, ship/bill suffixes, and conversion factors must preserve their confirmed COBOL widths, signs, and scales rather than remain unconstrained primitives.
- Structural characterization scenarios are not substitutes for authoritative parity fixtures with confirmed outputs.

## Approval rationale

All T001–T010 deliverables exist, are marked complete, cite supplied COBOL/copybooks, use confidence labels, and explicitly identify unavailable evidence instead of inventing behavior. This satisfies the discovery gate and G1's requirement that evidence gaps be resolved or marked as blockers. Approval does not convert any `INFERRED` or `BLOCKED` statement into implementable behavior, does not approve OMGPR compatibility, and does not waive later parity gates.

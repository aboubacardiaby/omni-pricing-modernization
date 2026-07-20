# A6U01 — Characterization Scenario Catalog (T010)

**Scope:** Minimum characterization scenario matrix for the date-selection and rounding mechanisms
documented in `docs/rules/date-selection.md` and `docs/rules/rounding.md` — boundary behavior,
null dates, closest-expiration resolution, and rounding stages, per T010's acceptance criteria.
Each scenario states concrete input conditions and the expected decision path/output, traced to
the specific rule ID that governs it, so a future implementer (or Codex) can turn these directly
into test cases without re-deriving the COBOL logic.

**Not in scope:** a comprehensive scenario matrix for cost-selection or sell-selection business
rules — those decision tables (`docs/rules/cost-selection-rules.md`, T007;
`docs/rules/sell-selection-rules.md`, T008) are not yet built (`tasks.md` shows both `[ ]`
unclaimed as of this writing), so scenarios here are limited to the date/rounding mechanisms this
task actually covers, plus a small number of illustrative scenarios drawn from the already-built
`docs/rules/fees-and-adjustments.md` (T009) to show how those mechanisms interact with real fee
rules. A full scenario catalog spanning T007/T008/T009 together should be treated as follow-on
work once T007/T008 exist, not assumed complete here.

**Source of truth:** every scenario below is derived directly from a specific rule in
`docs/rules/date-selection.md` or `docs/rules/rounding.md` (cited per scenario); no new COBOL
evidence was read to build this document.

**Confidence key:** CONFIRMED (the expected outcome follows directly and unambiguously from
already-CONFIRMED source rules), INFERRED (outcome follows from an INFERRED source-rule detail),
BLOCKED (outcome cannot be stated because the governing rule is itself BLOCKED).

---

## Date-selection scenarios (`docs/rules/date-selection.md`)

| ID | Scenario | Input conditions | Expected outcome | Governing rule | Confidence |
|---|---|---|---|---|---|
| SCN-DATE-001 | Fully null expiration, single source | One fee source contributes a candidate whose expiration column is DB2 `NULL` (indicator `-1`). | The null date is never passed to `7695-ADD-EXP-DATE-ARRA-010` at all (the caller's own `IF <FIELD>-NI NOT = -1` guard skips the call). If this was the *only* candidate source touched this pricing pass, `WS-Q-CHECK` stays 0 and `OMGPR-D-EXPIRATION` is explicitly set to `SPACES`. | R-DATE-001, R-DATE-004 (item 6, `WS-Q-CHECK = 0` branch) | CONFIRMED |
| SCN-DATE-002 | Effective date exactly equal to pricing date | A fee-row's `_EF` (effective) column equals `WS-CURRENT-DB2-DATE` exactly. | The row matches (`<=` is inclusive) — it is eligible for selection on the effective-date boundary itself, not excluded until the day after. | R-DATE-002 | CONFIRMED |
| SCN-DATE-003 | Expiration date exactly equal to pricing date | A fee-row's `_EX` (expiration) column equals `WS-CURRENT-DB2-DATE` exactly, and is not null. | The row still matches (`>=` is inclusive) — a row expiring "today" is still usable today; exclusion begins the day after expiration. | R-DATE-002 | CONFIRMED |
| SCN-DATE-004 | Effective date one day in the future | A fee-row's `_EF` is `WS-CURRENT-DB2-DATE + 1`. | The row does not match the WHERE clause at all — excluded from the result set, falls through to whatever fallback that fee area's cascade defines (e.g. R-FREIGHT-002 after R-FREIGHT-001 in `fees-and-adjustments.md`). | R-DATE-002 | CONFIRMED |
| SCN-DATE-005 | Multiple candidate dates, distinct values | Three different fee sources each contribute a non-null expiration date in the same pricing pass: `2026-09-01`, `2026-07-25`, `2026-12-31`. | `OMGPR-D-EXPIRATION` resolves to `2026-07-25` (the earliest) — `7720-PRO-EXPIRE-DATES-010`'s linear scan keeps the minimum, not the first-added or last-added. | R-DATE-003, R-DATE-004 | CONFIRMED |
| SCN-DATE-006 | Duplicate candidate dates | Two fee sources both contribute the identical expiration date. | Both are still appended to `WS-D-CHECK` by R-DATE-003 (accumulation is unconditional), but R-DATE-004's dedup check (`WS-D-CHECK(WS-SUB1) NOT = WS-D-CHECK-HOLD`) means the second identical value is skipped as a no-op comparison once the first has already become the running hold value — same final result (`OMGPR-D-EXPIRATION` = that shared date), just a harmless redundant array slot consumed. | R-DATE-003, R-DATE-004 | CONFIRMED |
| SCN-DATE-007 | Candidate-array overflow | A single pricing pass triggers 51 or more calls to `7695-ADD-EXP-DATE-ARRA-010` (e.g., an order line with an unusually large number of applicable fee/cost/sell sources all contributing distinct non-null dates). | The 51st call sets `OMGPR-Q-ERROR-NBR = 145`, `OMGPR-F-PRICER-ERROR = 'Y'`, and returns WITHOUT storing the 51st date or aborting the pricer directly. Whether the overall pricing call then fails depends entirely on whether the *specific* calling paragraph checks `OMGPR-PRICER-ERROR` immediately afterward — confirmed true for at least one site (`0205-PRO-FREIGHT-COST-010`), not verified for all. **This scenario's actual end-to-end outcome is only INFERRED, not CONFIRMED, for call sites other than the one directly verified**, and would benefit from a real execution-environment test rather than static analysis alone. | R-DATE-003 item 10 | INFERRED |
| SCN-DATE-008 | JIT-ON-COST pricing mode | `OMGPR-JIT-ON-COST` = `'J'` selected as the pricing-request switch for this call. | `0050-PROCESS-EXP-DATE` (and therefore both the rounding stage and the closest-expiration resolution) is never reached — the top-level dispatcher routes straight to `7225-PRO-JIT-ADJ-010` and back. `OMGPR-D-EXPIRATION` is left at whatever value it held on entry to this call (not explicitly blanked or set), and `OMGPR-C-ACCT-ROUNDING`'s configured mode has no effect for this call. | R-DATE-000 item 9 | CONFIRMED |
| SCN-DATE-009 | Blank/spaces stored date value in the array | A `WS-PRICE-EXP-DATE` staging value that is spaces/blank somehow reaches the array (e.g. a caller populates the array without actually having a real date — not confirmed to occur in practice, but the resolver explicitly guards against it). | R-DATE-004's loop explicitly skips any array entry that is not `> SPACES`, so a blank entry never becomes the running hold value and never causes a false "earliest date" result. | R-DATE-004 item 6 | CONFIRMED (defensive code path; whether it is ever actually exercised in practice was not verified) |

## Rounding scenarios (`docs/rules/rounding.md`)

| ID | Scenario | Input conditions | Expected outcome | Governing rule | Confidence |
|---|---|---|---|---|---|
| SCN-ROUND-001 | No-rounding account, non-JIT-ON-COST pricing | `OMGPR-C-ACCT-ROUNDING` = `'N'` or space; computed `OMGPR-A-CUS-UOM-SELL-PRC` = `12.345678` (full native precision). | `7715-PRO-ROUNDING-010` exits immediately (`WHEN WS-NO-ROUNDING -> GO TO 7715-EXIT`); `OMGPR-A-CUS-UOM-SELL-PRC` is returned at full native precision, `12.345678`, not truncated to 2 decimals. `OMGPR-A-CUS-UOM-SELL-PRC-UN` equals the same value (both fields identical in this mode). | R-ROUND-001 | CONFIRMED |
| SCN-ROUND-002 | Normal-round-two, exact half-cent value | `OMGPR-C-ACCT-ROUNDING` = `'R'`; pre-rounding unit price = `12.345`. | `COMPUTE ... ROUNDED = 1.0 * 12.345` — under the assumed platform default (round-half-away-from-zero, INFERRED per `rounding.md`'s `R-ROUND-002` item 6, not independently confirmed against an actual compiler), rounds to `12.35` (the 3rd-decimal `5` rounds the 2nd decimal up). `OMGPR-A-CUS-UOM-SELL-PRC-UN` retains the pre-rounding `12.345` regardless. **The exact behavior at a true half-cent tie is the one detail in this whole rounding mechanism that is INFERRED rather than CONFIRMED** — flagged explicitly since it is exactly the kind of boundary case most likely to differ between COBOL runtime implementations. | R-ROUND-001, R-ROUND-002 | INFERRED |
| SCN-ROUND-003 | Round-up-two, tiny nonzero remainder | `OMGPR-C-ACCT-ROUNDING` = `'Y'`; pre-rounding unit price = `12.3401` (a remainder of only `0.0001` beyond 2 decimals). | `0.0049 + 12.3401 = 12.3450`; `ROUNDED` to 2 decimals (3rd digit `5`) rounds up to `12.35` — i.e. even a one-hundredth-of-a-cent remainder is enough to push the price up a full cent under this mode. This is the confirmed "ceiling to the cent" behavior documented in `R-ROUND-001` item 6 — worth a dedicated test case specifically because the effect is easy to underestimate from the field's name ("round up") without doing the arithmetic. | R-ROUND-001 | CONFIRMED (arithmetic re-derived directly, not merely asserted) |
| SCN-ROUND-004 | Round-up-two, already-exact 2-decimal value | `OMGPR-C-ACCT-ROUNDING` = `'Y'`; pre-rounding unit price = `12.3400` exactly (no remainder beyond 2 decimals). | `0.0049 + 12.3400 = 12.3449`; `ROUNDED` to 2 decimals (3rd digit `4`) rounds down/stays at `12.34` — an already-exact 2-decimal value is NOT pushed up a cent by this mode, confirming the bias only affects values that actually have a sub-cent remainder. | R-ROUND-001 | CONFIRMED |
| SCN-ROUND-005 | Unrecognized rounding code | `OMGPR-C-ACCT-ROUNDING` = `'X'` (not `N`/space/`Y`/`R`). | `WHEN OTHER -> GO TO 7715-EXIT` — behaves identically to `WS-NO-ROUNDING` (SCN-ROUND-001), with no error raised and no diagnostic. | R-ROUND-001 item 10 | CONFIRMED |
| SCN-ROUND-006 | Unrounded-value preservation across all three modes | Same pre-rounding unit price (e.g. `12.345678`) priced three times, once under each of `N`, `Y`, `R`. | `OMGPR-A-CUS-UOM-SELL-PRC-UN` and `OMGPR-A-TOTAL-SELL-UN` equal `12.345678`-equivalent (the true unrounded computed value) in **all three** cases — these two fields are populated before the `EVALUATE TRUE` runs and are never touched by any of the three branches. A test asserting "the `-UN` fields always carry the unrounded value regardless of account rounding mode" should pass identically across all three modes. | R-ROUND-001 item 9 | CONFIRMED |
| SCN-ROUND-007 | Surcharge amount, no ROUNDED keyword | A line with a matched surcharge percentage (per `fees-and-adjustments.md`'s `R-SURCHARGE-001`) where `OMGPR-A-TOTAL-COST * OMGPR-P-SURCHARGE` has a nonzero remainder beyond the target field's decimal precision. | `OMGPR-A-SURCHARGE` (and the subsequent `OMGPR-A-TOTAL-SELL` fold-in) is **truncated**, not rounded — this is the sole confirmed exception to R-ROUND-002's pervasive `ROUNDED` convention. A characterization test comparing surcharge's output against a "rounded" expectation would fail; the correct expected value truncates the excess digits. | `fees-and-adjustments.md` R-SURCHARGE-002; `rounding.md` R-ROUND-002 item 9 | CONFIRMED |

## Cross-fee illustrative scenarios (from `docs/rules/fees-and-adjustments.md`)

A small set of scenarios showing the date/rounding mechanisms operating inside real fee rules,
not a substitute for a full T007/T008/T009 scenario matrix.

| ID | Scenario | Input conditions | Expected outcome | Governing rule | Confidence |
|---|---|---|---|---|---|
| SCN-FEE-001 | Rebate max-cap boundary at zero | `OMGPR-A-CNT-LN-MAX-REBT` = `0` (no cap configured), computed total rebate = `50.00`. | The cap does NOT apply (`R-REBATE-006`'s guard requires `MAX-REBT > 0`) — the rebate stays at `50.00`, not zeroed. This is the documented footgun: a naive `>= 0` reimplementation would incorrectly zero every uncapped rebate. | fees-and-adjustments.md R-REBATE-006 | CONFIRMED |
| SCN-FEE-002 | PANDAC price-protection window, pricing date one day after window end | `OMGPR-D-CNT-PROT-END` = `2026-07-17`, pricing date (`WS-D-PRICING-COMPARE`) = `2026-07-18`. | The nested nested-IF nested `<=` end-date compare fails, so `OMGPR-A-CUR-PRICE-PROT` is not computed for this call — price protection is date-window-scoped using the same inclusive-boundary convention as R-DATE-002, applied to a working-storage comparison rather than a SQL WHERE clause. | fees-and-adjustments.md R-REBATE-004 | CONFIRMED |
| SCN-FEE-003 | Freight account-vendor row found, expiration null | `VNG31` row matches on account+vendor, `D_AC_VN_FREIGHT_EX` is DB2 `NULL`. | Freight amount computes normally (R-FREIGHT-001); per R-DATE-001, the null expiration is never added to the closest-expiration array from this source — if this is the only fee/cost/sell source touched this pass, `OMGPR-D-EXPIRATION` resolves per SCN-DATE-001. | fees-and-adjustments.md R-FREIGHT-001; date-selection.md R-DATE-001 | CONFIRMED |

---

## What was scoped out of this pass

- A comprehensive scenario matrix for `docs/rules/cost-selection-rules.md` (T007) and
  `docs/rules/sell-selection-rules.md` (T008) — neither document exists yet (`tasks.md` shows both
  unclaimed as of this writing). This catalog's three "cross-fee illustrative scenarios" are a
  small sample from the already-built T009 document, not a substitute.
- Execution-verified outcomes for any scenario above — every expected outcome was derived by
  static analysis of the COBOL source, not by running the pricer in a real DB2/CICS environment.
  SCN-DATE-007 and SCN-ROUND-002 are explicitly flagged INFERRED rather than CONFIRMED for this
  reason.

## Assumptions

1. Carries forward every assumption already stated in `docs/rules/date-selection.md` and
   `docs/rules/rounding.md` (10-character date layout, compiler default rounding mode) — not
   restated in full here.

## Blockers

1. No DB2/CICS/COBOL execution environment to verify any scenario's actual runtime outcome — the
   standing blocker across this entire document set, most consequential here since a scenario
   catalog's whole purpose is enabling test-case construction, and static analysis alone cannot
   confirm true half-cent-tie rounding behavior (SCN-ROUND-002) or full error-escalation coverage
   for the array-overflow path (SCN-DATE-007).
2. T007/T008 not yet built — limits this catalog's cost/sell-selection coverage to the three
   illustrative scenarios drawn from T009.

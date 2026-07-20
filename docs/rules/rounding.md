# A6U01 — Rounding Rules (T010)

**Scope:** Rounding conventions used across all pricing calculations — the intermediate
per-formula `ROUNDED` convention already applied (and individually cited) throughout
`docs/rules/fees-and-adjustments.md`, plus the final, account-configurable output-rounding stage
that has not been documented anywhere else in this repository until now.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`. Central mechanism:
`7715-PRO-ROUNDING-010` (lines 17364-17408), called from `0050-PROCESS-EXP-DATE`
(lines 2621-2633) immediately before the closest-expiration-date resolution documented in
`docs/rules/date-selection.md`. No `ROUNDED MODE IS ...` clause appears anywhere in
`A6U01.CBL` — every `COMPUTE ... ROUNDED` in this codebase uses the compiler's default rounding
mode (see R-ROUND-002 item 6 for what that default is, and its confidence caveat).

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## R-ROUND-001 Final account-level output rounding

1. ID/Name: R-ROUND-001 Account-configurable final rounding of the two headline sell fields
2. Program/paragraph: `7715-PRO-ROUNDING-010`, lines 17364-17408; called from
   `0050-PROCESS-EXP-DATE` (2621-2633), which runs at the end of every pricing mode except
   JIT-ON-COST (see `docs/rules/date-selection.md`'s `R-DATE-000` item 9)
3. Preconditions: runs unconditionally once per completed (non-JIT-ON-COST) pricing pass, after
   all cost/sell/fee computation is finished — this is the LAST calculation step before
   closest-expiration-date resolution
4. Data deps: `OMGPR-C-ACCT-ROUNDING` (COMMAREA input field, a caller-supplied account-level
   rounding-mode code — MOVEd into working-storage `WS-C-ACCT-ROUNDING` earlier in the paragraph
   chain, at line 12299, inside cost-side setup) with three defined 88-level values:
   `WS-NO-ROUNDING` (`'N'` or space), `WS-UP-TWO-ROUNDING` (`'Y'`), `WS-NORMAL-TWO-ROUNDING`
   (`'R'`)
5. Priority: single `EVALUATE TRUE` on the account's rounding-mode code, mutually exclusive by
   construction — **this rounding mode is a per-account configuration value passed in on every
   pricing call, not looked up via SQL** — it is entirely caller-supplied, so there is no
   "priority cascade" the way most fee-area lookups have; whatever the caller passes in
   `OMGPR-C-ACCT-ROUNDING` for this call directly selects the branch.
6. Calculation, three modes:
   - **No rounding** (`WS-NO-ROUNDING`, the default for blank/`'N'`): `GO TO 7715-EXIT`
     immediately — `OMGPR-A-CUS-UOM-SELL-PRC` and `OMGPR-A-TOTAL-SELL` are left at whatever
     precision their own `PIC` clauses already carry (their full computed precision, not
     truncated to 2 decimals at all).
   - **Round up to two** (`WS-UP-TWO-ROUNDING`, `'Y'`): `COMPUTE WS-A-UNIT-PRICE-ROUND ROUNDED =
     0.0049 + <field>`, then move the result back into the 2-decimal-precision field
     (`WS-A-UNIT-PRICE-ROUND` is declared `PIC S9(07)V9(02) COMP-3`, lines 1062-1064 — the
     2-decimal truncation happens via this target field's own `PIC` clause, not a separate
     truncation step). **Adding `0.0049` before rounding is a deliberate bias, not a rounding-mode
     name that happens to say "up"**: for a positive sell price, this causes ANY nonzero
     fractional remainder beyond 2 decimals (as small as `0.0001`) to push the 3rd decimal digit
     high enough to round the 2nd decimal up under standard round-half-up — in effect this
     implements a **ceiling-to-the-cent** function for positive values, not merely "round 0.005
     up instead of down." Confirmed by direct arithmetic: e.g. a raw value of `X.XX01` (any
     nonzero 3rd-decimal-and-beyond remainder) plus `0.0049` crosses the `X.XX+0.005` round-up
     threshold that plain `ROUNDED` alone would not reach for a remainder that small.
   - **Normal round to two** (`WS-NORMAL-TWO-ROUNDING`, `'R'`): `COMPUTE WS-A-UNIT-PRICE-ROUND
     ROUNDED = 1.0 * <field>` — a plain multiply-by-one-and-round, i.e. standard COBOL `ROUNDED`
     truncation to the target's 2-decimal `PIC` clause with no bias added (see R-ROUND-002 for
     what "standard" means here).
   Both the up-two and normal-two branches apply the identical formula to BOTH
   `OMGPR-A-CUS-UOM-SELL-PRC` (unit price) and `OMGPR-A-TOTAL-SELL` (line total) independently —
   CONFIRMED, two separate `COMPUTE`s per branch, not one rounding applied and then propagated by
   multiplication (i.e. `TOTAL-SELL`'s rounding is not derived from the already-rounded unit
   price; each field is rounded from its own pre-rounding value).
7. Output: `OMGPR-A-CUS-UOM-SELL-PRC`, `OMGPR-A-TOTAL-SELL` (rounded in place, in the up-two/
   normal-two branches); `OMGPR-A-CUS-UOM-SELL-PRC-UN`, `OMGPR-A-TOTAL-SELL-UN` (see item 9)
8. Dates: none — this rule is purely numeric
9. Exclusions/fallbacks: **CONFIRMED — the unrounded values are always preserved**, regardless of
   which of the three modes applies: `OMGPR-A-CUS-UOM-SELL-PRC-UN = OMGPR-A-CUS-UOM-SELL-PRC` and
   `OMGPR-A-TOTAL-SELL-UN = OMGPR-A-TOTAL-SELL` are both populated (lines 17376-17378)
   unconditionally, BEFORE the `EVALUATE TRUE` that applies (or skips) rounding. Any downstream
   consumer needing the true, unrounded computed price has these `-UN` fields available in every
   rounding mode, including `WS-NO-ROUNDING` (where they are simply identical to the non-`-UN`
   fields, since no rounding was applied to either).
10. Errors: none in this paragraph — an `OMGPR-C-ACCT-ROUNDING` value outside `{N/space, Y, R}`
    falls to `WHEN OTHER -> GO TO 7715-EXIT` (line 17401-17402), the same silent-no-op idiom
    documented repeatedly in `fees-and-adjustments.md` (e.g. `R-JIT-001` item 9) — an unrecognized
    rounding code behaves identically to `WS-NO-ROUNDING`, with no diagnostic.
11. Confidence: CONFIRMED for the three modes' control flow and formulas. INFERRED that the
    `0.0049` bias's practical effect is "ceiling to the cent" for all realistic positive sell
    prices — not exhaustively proven for every possible input magnitude/sign, though the
    arithmetic reasoning is straightforward and was independently re-derived, not merely assumed
    from the field name.

## R-ROUND-002 Intermediate per-formula rounding (`COMPUTE ... ROUNDED`)

1. ID/Name: R-ROUND-002 Standard intermediate rounding convention used by nearly every fee formula
2. Program/paragraph: pervasive — every `COMPUTE` in every rule of `docs/rules/fees-and-
   adjustments.md` except the confirmed exception in item 9 uses the `ROUNDED` phrase
3. Preconditions: applies at the point each individual fee/rebate/cost amount is computed, well
   before R-ROUND-001's final output-stage rounding runs
4. Data deps: none beyond the formula's own operands (already documented per rule in
   `fees-and-adjustments.md`)
5. Priority: N/A — this is a formula-level detail of each already-documented rule, not a
   competing rule of its own; recorded here once as the shared convention rather than repeated in
   every citing rule
6. Calculation: plain `COMPUTE <target> ROUNDED = <expression>`, with **no `ROUNDED MODE IS ...`
   clause anywhere in `A6U01.CBL`** (confirmed by search) — meaning every one of these `ROUNDED`
   phrases uses whatever the compiler's default rounding mode is. For IBM Enterprise COBOL (the
   most likely compiler for a CICS/DB2 mainframe program of this vintage and naming convention,
   though the exact compiler/runtime was not itself confirmed anywhere in the supplied source),
   the default `ROUNDED` behavior is "round half away from zero" (nearest value, ties round away
   from zero) — this is standard platform behavior, not a business rule this codebase defines,
   and is stated here as INFERRED rather than CONFIRMED since no supplied source names the actual
   compiler or its configured default explicitly.
7. Output: whatever target field each individual formula in `fees-and-adjustments.md` already
   documents
8. Dates: none
9. Exclusions/fallbacks: **the one confirmed exception in the entire fee-area survey is
   Surcharge** (`R-SURCHARGE-002` in `fees-and-adjustments.md`) — both
   `OMGPR-A-SURCHARGE`'s `COMPUTE` and its fold-in `COMPUTE OMGPR-A-TOTAL-SELL = ...` use plain
   `COMPUTE` with no `ROUNDED` keyword at all, meaning DB2/COBOL's default *truncation* (not
   rounding) applies at whatever precision the target field's `PIC` clause allows. This was
   already flagged in `fees-and-adjustments.md` and is repeated here as the canonical location
   for cross-fee-area rounding conventions; do not "normalize" this to `ROUNDED` in a
   reimplementation on the assumption that the omission was accidental — it may well be, but
   nothing in the supplied source confirms or denies intent, so behavior-preservation requires
   keeping it exactly as written (truncating) unless the business explicitly confirms otherwise.
10. Errors: none
11. Confidence: CONFIRMED that `ROUNDED` (no `MODE` clause) is the pervasive convention and that
    Surcharge is the sole confirmed exception found while building `fees-and-adjustments.md`.
    INFERRED (not CONFIRMED) that the compiler's default rounding-half-away-from-zero applies,
    since the actual compiler/runtime configuration is not stated in any supplied source —
    **flagged as a blocker for anyone needing bit-exact reproduction of rounding at the boundary
    cases** (e.g. exact half-cent ties), since a different compiler or a `ROUNDED MODE` compiler
    option set outside the source file itself could change this silently.

---

## What was scoped out of this pass

- **`WS-C-ACCT-ROUNDING`'s upstream population** — confirmed as a direct `MOVE` from
  `OMGPR-C-ACCT-ROUNDING` (a COMMAREA input field, line 12299), but the business rule for how the
  calling system decides what rounding code to pass in for a given account was not traced (out of
  scope — that decision is made by a caller outside `A6U01.CBL`).
- **Individual fee-formula rounding** already documented per-rule in `fees-and-adjustments.md` —
  not re-transcribed here; this document only adds the shared convention and the final-stage
  mechanism that sits on top of them.

## Assumptions

1. The compiler generating this COBOL is assumed to follow the common IBM Enterprise COBOL
   default `ROUNDED` behavior (round-half-away-from-zero) in the absence of any `ROUNDED MODE`
   clause — not confirmed from any supplied source (see R-ROUND-002 item 11).

## Blockers

1. The actual compiler and any compile-time `ROUNDED MODE` default/override configured outside
   `A6U01.CBL`'s own source text (R-ROUND-002 item 11) — needed for bit-exact reproduction of
   rounding at tie-breaking boundary values.
2. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set;
   the `0.0049`-bias "ceiling to the cent" interpretation (R-ROUND-001 item 6) was derived by
   arithmetic reasoning, not by observing actual runtime output.

# A6U01 — Buy-Group Low-UOM / Break-Bulk Charge Resolution (T008)

**Scope:** Decision-table extraction for `0245-PRO-BG-LOW-UOM-010` and its full call chain:
`7885-FIND-VEND-EXCL-010`, `7872-LOAD-ALTER-UOM`, `7874-FIND-CONV-FACT`,
`7875-FIND-ALT-UOM-DESIGNATOR`, `7880-FIND-LUOM-BB-CHG`, `7896-CALC-LUOM-OR-BB-FEE` (legacy) and
`7898-CALC-LUOM-OR-BB-FEE-NEW` (2022 fee-component version). This paragraph chain was flagged as an
unread blocker in three prior documents (`contract-cost-method.md`, and `jit-adjustment.md` twice)
before being extracted here.
**Correction to a prior finding — read this first:** `jit-adjustment.md`'s `R-JIT-003` (T006)
concluded that JIT break-bulk fee computation was "dead code" because the inline percent/rate
formula inside `7225-PRO-JIT-ADJ-010` is commented out. **That conclusion is wrong, or at least
incomplete.** This document shows the computation was not deleted — it was *relocated* into this
paragraph chain, which `7225` calls precisely when the inline break-bulk fee comes out to zero
(`R-JIT-003`'s own precondition). `jit-adjustment.md` has been updated with a correction note
pointing here; see this document's "Correction applied to T006" section at the end for the exact
wording.
**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`, lines 6021–6088 and
19050–19513. No DCLGEN copybook for `VNG05`, `BGG25`, `VNG20` is supplied in `upload/`.
**Confidence key:** same as prior decision-table documents.

---

## Control-flow map (CONFIRMED)

```text
7225-PRO-JIT-ADJ-010 (R-JIT-003's precondition: OMGPR-A-JIT-BREAK-FEE = 0, LUOM-eligible, stock order)
  └─ 0245-PRO-BG-LOW-UOM-010
       ├─ [OMGPR-F-LUOM-VEND-EXCL = 'Y'] → 7885-FIND-VEND-EXCL-010 (BGG25 vendor-exclusion check)
       │    └─ [excluded AND (LUOM-group OR break-bulk-group)] → GO TO 0245-EXIT (no charge at all)
       ├─ (always) resolve WS-ACTUAL-C-ORD-UOM / WS-ACTUAL-Q-ORD-LIN-ORDERED
       │    (alt-order-UOM override logic, independent of the rest of this chain)
       ├─ [OMGPR-Q-ORD-LIN-ORDERED > 1] → 7872-LOAD-ALTER-UOM (load all VNG05 alt-UOM rows into an array)
       │                                → 7874-FIND-CONV-FACT (find the alt-UOM whose factor evenly
       │                                   divides the ordered quantity with zero remainder)
       ├─ [else, ordered qty <= 1]      → 7875-FIND-ALT-UOM-DESIGNATOR (direct VNG05 lookup keyed by
       │                                   the actual order UOM)
       │                                   (both paths resolve WS-UOM-DESIGNATOR to 'L', 'B', or
       │                                    neither)
       └─ 7880-FIND-LUOM-BB-CHG
            └─ [WS-UOM-DESIGNATOR = 'L' or 'B']
                 ├─ [OMGPR-FEE-SHRT-CODE-BB > SPACES] → 7898-CALC-LUOM-OR-BB-FEE-NEW (2022 system)
                 └─ [else]                            → 7896-CALC-LUOM-OR-BB-FEE (legacy)
```

---

## Rule R-LUOM-001 — Buy-group vendor exclusion can suppress the entire charge before any UOM logic runs

1. **Rule ID / name:** R-LUOM-001 — If the buy-group's low-UOM vendor-exclusion flag is set and a
   `BGG25` exclusion row exists for this vendor, and the account/group's LUOM-or-break-bulk source
   is group-level, no charge is computed at all.
2. **Program / paragraph:** `A6U01`, `0245-PRO-BG-LOW-UOM-010` (lines 6025–6038) +
   `7885-FIND-VEND-EXCL-010` (lines 19252–19290).
3. **Preconditions:** `OMGPR-F-LUOM-VEND-EXCL = 'Y'` — CONFIRMED this field is populated by
   `CUP100`'s `A830-SEL-LOW-UOM` (per `program-inventory.md` §1.5, `BGG23-F-LUOM-VEND-EXCL`), so
   this rule's *first* gate is itself a cross-program dependency already documented, not derived
   fresh here.
4. **Data/SQL dependencies:** `7885` selects from `BGG25`, keyed by `OMGPR-I-BUY-GROUP-LUOM`,
   `OMGPR-D-BG-LOW-UOM-EFF`, `OMGPR-I-VENDOR` (all three populated upstream — `BG-LOW-UOM-EFF` and
   `I-BUY-GROUP-LUOM` specifically from the same `CUP100` `A830` paragraph). `SQLCODE = 0` or
   `-811` (the same "duplicate row exists" reuse-as-signal pattern already seen in
   `contract-cost-method.md`'s `R-COST-002` for `CCG25`) both mean "exclusion exists"; `100` or any
   other code means "no exclusion" (CONFIRMED — note `WHEN OTHER` here does **not** raise a fatal
   error the way most other `WHEN OTHER` branches in this codebase do; it treats an unexpected
   SQLCODE the same as not-found, while *also* still populating the DB-error fields and setting
   `OMGPR-F-PRICER-ERROR`... **actually CONFIRMED on closer read: `WHEN OTHER` at lines
   19272–19285 does set `WS-BG-LOW-VEXC-EXISTS-SW = 'N'` first but then falls through to the same
   fatal-error `MOVE`s and `GO TO 0020-EXIT-PRICER` as every other `WHEN OTHER` in this codebase —
   the `'N'` assignment is dead in that branch since the program aborts immediately after. Not a
   real behavioral difference, just an unusual ordering.**
5. **Priority relative to competing rules:** This is the first gate in the whole chain — if it
   fires, **every subsequent rule in this document (R-LUOM-002 through R-LUOM-005) is skipped**
   for this line via `GO TO 0245-EXIT`. The exclusion additionally requires
   `(OMGPR-F-LUOM-GROUP OR OMGPR-F-BREAK-BULK-GROUP)` — CONFIRMED, an **account**-level LUOM/BB
   source (`OMGPR-F-LUOM-ACCOUNT`/`OMGPR-F-BREAK-BULK-ACCOUNT`) is **not** subject to this
   vendor-exclusion short-circuit, only a group-level one is.
6. **Calculation and rounding stage:** N/A — this rule only decides whether to proceed.
7. **Output fields:** none written by this rule beyond the exclusion check itself; the exit skips
   all of R-LUOM-002 through R-LUOM-005's output fields.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** This rule *is* an exclusion mechanism — its "fallback" is simply
   "no low-UOM or break-bulk charge for this line."
10. **Errors:** `WHEN OTHER` on the `BGG25` select → error `301`, fatal, `GO TO 0020-EXIT-PRICER`.
11. **Confidence:** CONFIRMED.

---

## Rule R-LUOM-002 — Ordered-quantity-dependent UOM-designator resolution

1. **Rule ID / name:** R-LUOM-002 — Whether this line is treated as a "Low UOM" (`'L'`) or "Break
   Bulk" (`'B'`) situation is determined by matching the actual order quantity against the vendor's
   alternate-UOM conversion factors — and the *method* used to find that match itself depends on
   whether more than one unit was ordered.
2. **Program / paragraph:** `A6U01`, `0245` lines 6074–6082, `7872-LOAD-ALTER-UOM` (19050–19156),
   `7874-FIND-CONV-FACT` (19158–19192), `7875-FIND-ALT-UOM-DESIGNATOR` (19194–19233).
3. **Preconditions:** Runs after R-LUOM-001's exclusion gate passes (or wasn't applicable).
4. **Data/SQL dependencies:**
   - **`OMGPR-Q-ORD-LIN-ORDERED > 1`** (more than one unit ordered) →
     `7872-LOAD-ALTER-UOM`: opens cursor over `VNG05` (`SELECT` via `9580`/`9585`, keyed by
     `I_VENDOR`/`I_VND_PRODUCT`) and loads **every** alternate-UOM row for this vendor+product into
     a working array (`WS-ALT-UOM-ENTRY`/`WS-ALT-UOM-FACT-ENTRY`/`WS-ALT-UOM-DESIGNATOR`, up to
     `WS-MAX-ALTER-UOM-ENTRIES` — exceeding that is itself a fatal error, code `703`), with each
     factor pre-multiplied by `WS-BASE-ORD-CONV-FACTOR` (resolved earlier in `0245` from
     `VNG02-T-VND-PROD-UM-DESC`'s numeric-prefix parsing, lines 6047–6059 — a base-UOM-to-numeric
     extraction this document does not re-derive further). If `VNG05` has **no** rows at all
     (`SQLCODE = 100` on the very first fetch), a single synthetic entry is created instead, using
     `WS-BASE-ORD-CONV-FACTOR` as the factor and `VNG02-F-UOM-DESIGNATOR` (the product's own base
     designator, not an alternate one) as the designator (lines 19125–19131) — CONFIRMED, this
     ensures the array is never empty even for a product with no configured alternate UOMs.
     Then `7874-FIND-CONV-FACT`: walks the loaded array in order, dividing
     `WS-ACTUAL-Q-ORD-LIN-ORDERED` by each entry's factor and checking for a **zero remainder**
     (`DIVIDE ... GIVING ... REMAINDER ...`) — the **first** array entry that evenly divides the
     ordered quantity wins; its designator becomes `WS-UOM-DESIGNATOR`. If none divide evenly, the
     loop simply exhausts (`WS-SUBX > WS-NUMBER-OF-ALT-UOMS`) and `WS-UOM-DESIGNATOR` remains
     whatever `INITIALIZE WS-UOM-DESIGNATOR` set it to (blank) — CONFIRMED, no fallback beyond
     that; a designator that never resolves to `'L'` or `'B'` means R-LUOM-004/005 never fire (per
     `7880`'s own gate).
   - **`OMGPR-Q-ORD-LIN-ORDERED <= 1`** (one unit or fewer/unspecified) →
     `7875-FIND-ALT-UOM-DESIGNATOR`: a single direct `VNG05` select (`9330`) keyed by the *actual*
     UOM being ordered (`WS-ACTUAL-C-ORD-UOM`, resolved earlier in `0245`) — `SQLCODE = 0` →
     designator from the matched `VNG05` row; `SQLCODE = 100` → falls back to the product's base
     designator (`VNG02-F-UOM-DESIGNATOR`), the same fallback value used by `7872`'s no-rows case.
5. **Priority relative to competing rules:** These two resolution methods are mutually exclusive
   (single `IF`/`ELSE` on order quantity, `0245` lines 6074–6082) — **not** a "try one, then the
   other" cascade; exactly one runs per line. This is a genuinely different *algorithm* depending on
   quantity, not just a different data source: quantity `> 1` searches for a
   quantity-divides-evenly match across *all* alternates (which alternate UOM does this order
   quantity naturally pack into?), while quantity `<= 1` does a direct lookup on the *UOM itself*
   (what designator does this specific unit-of-measure have?) — two different questions that happen
   to produce a value in the same field.
6. **Calculation and rounding stage:** `7874`'s `DIVIDE ... REMAINDER` is integer/fixed-point
   division with an explicit remainder check, not `ROUNDED` — an exact-division test, not an
   approximation.
7. **Output fields:** `WS-UOM-DESIGNATOR` (`'L'`, `'B'`, or blank/other), consumed by `7880`/
   R-LUOM-003 onward.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** Both `VNG05`-not-found sub-cases fall back to the product's own
   base `VNG02-F-UOM-DESIGNATOR` rather than erroring — CONFIRMED at lines 19130–19131 and
   19213–19214, the same fallback value in both the quantity`>1` and quantity`<=1` branches.
10. **Errors:** `7872`'s cursor open/fetch failures → errors `701`/`702` (fatal); array-overflow →
    `703` (fatal); `7875`'s select failure → error `86` (fatal).
11. **Confidence:** CONFIRMED for the control flow and formulas. INFERRED (not independently
    verified) that `WS-BASE-ORD-CONV-FACTOR`'s derivation from `VNG02-T-VND-PROD-UM-DESC`'s numeric
    prefix (lines 6047–6059, a `PERFORM VARYING` scanning up to 5 characters for a leading numeric
    substring) always produces a sensible factor — this task read the mechanism but did not
    independently confirm what values `VNG02-T-VND-PROD-UM-DESC` actually takes in practice
    (BLOCKED, no DCLGEN for `VNG02`).

---

## Rule R-LUOM-003 — Account-level vs. group-level charge source (cross-link to `CUP100`)

1. **Rule ID / name:** R-LUOM-003 — Once classified as `'L'` or `'B'`, the actual percentage/amount
   used depends on whether the account or the buy-group is the source, per switches set entirely
   outside `A6U01`.
2. **Program / paragraph:** `A6U01`, `7896-CALC-LUOM-OR-BB-FEE`, lines 19356–19380 (this same
   account-vs-group split is *not* present in `7898`, the 2022 version — see R-LUOM-005).
3. **Preconditions:** `WS-UOM-DESIGNATOR = 'L'` or `'B'` (from R-LUOM-002); this specific
   account-vs-group branching only applies to the **legacy** path (`7896`), reached when no 2022
   fee-component row exists for break-bulk (`OMGPR-FEE-SHRT-CODE-BB = SPACES`, per the top-level
   control-flow map).
4. **Data/SQL dependencies:** No new SQL — reads switches already resolved upstream:
   `OMGPR-F-LUOM-ACCOUNT`/`OMGPR-F-LUOM-GROUP` (88-levels on `OMGPR-F-LUOM-SW`) and
   `OMGPR-F-BREAK-BULK-ACCOUNT`/`OMGPR-F-BREAK-BULK-GROUP` (88-levels on `OMGPR-F-BREAK-BULK-SW`).
   **Both switches are set in `CUP100`'s `A400-GET-JIT-ADJ`, from `CUR120-F-LUOM-ACT-GRP-FEE` and
   `CUR120-F-BRK-BULK-ACT-GRP-FEE` respectively (CONFIRMED, `program-inventory.md` §1.5) — i.e.
   ultimately sourced from the still-BLOCKED `CUP120`/`CUS120` chain, exactly like the LUOM
   eligibility switch already flagged twice in `jit-adjustment.md`. This is a third independent
   confirmed consumer of data whose actual business rule lives in the missing `CUS120` member.**
5. **Priority relative to competing rules:** For `'L'` (Low UOM): if `OMGPR-F-LUOM-ACCOUNT`, use
   `OMGPR-JIT-LUOM-CHRG-AMT` (an account-specific amount, itself from `CUR120` via `CUP100`);
   **else if** `OMGPR-F-LUOM-GROUP`, use `OMGPR-P-LOW-UOM` (a buy-group percentage, from `CUP100`'s
   `A800-GET-LOW-UOM-PCT`/`A830-SEL-LOW-UOM` buy-group-priority walk, per `program-inventory.md`
   §1.5); **else** (neither switch set) — `WS-VND-JIT-BB-FEE` is left unset (whatever it held from
   `7225`'s earlier field-setup, per `jit-adjustment.md` R-JIT-002 item 5's `WS-VND-JIT-BB-FEE`
   initialization from `OMGPR-JIT-BREAK-CHRG-AMT`). For `'B'` (Break Bulk): identical structure,
   `OMGPR-JIT-BREAK-CHRG-AMT` (account) vs. `OMGPR-P-BREAK-BULK` (group, from the same `CUP100`
   buy-group walk). **Account takes priority over group when both switches happen to be set** —
   CONFIRMED by the `IF ... ELSE IF ...` structure (not independently verified whether both switches
   being simultaneously `'Y'` is possible given `CUR120`'s own logic, which is BLOCKED).
6. **Calculation and rounding stage:** No computation here — a straight `MOVE` of whichever source
   field applies into `WS-VND-JIT-BB-FEE`, consumed by R-LUOM-004's formula next.
7. **Output fields:** `OMGPR-F-BREAK-BULK-OR-LUOM` (set to `'L'` or `'B'`, a *record* of which
   classification applied — distinct from, but redundant with, `WS-UOM-DESIGNATOR`),
   `WS-VND-JIT-BB-FEE`.
8. **Effective/expiration dates contributed:** None in this fragment.
9. **Exclusions and fallbacks:** The "neither switch set" case (item 5) is a silent no-op — same
   idiom flagged repeatedly in `jit-adjustment.md`.
10. **Errors:** None in this fragment.
11. **Confidence:** CONFIRMED for the branching structure. BLOCKED for whether
    `OMGPR-F-LUOM-ACCOUNT`/`-GROUP` can both be true simultaneously (would need `CUR120`'s/`CUS120`'s
    own logic, not supplied).

---

## Rule R-LUOM-004 — Legacy fee computation: cost-vs-sell basis from the JIT service-fee code

1. **Rule ID / name:** R-LUOM-004 — The legacy path's final fee amount is cost-based or sell-based
   using the *same* `OMGPR-JIT-SERVICE-FEE` test as the rest of `7225` (`jit-adjustment.md`'s
   `R-JIT-002`/`R-JIT-005`).
2. **Program / paragraph:** `A6U01`, `7896-CALC-LUOM-OR-BB-FEE`, lines 19390–19407.
3. **Preconditions:** `WS-VND-JIT-BB-FEE > 0` after R-LUOM-003's source selection (and after
   `7227-CHK-FOR-OVERRIDES` runs again here — the same override cascade `jit-adjustment.md` left
   out of scope, called a **second, independent time** for this specific fee, line 19390).
4. **Data/SQL dependencies:** None new.
5. **Priority relative to competing rules:** `IF (OMGPR-JIT-SERVICE-FEE = 'A' OR 'C') AND
   OMGPR-C-PRIVATE-LBL NOT = 'O'` → cost-based (`OMGPR-A-TOTAL-COST * WS-VND-JIT-BB-FEE`); `ELSE`
   → sell-based (`WS-A-SELL-BEFORE-ADJ * WS-VND-JIT-BB-FEE`) — **identical test and formula shape
   to every other JIT category in `R-JIT-002`/`R-JIT-005`.** This confirms the legacy LUOM/break-bulk
   fee is conceptually just a fifth JIT-fee category (alongside break-bulk-proper, label,
   apply-label, service-fee) that happens to live in a separate paragraph reached via `0245` rather
   than being inline in `7225`.
6. **Calculation and rounding stage:** `COMPUTE ... ROUNDED`, standard round-half-up, same as every
   other JIT fee formula.
7. **Output fields:** `OMGPR-P-ACTUAL-BB-OR-LUOM-PCT` (records whichever percent was actually used),
   `OMGPR-A-JIT-BREAK-FEE` — **the same output field `7225`'s (dead) inline break-bulk code would
   have written**, confirming this is a true relocation, not a new/different fee bucket.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** `WS-VND-JIT-BB-FEE = 0` (R-LUOM-003's "neither switch" case, or a
   zero account/group amount) → the whole `IF WS-VND-JIT-BB-FEE > ZEROS` block is skipped,
   `OMGPR-A-JIT-BREAK-FEE` stays at whatever it held before (typically zero).
10. **Errors:** None in this fragment (errors belong to the SQL-driven paragraphs upstream).
11. **Confidence:** CONFIRMED.

---

## Rule R-LUOM-005 — 2022 fee-component path: PANDAC suppression, flat-fee bypass, and a *different* cost-vs-sell test

1. **Rule ID / name:** R-LUOM-005 — When a 2022 fee-component row exists for break-bulk, three
   things differ from the legacy path: a PANDAC check can zero out the fee entirely, a flat fee
   type bypasses computation altogether, and — when a percent type does compute — the cost-vs-sell
   decision uses the fee-component's own type flag, not the JIT service-fee code.
2. **Program / paragraph:** `A6U01`, `7898-CALC-LUOM-OR-BB-FEE-NEW`, lines 19412–19513.
3. **Preconditions:** `OMGPR-FEE-SHRT-CODE-BB > SPACES` (a 2022 `CUTPRICE_COMPONENT` row exists for
   this account's break-bulk fee, per `program-inventory.md` §1.5), reached via `7880`'s gate.
4. **Data/SQL dependencies:** No new SQL in `7898` itself (all inputs already resolved by `CUP100`);
   calls `7707-CHECK-PANDAC-ACCT-FLAG` (not read in this pass — BLOCKED) and, like `7896`,
   `7227-CHK-FOR-OVERRIDES` (also out of scope).
5. **Priority relative to competing rules, in the order the code checks them:**
   1. **PANDAC suppression (lines 19414–19426, CONFIRMED):** `IF VNG01-F-VEND-PANDAC = 'Y' AND
      VNG02-F-PANDAC-ITEM = 'Y' AND WS-PANDAC-CUST` (a third condition, resolved by
      `7707-CHECK-PANDAC-ACCT-FLAG`, not itself traced) → **every 2022 break-bulk/LUOM
      fee-component field is blanked/zeroed** (`OMGPR-FEE-SHRT-CODE-BB`, `-TYPE-BB`, `-SKU-CODE-BB`,
      `-BILLING-FRQ-BB`, `OMGPR-P-BREAKBULK-BB`, `OMGPR-A-BREAKBULK-BB`) and the paragraph exits
      immediately — **no break-bulk/LUOM charge of any kind for a PANDAC-eligible vendor+item+
      customer combination, full stop.** This is a new, previously undocumented mutual-exclusivity
      finding: PANDAC pricing and break-bulk/LUOM fees do not coexist on the same line, at least via
      the 2022 fee-component path. (`jit-adjustment.md` documented PANDAC as a sell-arrangement-side
      concept via `0165`/`0170`/`0172`; this is the first evidence PANDAC also reaches into the
      cost-side JIT/LUOM machinery.)
   2. **Flat-fee bypass (lines 19436–19447, CONFIRMED, same pattern as `jit-adjustment.md`'s
      `R-JIT-004`):** `IF OMGPR-FEE-PER-LINE-BB OR -PER-ORDER-BB OR -PER-QTY-BB` → for `'L'`,
      `OMGPR-A-JIT-BREAK-FEE ← OMGPR-A-LUM-FEE-AMT-BB` directly; for `'B'`,
      `← OMGPR-A-BREAKBULK-BB` directly — **no percent calculation, no `7227` override check for
      this fee** (the paragraph exits via `GO TO 7898-EXIT` immediately after, line 19446, skipping
      the `7227` call at line 19477 entirely — CONFIRMED by control flow, not merely inferred from
      the pattern seen elsewhere).
   3. **Percent-based (lines 19449–19493, reached only if neither of the above applied):** source
      selection is `'L'` → `OMGPR-P-LUM-FEE-PCT-BB` if `OMGPR-FEE-PCT-SELL-BB OR -PCT-COST-BB`, else
      zero; `'B'` → `OMGPR-P-BREAKBULK-BB` under the same type test, else zero — **no
      account-vs-group split here** (unlike R-LUOM-003's legacy path) — the 2022 system has exactly
      one percentage source per designator, not two. `7227-CHK-FOR-OVERRIDES` then runs (line
      19477–19478), and finally: `IF OMGPR-FEE-PCT-COST-BB AND OMGPR-C-PRIVATE-LBL NOT = 'O'` →
      cost-based; `ELSE` → sell-based. **This cost-vs-sell test uses `OMGPR-FEE-PCT-COST-BB` (the
      2022 fee-component row's own type flag), not `OMGPR-JIT-SERVICE-FEE` (the account's JIT
      billing-type code used by every other cost-vs-sell decision in this rule area and in
      `jit-adjustment.md`).** This is a genuine, confirmed divergence between the legacy and 2022
      systems: it is possible for an account's JIT service-fee type to say "bill at cost" (`A`/`C`)
      while its 2022 break-bulk fee-component row is configured as percent-of-sell, in which case
      the LUOM/break-bulk portion would compute from sell price even though everything else on the
      line computes from cost — or vice versa. **Flagged as the single most important behavioral
      subtlety in this document** for anyone validating a migrated implementation, since it is easy
      to assume (incorrectly) that "cost-based JIT" is a per-line, all-or-nothing property.
6. **Calculation and rounding stage:** Same `COMPUTE ... ROUNDED` shape as R-LUOM-004 for the
   percent-based sub-case; direct `MOVE` (no rounding needed, already-rounded source) for the
   flat-fee bypass.
7. **Output fields:** Same as R-LUOM-004 (`OMGPR-P-ACTUAL-BB-OR-LUOM-PCT`,
   `OMGPR-A-JIT-BREAK-FEE`), plus the PANDAC-suppression case's cleared fee-component fields.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** The `OMGPR-FEE-PCT-SELL-BB OR -PCT-COST-BB` false case (neither
   percent type, and not a flat type either — a data state this task cannot name a concrete
   business scenario for) forces `WS-VND-JIT-BB-FEE = 0`, same silent-zero idiom as elsewhere.
10. **Errors:** None raised directly in `7898`.
11. **Confidence:** CONFIRMED for the three-tier priority (PANDAC → flat → percent) and the
    cost-vs-sell divergence from the legacy path. BLOCKED for `7707-CHECK-PANDAC-ACCT-FLAG`'s and
    `WS-PANDAC-CUST`'s own derivation.

---

## What was scoped out of this pass

- **`7707-CHECK-PANDAC-ACCT-FLAG`** — referenced by R-LUOM-005's PANDAC suppression, not read.
- **`7227-CHK-FOR-OVERRIDES`** — same standing deferral as `jit-adjustment.md`; now confirmed to be
  called from **two additional sites** in this chain (`7896` and, conditionally, `7898`), on top of
  the four sites already noted in T006.
- **`VNG05`/`BGG25`/`VNG20`'s full DCLGEN** — SQL-text-level column confirmation only, per this
  document's header.

## Assumptions

1. `WS-MAX-ALTER-UOM-ENTRIES`'s actual configured limit was not read; only that exceeding it is a
   fatal error.
2. `WS-PANDAC-CUST` is assumed (not confirmed) to be a per-customer PANDAC-eligibility flag,
   analogous to the vendor/item PANDAC flags it's tested alongside — its own paragraph
   (`7707-CHECK-PANDAC-ACCT-FLAG`) was not read.

## Blockers

1. `7707-CHECK-PANDAC-ACCT-FLAG` — needed to know exactly which customers trigger the PANDAC
   break-bulk/LUOM suppression (R-LUOM-005 item 5.1).
2. `CUR120`/`CUS120`'s derivation of `OMGPR-F-LUOM-ACCOUNT`/`-GROUP` and
   `OMGPR-F-BREAK-BULK-ACCOUNT`/`-GROUP` (R-LUOM-003) — third independent confirmed consumer of
   this still-missing library member's logic.
3. `VNG02-T-VND-PROD-UM-DESC`'s actual data domain (R-LUOM-002 item 11).
4. No DB2/CICS/COBOL execution environment — standing blocker across this entire document set.

## Correction applied to T006

`docs/cobol-analysis/decision-tables/jit-adjustment.md`'s `R-JIT-003` has been updated with a
note reading: *"CORRECTION (see `low-uom-break-bulk.md`, T008): this rule's conclusion that break-
bulk JIT fee computation is dead code is incomplete. The computation was relocated, not deleted —
it runs via `0245-PRO-BG-LOW-UOM-010`'s call chain whenever this rule's own precondition
(`OMGPR-A-JIT-BREAK-FEE = 0`) holds, which per the original inline code being commented out, is
now unconditionally true. In other words: this rule's fallback (`0245`) is not a fallback for a
rarely-zero case — it is now the **only** live path to a break-bulk/LUOM charge. See
`low-uom-break-bulk.md` for the actual computation (rules R-LUOM-001 through R-LUOM-005)."*

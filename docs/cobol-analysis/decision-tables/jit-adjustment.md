# A6U01 — JIT (Just-In-Time) Fee Adjustment (T006)

**Scope:** Decision-table extraction for JIT fee computation — `7225-PRO-JIT-ADJ-010` (the core
module) plus two directly-adjacent, PERFORM-linked paragraphs: `7226-CHK-FRT-FEE-EXEMPT` (freight
exemption, called from the cost-adjustment paragraph `7200` right after JIT concerns) and
`7227-CHK-FOR-OVERRIDES` (vendor JIT-fee override lookup, called from inside `7225` itself).
**Not in scope:** the override-lookup internals (`7325`/`7328`/`7330`/`7332`/`7335`/`7337`, six
paragraphs behind `7227`), and the upstream `CUP100`→`CUP120`→`CUS120` chain that supplies the raw
`OMGPR-JIT-*` input fields this rule area consumes (already fully documented as BLOCKED in
`program-inventory.md` §1.5/§1.9 — this document does not re-litigate that, only confirms *where*
those fields get used once they arrive).
**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`, lines 13020–13497. No DCLGEN
copybook for `CUG55/56/57/73/74/75` (the override-exemption-type tables referenced but whose
lookup paragraphs are out of scope) is supplied in `upload/`.
**Confidence key:** same as prior decision-table documents.

> **Correction (added after T008):** `R-JIT-003` below originally concluded that JIT break-bulk fee
> computation was dead code. That conclusion is incomplete — see the correction note inserted at
> the top of `R-JIT-003` and `docs/cobol-analysis/decision-tables/low-uom-break-bulk.md` (T008) for
> the full picture: the computation was relocated, not deleted, and `low-uom-break-bulk.md`'s rules
> R-LUOM-001 through R-LUOM-005 are now the authoritative source for how this fee is actually
> computed.

---

## Rule R-JIT-001 — Service-fee type gates whether JIT is computed at all, and from which base

1. **Rule ID / name:** R-JIT-001 — `OMGPR-JIT-SERVICE-FEE` (`A`/`C`/`R`/`P`) is the master switch:
   any other value skips this entire rule area, and the four valid values split into two pairs
   (cost-based vs. sell-based) that also differ in whether the fee is embedded in the line price.
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, lines 13020–13341 (gate at
   13044–13047, cost-vs-sell split at 13320–13334).
3. **Preconditions:** `7225` is called from `7195-PRO-ADJ-SELL-010` (already documented in
   `contract-cost-method.md`'s control-flow map, though not itself extracted there) only when
   `OMGPR-F-JIT-EXEMPT NOT = 'Y'` and `OMGPR-F-ST-JIT-CUSTOMER = 'Y'` and the service-fee code is
   one of `R`/`P`/`C`/`A` — i.e., the *caller* re-checks the same four-value set before even
   `PERFORM`ing `7225`.
4. **Data/SQL dependencies:** No SQL in `7225` itself — every input (`OMGPR-JIT-SERVICE-FEE`,
   `OMGPR-JIT-BREAK-CHRG-AMT`, `OMGPR-JIT-LABEL-CHRG-AMT`, `OMGPR-JIT-APPLY-CHRG-AMT`,
   `OMGPR-JIT-SERVICE-FEE-PCT`, `OMGPR-JIT-LUM-FEE-PCT`, `OMGPR-JIT-EXTRA-DELIV-FEE-PCT`,
   `OMGPR-JIT-NON-OM-SLCT-FEE-PCT`) was already populated upstream, ultimately from `CUR120` via
   `CUP100`'s `A400-GET-JIT-ADJ` (per `program-inventory.md` §1.5), which in turn calls the
   still-BLOCKED `CUP120`/`CUS120`.
5. **Priority relative to competing rules:** The four codes' documented meanings (CONFIRMED,
   header comment lines 13024–13043, i.e. this is CONFIRMED-from-comment corroborated by the
   code's actual branching at lines 13320–13340):

   | Code | Meaning | Calc base | Embedded in line item? |
   |---|---|---|---|
   | `A` | Bill at adjusted cost | Cost | No (order-total only) |
   | `C` | Add to cost | Cost | Yes |
   | `R` | Bill at price | Sell | No (order-total only) |
   | `P` | Add to price | Sell | Yes |

   Any other value (including spaces) means `7225`'s entire body is skipped — CONFIRMED, the whole
   paragraph is one `IF OMGPR-JIT-SERVICE-FEE = 'A' OR 'C' OR 'R' OR 'P' ... END-IF` (lines
   13044–13341), no `ELSE`, no error. **Every one of R-JIT-001 through R-JIT-006 below is
   conditional on this gate.**
6. **Calculation and rounding stage:** N/A at this level — see R-JIT-002 for the per-category
   formulas.
7. **Output fields:** N/A directly — see R-JIT-002/R-JIT-006.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** An unrecognized/blank service-fee code is a silent no-op, the same
   "no diagnostic" pattern already flagged for cost-entry-method code `01`/`06`/`07`/unrecognized in
   `contract-cost-method.md`'s R-COST-005 item 9 and SCN-COST-020 — worth noting this is now the
   **third** place in `A6U01` (contract cost-entry method, sell-price method's implicit "no method
   matched" default, and now JIT service-fee type) where an unexpected code value is treated as
   "do nothing," not "raise a diagnostic." This is a recurring design idiom in this codebase, not
   an isolated oversight — useful context for deciding how a migrated system should treat
   unexpected enum values across the board, not just for JIT.
10. **Errors:** None raised by `7225` itself.
11. **Confidence:** CONFIRMED.

---

## Rule R-JIT-002 — Five fee categories, each with its own item-level gate and percent/rate/flat sub-dispatch

1. **Rule ID / name:** R-JIT-002 — Within the `A`/`C`/`R`/`P` gate, up to four fee amounts
   (break-bulk, print-label, apply-label, service-fee-family) are computed independently, then
   summed.
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, four labeled sections: "CALC JIT
   BREAK BULK FEE #1" (13084–13131, largely dead code, see R-JIT-003), "CALC JIT PRINT LABEL FEE
   #2" (13132–13192), "CALC JIT APPLY LABEL FEE #3" (13193–13262), "CALC JIT SERVICE FEE #4"
   (13263–13317), summed at "CALCULATE TOTAL JIT FEE FOR COST/SELL #5" (13318–13334).
3. **Preconditions:** Each category has its own item-level 88-level gate on the *order line
   itself* (not the account): `OMGPR-JIT-ITEM-BREAK-BULK` (#1), `OMGPR-JIT-ITEM-LABEL` (#2),
   `OMGPR-JIT-ITEM-APPLY-LAB` (#3) — i.e., these three only apply if the specific line item was
   flagged (presumably by the calling order-entry system) as needing that service. Service fee (#4)
   has **no item-level gate** — it always attempts to compute if the vendor has a nonzero
   `WS-VND-JIT-SF-FEE` or `WS-VND-JIT-PF-FEE`.
4. **Data/SQL dependencies:** No new SQL; consumes the `WS-VND-JIT-*-FEE` working fields set at the
   top of `7225` from the `OMGPR-JIT-*` inputs (see R-JIT-004 for label/break-bulk's 2022-system
   interaction).
5. **Priority relative to competing rules:** Each category's calc is itself sub-dispatched by a
   percent-vs-rate-vs-flat 88-level check on the *category's own charge-type field*
   (`OMGPR-PCT-PER-BREAK`/`OMGPR-RATE-PER-BREAK` for #1; `OMGPR-PCT-PER-LABEL`/`OMGPR-RATE-PER-
   LABEL`/`OMGPR-LABEL-RATE-PER-ITEM` for #2; `OMGPR-PCT-PER-APPLY`/`OMGPR-RATE-PER-APPLY`/
   `OMGPR-APPLY-RATE-PER-ITEM` for #3) — CONFIRMED these are independent `IF`s, not an `EVALUATE`,
   so in principle more than one could fire per category if the underlying 88-levels weren't
   mutually exclusive by value (they are, since they all test the same one-character field with
   distinct literal values, per `OMGPR.CPY`'s `OMGPR-JIT-LABEL-CHRG-TYPE`/`-APPLY-CHRG-TYPE`/
   `-BREAK-CHRG-TYPE` 88-levels already catalogued in `program-inventory.md` §2).
   - **Percent-based** (#2/#3, mirrored): `IF (OMGPR-JIT-SERVICE-FEE = 'A' OR 'C') AND
     OMGPR-C-PRIVATE-LBL NOT = 'O' → fee = OMGPR-A-TOTAL-COST * vendor-pct; ELSE → fee =
     WS-A-SELL-BEFORE-ADJ * vendor-pct`. **This is the same cost-vs-sell split as R-JIT-001 item 5,
     re-implemented per-category rather than inherited** — CONFIRMED, each category independently
     re-tests `OMGPR-JIT-SERVICE-FEE = 'A' OR 'C'`, it is not computed once and reused. The extra
     `AND OMGPR-C-PRIVATE-LBL NOT = 'O'` clause is a further carve-out — see R-JIT-005.
   - **Rate-based** (#2/#3, mirrored, `EVALUATE TRUE` at lines 13158/13225): a 3-way UOM-dependent
     formula — `WHEN (label-UM found AND alt-order-UM found AND they're equal to the order UOM)` →
     `(fee-rate * OMGPR-ALT-ORD-CONV-FACTOR * item-qty) / OMGPR-CUST-LABEL-CONV-FACTOR`;
     `WHEN (label-UM found alone)` → `(fee-rate * item-qty) / OMGPR-CUST-LABEL-CONV-FACTOR`;
     `WHEN OTHER` → `fee-rate * item-qty` (no conversion at all). CONFIRMED identical structure for
     label (#2) and apply-label (#3), differing only in which `OMGPR-A-JIT-*-FEE` output field is
     written.
   - **Per-item flat rate** (`OMGPR-LABEL-RATE-PER-ITEM`/`OMGPR-APPLY-RATE-PER-ITEM`): direct
     `MOVE` of the vendor's flat fee, no quantity multiplication, no conversion — CONFIRMED, lines
     13187–13190/13256–13259.
   - **Break-bulk (#1)** has the same percent/rate structure *documented in comments* but the
     actual computation code is **entirely commented out** (`SJ0322*`/`NK0311*`, lines 13089–13117)
     — see R-JIT-003 for what replaced it.
   - **Service fee (#4)** is simpler: no percent/rate/flat split — it is always a straight
     `base * vendor-pct` for up to four sub-amounts simultaneously (service, LUM, extra-delivery,
     non-OM-select), gated only by the same cost-vs-sell test (lines 13274–13316). The non-OM-select
     sub-amount has an *additional* gate, `WS-NON-OM-SELECT-ITEM`, and is **added into**
     `OMGPR-A-JIT-SERVICE-FEE` rather than kept as a separate total (CONFIRMED, lines 13293–13294,
     13313–13314 — `OMGPR-A-JIT-NON-OM-SLCT-FEE` is computed and *also* folded into the service fee
     total in the same breath).
6. **Calculation and rounding stage:** All four categories' `COMPUTE`s use `ROUNDED`
   (round-half-up), confirmed at every site cited above.
7. **Output fields:** `OMGPR-A-JIT-BREAK-FEE`, `OMGPR-A-JIT-LABEL-FEE`, `OMGPR-A-JIT-APPLY-FEE`,
   `OMGPR-A-JIT-SERVICE-FEE`, `OMGPR-A-JIT-LUM-FEE`, `OMGPR-A-JIT-EXTRA-DELIV-FEE`,
   `OMGPR-A-JIT-NON-OM-SLCT-FEE` (folded into `OMGPR-A-JIT-SERVICE-FEE`, per item 5).
8. **Effective/expiration dates contributed:** None in this paragraph.
9. **Exclusions and fallbacks:** Each category's item-level gate (item 3) is itself the
   exclusion mechanism — an item not flagged for a given service simply never computes that fee
   (stays at whatever it was initialized to, presumably zero, not traced to its `INITIALIZE` site
   in this pass).
10. **Errors:** None raised in this rule.
11. **Confidence:** CONFIRMED for the structure and formulas as read. The `7227-CHK-FOR-OVERRIDES`
    call preceding each of categories #1(historically)/#2/#3/#4 (lines 13096, 13140, 13207, 13270)
    can alter `WS-VND-JIT-*-FEE` before these formulas run — its own internal logic is BLOCKED
    (out of scope, see document header), so **the exact vendor-fee value these formulas consume may
    have been overridden by an account/product-category/vendor-level exception this document does
    not trace.**

---

## Rule R-JIT-003 — Break-bulk JIT fee is unconditionally redirected to the buy-group low-UOM/break-bulk paragraph chain

> **CORRECTION (added after T008 — `low-uom-break-bulk.md`):** this rule was originally titled
> "Break-bulk JIT fee is dead code" and concluded the fee was effectively retired. That is
> incomplete: the computation was **relocated, not deleted**. It runs inside
> `0245-PRO-BG-LOW-UOM-010`'s call chain (`7880-FIND-LUOM-BB-CHG` → `7896-CALC-LUOM-OR-BB-FEE` /
> `7898-CALC-LUOM-OR-BB-FEE-NEW`), fully extracted in `docs/cobol-analysis/decision-tables/
> low-uom-break-bulk.md` as rules `R-LUOM-001`–`005`. What remains accurate below: this rule's
> *precondition* (item 5) is still correct as written — because the inline break-bulk formula in
> `7225` is commented out, `OMGPR-A-JIT-BREAK-FEE` really is always `0` at this check, so the
   `0245` branch really does fire unconditionally whenever `LUOM-ELIG`/`ORDER-TYPE-STOCK` hold.
> What was wrong is characterizing that branch as a "LUOM charge" fallback distinct from break-bulk
> — `0245`'s chain computes **both** `'L'` (low-UOM) and `'B'` (break-bulk) charges depending on a
> UOM-designator classification, writing the result to this same `OMGPR-A-JIT-BREAK-FEE` field.
> Items 6–8 below (previously "not traced") are now answered in `low-uom-break-bulk.md`.

1. **Rule ID / name:** R-JIT-003 — When JIT break-bulk fee computes to zero (which, per R-JIT-002,
   is *always* true today since the computation is commented out), the pricer unconditionally
   redirects to `0245-PRO-BG-LOW-UOM-010`'s buy-group low-UOM/break-bulk paragraph chain instead,
   gated by an eligibility flag whose origin is the already-BLOCKED `CUS120` chain.
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, lines 13118–13131 (the redirect);
   see `low-uom-break-bulk.md` for what `0245` itself does.
3. **Preconditions (CONFIRMED, lines 13123–13128):** `OMGPR-A-JIT-BREAK-FEE = 0` **and**
   `OMGPR-F-LUOM-ELIG-SW = 'Y'` **and** `OMGPR-ORDER-TYPE-STOCK` (order type is `'S'` or space, per
   `OMGPR.CPY`'s `OMGPR-ORDER-TYPE-STOCK` 88-level).
4. **Data/SQL dependencies:** `0245-PRO-BG-LOW-UOM-010` — fully extracted in
   `low-uom-break-bulk.md` (T008), no longer out of scope.
5. **Priority relative to competing rules:** Because the break-bulk percent/rate computation code
   is entirely commented out (R-JIT-002), `OMGPR-A-JIT-BREAK-FEE` is **always** `0` when this check
   runs (assuming it was initialized to zero and nothing else in the paragraphs read sets it) —
   meaning **this redirect is not really conditional in practice; it fires whenever the other two
   preconditions (`LUOM-ELIG` and `ORDER-TYPE-STOCK`) hold, every time.** In other words: `0245`'s
   chain (not the inline formula in `7225`) is the **only live path** to a break-bulk/LUOM JIT
   charge in this codebase, for both the `'L'` and `'B'` classifications alike — see
   `low-uom-break-bulk.md` for confirmation that `'B'` (break-bulk proper) is very much still
   computed there, just not inline in `7225`.
6. **Calculation and rounding stage:** See `low-uom-break-bulk.md` rules R-LUOM-003–005.
7. **Output fields:** See `low-uom-break-bulk.md` — same `OMGPR-A-JIT-BREAK-FEE` field this rule's
   (dead) inline formula would have written.
8. **Effective/expiration dates contributed:** None (confirmed in `low-uom-break-bulk.md`).
9. **Exclusions and fallbacks:** `OMGPR-F-LUOM-ELIG-SW` is the exclusion gate — its own header
   comment (CONFIRMED, `OMGPR.CPY`, cited already in `program-inventory.md` and reproduced by the
   in-source comment at lines 13124–13126) states: **"LUOM CHARGES WILL BE COMPUTED ONLY IF ACCOUNT
   IS ELIGIBLE FOR THIS CHARGE. THIS SWITCH IS SET IN CUS120 AND PASSED THROUGH CUP100/CUP110."**
   This is a direct, textual confirmation that `OMGPR-F-LUOM-ELIG-SW`'s value originates in the same
   still-BLOCKED `CUS120` library member already flagged in `program-inventory.md` §1.9 — **this
   task cannot state the actual eligibility rule (which accounts qualify), only that `A6U01` trusts
   whatever `CUS120` decided.**
10. **Errors:** None in this fragment; `0245`'s own error handling is covered in
    `low-uom-break-bulk.md`.
11. **Confidence:** CONFIRMED for the control-flow finding (break-bulk fee is effectively always
    zero, so this redirect is effectively unconditional given its other two gates), and for
    everything inside `0245` now that `low-uom-break-bulk.md` (T008) has extracted it. Still
    BLOCKED for `CUS120`'s actual eligibility rule.

---

## Rule R-JIT-004 — 2022 fee-component system silently supersedes legacy percent-based break-bulk/apply-label JIT fees

1. **Rule ID / name:** R-JIT-004 — If `CUP100`'s 2022-added fee-component lookup (`CUTPRICE_COMPONENT`,
   per `program-inventory.md` §1.5's `3000-GET-PRICE-COMPONENT-DATA`) found a break-bulk or
   apply-label fee code for this account, the *legacy* JIT percent-based amount is suppressed
   (forced to zero for break-bulk) or bypassed entirely (apply-label reads the new system's
   pre-computed amount directly, skipping the override-check/percent/rate cascade altogether).
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, lines 13049–13060 (break-bulk source
   selection) and 13194–13261 (apply-label, note the `SJ1022`-tagged early branch at 13195–13200).
3. **Preconditions:**
   - **Break-bulk:** `IF OMGPR-FEE-SHRT-CODE-BB > SPACES` (a 2022 fee-component row exists for
     break-bulk) `AND (OMGPR-FEE-PCT-SELL-BB OR OMGPR-FEE-PCT-COST-BB)` (its fee *type* is
     percent-of-sell or percent-of-cost) → use `OMGPR-JIT-BREAK-CHRG-AMT` as before (no actual
     change in this specific sub-case); **`ELSE` (fee-component row exists but its type is
     something else — e.g. flat-per-line/per-qty/per-order) → `WS-VND-JIT-BB-FEE` is forced to
     `ZERO`**, suppressing the legacy JIT break-bulk percent entirely (CONFIRMED, lines
     13050–13056). If no fee-component row exists at all (`OMGPR-FEE-SHRT-CODE-BB = SPACES`), the
     legacy `OMGPR-JIT-BREAK-CHRG-AMT` is used unmodified (line 13057–13059) — i.e., the *absence*
     of 2022 data preserves old behavior, and the *presence* of non-percent 2022 data actively
     zeroes out the legacy calculation (which, per R-JIT-003, was already dead code in practice —
     so this specific interaction is currently moot for break-bulk, but the code exists and would
     matter immediately if break-bulk's percent/rate computation were ever un-commented).
   - **Apply-label:** `IF OMGPR-FEE-SHRT-CODE-AL > SPACES AND (per-line OR per-order OR per-qty fee
     type)` → `OMGPR-A-JIT-APPLY-FEE` is set **directly** from `OMGPR-A-APPLY-LAB-AL` (the 2022
     fee-component's own pre-computed amount, populated by `CUP100`'s `3700-GET-CUS-DATA` per
     `program-inventory.md` §1.5) — **the entire `7227-CHK-FOR-OVERRIDES`/percent/rate cascade for
     apply-label is skipped entirely** in this case (CONFIRMED, lines 13195–13200 execute instead
     of, not in addition to, the `7201`-onward block). Only when this specific condition is false
     does the legacy override-check-then-percent/rate-or-flat cascade (lines 13201–13260) run.
4. **Data/SQL dependencies:** `OMGPR-FEE-SHRT-CODE-BB`/`OMGPR-FEE-PCT-SELL-BB`/`-COST-BB`,
   `OMGPR-A-BREAKBULK-BB`, `OMGPR-FEE-SHRT-CODE-AL`/`OMGPR-FEE-PER-LINE-AL`/`-ORDER-AL`/`-QTY-AL`,
   `OMGPR-A-APPLY-LAB-AL` — all populated by `CUP100`'s 2022 fee-component paragraphs (already
   CONFIRMED in `program-inventory.md` §1.5, not re-derived here).
5. **Priority relative to competing rules:** The 2022 fee-component system's presence and *type*
   (percent vs. flat) directly gates whether the legacy JIT percent/rate machinery in R-JIT-002 even
   runs for these two categories — this is a genuine precedence rule between two independently-built
   subsystems layered 20+ years apart, not documented anywhere except in this branching logic
   itself.
6. **Calculation and rounding stage:** No new computation for apply-label's fast path (direct
   `MOVE`, already-rounded by whatever populated `OMGPR-A-APPLY-LAB-AL`); break-bulk's suppression
   just zeroes a working field consumed by R-JIT-002's (currently dead) formula.
7. **Output fields:** `WS-VND-JIT-BB-FEE` (break-bulk), `OMGPR-A-JIT-APPLY-FEE` (apply-label, fast
   path only).
8. **Effective/expiration dates contributed:** None in this fragment.
9. **Exclusions and fallbacks:** Absence of a 2022 fee-component row is itself the fallback to
   legacy behavior, per item 3.
10. **Errors:** None.
11. **Confidence:** CONFIRMED for break-bulk and apply-label as read. Print-label (#2) and service
    fee (#4) show **no equivalent 2022-fee-component fast path** in the paragraph read — CONFIRMED
    by absence; only break-bulk and apply-label have this interaction, not the other three JIT
    categories, despite `program-inventory.md` §1.5 documenting 2022 fee-component data being
    fetched for a broader set of categories (PANDAC, SurgiTrak, standard freight, terms/surcharge,
    markup groups) — those other categories are consumed elsewhere (per `R-SELL-003`'s markup-bucket
    discussion and `program-inventory.md`'s PANDAC references), not inside `7225`.

---

## Rule R-JIT-005 — Private-label `'O'` products are always billed JIT on sell, never on cost

1. **Rule ID / name:** R-JIT-005 — Regardless of the account's `A`/`C` (cost-based) service-fee
   type, a private-label-`'O'` product's JIT fees are computed from sell price, not cost.
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, the recurring clause `AND
   OMGPR-C-PRIVATE-LBL NOT = 'O'` guarding every cost-vs-sell split — lines 13146, 13213, 13276,
   13322 (four occurrences: label, apply-label, service-fee, and the final cost/sell total split).
3. **Preconditions:** `OMGPR-JIT-SERVICE-FEE = 'A'` or `'C'` (the two cost-based codes) **and**
   `OMGPR-C-PRIVATE-LBL = 'O'`.
4. **Data/SQL dependencies:** `OMGPR-C-PRIVATE-LBL` (populated upstream, not traced in this task —
   present in `OMGPR.CPY`'s product-data group per `program-inventory.md` §2).
5. **Priority relative to competing rules:** This clause overrides R-JIT-001/002's stated cost-basis
   for codes `A`/`C` specifically for private-label-`'O'` products — CONFIRMED to apply
   **consistently across all four sites** (every cost-vs-sell branch point in the paragraph carries
   the identical additional test), so this is a genuine, deliberate carve-out, not an isolated
   one-off. No comment explains *why* `'O'`-sourced private-label products are exempted from
   cost-based JIT billing; flagged as a business-rule question, not something inferable from the
   code alone.
6. **Calculation and rounding stage:** No new formula — this clause only changes *which* formula
   (cost-based vs. sell-based) applies, per R-JIT-002 item 5 and R-JIT-006.
7. **Output fields:** Same fields as R-JIT-002/R-JIT-006, just routed through the sell-based
   formula instead of the cost-based one.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** N/A — this rule *is* the exclusion.
10. **Errors:** None.
11. **Confidence:** CONFIRMED for the mechanism (four consistent occurrences). INFERRED that `'O'`
    means "Owens-sourced" (a natural reading given this codebase's Owens & Minor origin, consistent
    with `A6O016U`'s `'O'` = "Owens kit" / `'S'` = "supplier kit" product-type convention documented
    in `program-inventory.md` §1.6) — not confirmed from any comment in this specific paragraph.

---

## Rule R-JIT-006 — Total JIT fee routes to cost or sell bucket, and can suppress its own line-item visibility

1. **Rule ID / name:** R-JIT-006 — The four category fees sum into either `OMGPR-A-JIT-COST` or
   `OMGPR-A-JIT-SELL` (never both), and a separate flag records whether this line's displayed price
   should exclude the JIT amount.
2. **Program / paragraph:** `A6U01`, `7225-PRO-JIT-ADJ-010`, lines 13318–13340.
3. **Preconditions:** Runs unconditionally once the four category calcs (R-JIT-002) complete,
   still within the outer `A`/`C`/`R`/`P` gate (R-JIT-001).
4. **Data/SQL dependencies:** None new — pure arithmetic on already-computed `OMGPR-A-JIT-*-FEE`
   fields.
5. **Priority relative to competing rules:** Same `(A or C) AND PRIVATE-LBL NOT = 'O'` test as
   R-JIT-002/005, applied once more at the total level (line 13320–13322) — `OMGPR-A-JIT-COST` gets
   the sum in that case, `OMGPR-A-JIT-SELL` otherwise. **This is the field `R-COST-005`/`R-SELL-003`
   and `7195`/`7200` (already referenced but not extracted) actually consume** — e.g.
   `contract-cost-method.md`'s control-flow map shows `7200-PRO-ADJ-COST-010` folding
   `OMGPR-A-JIT-COST`-adjacent totals, and this document's own `R-SELL-003` scope boundary noted
   `7195` reading `OMGPR-A-JIT-SELL`/`OMGPR-A-JIT-COST` in its total-adjustment-to-sell formula
   (`sell-arrangement-resolution.md`'s "not in scope" note references `7195` without extracting it —
   this rule is the producer of the values that paragraph consumes).
6. **Calculation and rounding stage:** `COMPUTE ... ROUNDED = service + label + apply + break`, one
   sum, no intermediate rounding beyond what each category already applied.
7. **Output fields:** `OMGPR-A-JIT-COST` (mutually exclusive with the next field), `OMGPR-A-JIT-SELL`,
   `OMGPR-F-JIT-REMOVED`.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** `OMGPR-F-JIT-REMOVED` is set to `'Y'` (CONFIRMED, lines
   13336–13340) when `(OMGPR-JIT-SERVICE-FEE = 'A' AND OMGPR-A-JIT-COST > 0) OR
   OMGPR-JIT-SERVICE-FEE = 'R'` — i.e., precisely the two "don't add to line item, order-total
   only" codes from R-JIT-001's table, **and only for `'A'` when the cost-based total actually came
   out positive** (an `'A'`-type line with zero JIT cost does not get flagged as removed — there is
   nothing to remove). `'R'` is flagged unconditionally, with no equivalent `> 0` guard — a minor
   asymmetry between the two "not embedded" codes worth noting, though not necessarily consequential
   since a zero sell-based JIT total flagged as "removed" has no visible effect either way.
10. **Errors:** None.
11. **Confidence:** CONFIRMED.

---

## Rule R-JIT-007 (adjacent, related) — Freight-fee exemption cascade

1. **Rule ID / name:** R-JIT-007 — A five-level custom→group-sanctioned→division/non-sanctioned→
   individual→default cascade decides whether this line is freight-exempt, then remaps the
   freight-type code accordingly.
2. **Program / paragraph:** `A6U01`, `7226-CHK-FRT-FEE-EXEMPT`, lines 13348–13441. Called from
   `7200-PRO-ADJ-COST-010` (the cost-adjustment paragraph, `contract-cost-method.md`'s control-flow
   map), positioned immediately after the freight-cost paragraph and before overhead — i.e., this
   is a **cost-side** rule, not itself part of JIT, but colocated in the source right after `7225`
   and structurally similar enough (a priority cascade gated by custom/group/division/individual
   flags) that it is documented here rather than left completely uncharacterized.
3. **Preconditions:** Called unconditionally from `7200` once per line.
4. **Data/SQL dependencies:** None new — reads `VNG02-C-CUSTOM-IND`, `OMGPR-F-EXEMPT-CUSTOM-FLAG`/
   `-SANC-FLAG`/`-NON-SANC-FLAG`/`-IND-FLAG`/`-NON-CONT-FLAG` (all populated upstream by `CUP100`'s
   `A425-PRO-CUG53`/`A450-READ-CID-FREIGHT-FLAG`, already CONFIRMED in `program-inventory.md` §1.5),
   `CCG09-I-RPT-GRP`, `OMGPR-F-GRP-CNT-FEES`, `BGG01-I-DIVISION`, `WS-INDIV-CNT`,
   `WS-COST-CONT-FND`.
5. **Priority relative to competing rules:** Sequential, first-match-wins (CONFIRMED, nested
   `IF`/`ELSE`, not a loop or `EVALUATE`, lines 13352–13384):
   1. **Custom account** (`VNG02-C-CUSTOM-IND = 'Y'`): exempt iff `OMGPR-F-EXEMPT-CUSTOM-FLAG = 'N'`.
   2. **Else, if a cost contract was found** (`WS-COST-CONT-FND`):
      a. Group-sanctioned (`CCG09-I-RPT-GRP > 0` or `OMGPR-F-GRP-CNT-FEES = 'Y'`): exempt iff
         `OMGPR-F-EXEMPT-SANC-FLAG = 'N'`.
      b. Else, division-01 non-sanctioned (`BGG01-I-DIVISION = '01' AND OMGPR-F-GRP-CNT-FEES =
         'N'`): exempt iff `OMGPR-F-EXEMPT-NON-SANC-FLAG = 'N'`.
      c. Else, individual contract or non-division-01 (`WS-INDIV-CNT OR BGG01-I-DIVISION NOT =
         '01'`): exempt iff `OMGPR-F-EXEMPT-IND-FLAG = 'N'`.
      d. Else: not exempt (falls to `CONTINUE`).
   3. **Else** (no cost contract at all): exempt iff `OMGPR-F-EXEMPT-NON-CONT-FLAG = 'N'`.
   **Note the inverted sense**: each exemption flag is checked for `= 'N'`, not `= 'Y'` — i.e., the
   flags as stored mean "NOT exempt" when `'Y'` and the code exempts when they read `'N'`. This
   inversion is easy to get backwards in a reimplementation; flagged explicitly.
6. **Calculation and rounding stage:** N/A — this rule only sets a boolean and remaps a type code.
7. **Output fields:** `WS-EXEMPT-FRT-CHARGES`, and (lines 13386–13437) a full **remapping of
   `OMGPR-C-INFRT-TYPE`** through one of up to three parallel `EVALUATE` tables depending on
   whether the line ended up exempt, or (if not exempt) whether `WS-APPLY-FRT-CST-SELL-SW` is
   `'S'` (sell-based) or `'V'` (variance-based, `NK0712` 2012 addition) — each table maps the same
   seven/eight source codes (`C`/`B`/`D`/`P`/`V`/`A`/`W`) to a *different* target letter depending
   on which table is active (e.g. source `'C'` becomes `'E'` when exempt, `'S'` when
   sell-based-non-exempt, and is not remapped at all in the variance-based table). **This document
   does not know what each single-letter `OMGPR-C-INFRT-TYPE` code means** (BLOCKED — no DCLGEN or
   copybook comment defines the domain) — only that exemption status and the freight-cost-basis
   switch together select which of three remapping tables applies.
8. **Effective/expiration dates contributed:** None.
9. **Exclusions and fallbacks:** Case 2d (group cost contract found, but none of sanctioned/
   division-01/individual conditions hold) has no explicit `ELSE` message — it silently falls to
   "not exempt" via `CONTINUE`, structurally similar to the "silent no-op for unmatched code" idiom
   flagged in R-JIT-001 item 9, though here the "no match" outcome (not exempt) is arguably a
   reasonable default rather than a true gap.
10. **Errors:** None raised in this paragraph.
11. **Confidence:** CONFIRMED for the five-level cascade and the flag-inversion. BLOCKED for the
    business meaning of the `OMGPR-C-INFRT-TYPE` letter codes themselves.

---

## What was scoped out of this pass

- **`7227-CHK-FOR-OVERRIDES`'s internals** (`7325-CHK-FOR-MEDC-OVERRIDES`, `7328-...-CD`,
  `7330-CHK-FOR-PCAT-OVERRIDES`, `7332-...-CD`, `7335-CHK-FOR-VEND-OVERRIDES`, `7337-...-CD`) — a
  medichoice-private-label → product-category → vendor-level override cascade (CONFIRMED to exist
  and to run before break-bulk/label/apply-label/service-fee calculations per its call sites, but
  its own logic — including what "`-CD`" suffix variants add — was not read).
- ~~`0245-PRO-BG-LOW-UOM-010`~~ — **RESOLVED** by T008, see `low-uom-break-bulk.md`.
- **`7325`–`7337`'s underlying tables** (likely `CUG55`/`56`/`57`/`73`/`74`/`75`, visible only as
  `MOVE ... TO CUGnn-C-EXEMPT-SVC-TYPE` targets in `7225` itself, lines 13134–13139, 13201–13206,
  13264–13269) — none supplied in `upload/`.

## Assumptions

1. `WS-VND-JIT-*-FEE` working fields are assumed to be correctly initialized to zero before `7225`
   runs each time (standard COBOL working-storage reuse across line items in a loop); this was not
   independently re-verified.
2. `OMGPR-C-PRIVATE-LBL = 'O'` is read as "Owens-sourced" by analogy to `A6O016U`'s product-type
   convention (R-JIT-005 item 11) — an inference, not a confirmed definition.

## Blockers

1. `7227`'s six override-check paragraphs and their underlying tables — needed to know the *actual*
   vendor JIT fee value R-JIT-002's formulas consume in any case where an override exists.
2. ~~`0245-PRO-BG-LOW-UOM-010`~~ — **RESOLVED** by T008 (`low-uom-break-bulk.md`), which also
   surfaced new blockers of its own (`7707-CHECK-PANDAC-ACCT-FLAG`, listed there).
3. `OMGPR-C-INFRT-TYPE`'s letter-code domain (R-JIT-007 item 7) — no supplied source defines it.
4. `CUS120`'s actual `OMGPR-F-LUOM-ELIG-SW` eligibility rule (R-JIT-003 item 9) — same standing
   blocker as `program-inventory.md` §1.9, now shown to have a second, independent downstream
   consumer (the first being `CUP100`'s own break-bulk/LUOM-charge-amount switch already documented
   there).
5. No DB2/CICS/COBOL execution environment — same standing blocker as every characterization
   document in this set (not itself produced for this rule area yet — see T007 candidate scope).

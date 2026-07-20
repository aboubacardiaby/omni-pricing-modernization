# A6U01 — Sell Arrangement Resolution & Sell-Method Computation (T004)

**Scope:** Decision-table extraction for the sell-side counterpart to
`docs/cobol-analysis/decision-tables/contract-cost-method.md` — how the pricer finds a customer's
*sell arrangement* (the row that says "this account/group gets this base sell price/percentage")
and then computes an actual sell price from it. Paragraphs `7180`, `0180`, `0181`, and `7090`.
**Deliberately narrower than full coverage — see "What was scoped out" below.** This rule area is
structurally three parallel ~13-level search cascades (one per cost-resolution outcome from
`R-COST-001`), not one. Extracting all three to the same depth as `contract-cost-method.md` would
roughly triple this document's size; given the effort budget for this task, this pass extracts the
top-level dispatch (all three branches) and the *individual-contract* cascade in full, and
documents the other two cascades' existence, entry condition, and general shape without full
paragraph-by-paragraph detail.
**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`. Same DCLGEN caveat as the cost
document: no copybook for `SAG04/05/06/07/08/09/10`, `SASSC*`, `CUG10/11/06/07/33/34`, `BGG01/02/03`,
`ING01`, `VNG02` is supplied in `upload/` — every column named below is SQL-text-confirmed only.
**Confidence key:** same as `contract-cost-method.md` — `CONFIRMED` / `INFERRED` / `BLOCKED`.

---

## Control-flow map (CONFIRMED)

```text
0040-PROCESS-SELL-N-ADJ
  └─ 7180-PRO-SELL-ARR-010          ("A6Z01-PRO SELL ARR MODULE")
       ├─ [OMGPR-F-JIT-EXEMPT = 'Y'] → skip nothing further is skipped in the code as currently
       │    active (see R-SELL-001 item 9 — most of the historical skip logic is commented out)
       ├─ 7730-SEL-ACCT-SELL-ARR-010     (always: resolve WS-ACCT-BAS-SELL)
       ├─ 7733-FND-CUST-BASE-SELL-ARR    (always: resolve WS-CUST-NBR-BAS-SELL)
       ├─ [WS-INDIV-CNT]      → 0180-PRO-SELL-INDV-CON-010  → 0181-PRO-SELL-INDV-CON-01-010
       ├─ [WS-PRIM-GRP-CNT or WS-OTHER-GRP-CNT] → 7075-PRO-SELL-GRP-CONT-010 (NOT fully extracted
       │                                            this pass — see "What was scoped out")
       └─ [WS-COST-CONT-NOT-FND] → 0185-PRO-SELL-ACQ-COST-010 (NOT fully extracted this pass)

7090-PRO-SELL-AMTS-010            ("A6Z01-PRO SELL AMTS MODULE")
  ("sell method" #0 default) → LIST PRICE, no sell arrangement found at all
  #1  GROSS MARGIN            (WS-C-SELL-PRC-METHOD = gross-margin)
  #2  COST PLUS               (WS-C-SELL-PRC-METHOD = cost-plus)
  #3  LIST PRICE              (OMGPR-PRICING-METHOD = list-price, sell arr found)
  #4  LIST LESS                (OMGPR-PRICING-METHOD = list-less)
  #5  SUGGESTED SELL           (OMGPR-PRICING-METHOD = suggested-sell, needs cost contract)
  #6  SUGGESTED SELL MARKUP    (OMGPR-PRICING-METHOD = suggested-sell-markup)
  #7  SUGGESTED SELL MARKDOWN  (OMGPR-PRICING-METHOD = suggested-sell-markdown)
  #8  STATED PRICE             (OMGPR-PRICING-METHOD = stated-price)
  (HC override, SJ0821 2021 addition) → net-delivered override, applied last, unconditionally
                                          replaces whatever #0-#8 computed if OMGPR-F-HC-SELL-FLAG='Y'
```

**Key cross-link to `contract-cost-method.md` (CONFIRMED):** which of the three `7180` branches
runs is entirely determined by `R-COST-001`'s outcome from the cost side — `WS-INDIV-CNT`,
`WS-PRIM-GRP-CNT`/`WS-OTHER-GRP-CNT`, and `WS-COST-CONT-NOT-FND` are read from
`WS-F-CONTRACT-TYPE-SW` (set in `0195`'s individual/group branches) and `WS-F-COST-CONT-FND`,
never independently re-derived. **The sell side cannot be characterized in isolation from the cost
side** — a scenario for this document always presupposes a specific cost-resolution outcome from
the other document.

---

## Rule R-SELL-001 — Sell-arrangement source dispatch

1. **Rule ID / name:** R-SELL-001 — Which sell-arrangement cascade runs is gated by which
   cost-contract source won `R-COST-001`, not searched independently.
2. **Program / paragraph:** `A6U01`, `7180-PRO-SELL-ARR-010`, lines 11807–12185.
3. **Preconditions:** Entered once per line from `0040-PROCESS-SELL-N-ADJ` (itself only reached for
   price/cost-only/default requests, per `program-inventory.md` §1.4's top-level dispatch — not for
   sell-only or JIT-on-cost requests, which have their own separate entry points).
4. **Data/SQL dependencies:** `7730-SEL-ACCT-SELL-ARR-010` and `7733-FND-CUST-BASE-SELL-ARR` (not
   read in this pass beyond their call sites — they populate `WS-ACCT-BAS-SELL` and
   `WS-CUST-NBR-BAS-SELL`, the base-sell-arrangement keys every subsequent level uses; BLOCKED for
   their own internal SQL).
5. **Priority relative to competing rules:** Three mutually exclusive branches, selected by data
   state carried over from `R-COST-001`/`R-COST-003` (CONFIRMED, not a fresh independent check):
   - `WS-INDIV-CNT` true (an individual cost contract won `R-COST-001`) → `0180` (R-SELL-002).
   - `WS-PRIM-GRP-CNT` or `WS-OTHER-GRP-CNT` true (a group cost contract won) → `7075` (not fully
     extracted, see below).
   - `WS-COST-CONT-NOT-FND` true (no cost contract at all, price-list fallback per `R-COST-001`
     item 9) → `0185` (not fully extracted, see below).
   These are **not** mutually exclusive by construction the way `7085`'s `EVALUATE` was for cost —
   they are three independent `IF` blocks (lines 11855, 11940, 12052). CONFIRMED by reading: since
   `WS-F-CONTRACT-TYPE-SW`/`WS-F-COST-CONT-FND` can only reflect one cost-resolution outcome per
   line, at most one of the three `IF` guards is true in practice — but this is a data invariant
   carried from the cost side, not something `7180` itself enforces. A future maintainer changing
   `R-COST-001`'s field-setting without updating `7180`'s guards could break this invariant
   silently; flagged as a coupling risk between the two rule areas.
6. **Calculation and rounding stage:** N/A at dispatch level.
7. **Output fields:** `OMGPR-I-BAS-SELL` (which specific sell-arrangement row won),
   `OMGPR-C-SELL-TYPE`/`OMGPR-C-SELL-LEVEL` (from `WS-F-SELL-ARR-TYPE-SW`/`WS-F-SELL-ARR-LEVEL-SW`,
   set inside whichever cascade ran), `OMGPR-N-BUY-GROUP-SHORT-SELL`/`OMGPR-I-DIVISION-SELL` (only
   when the winning arrangement is buy-group-level, `OMGPR-I-BUY-GROUP-SELL > ZEROES`),
   `OMGPR-T-SELL-COMMENT` (populated from a hold field keyed by which type won — account, corporate,
   group, or customer-number — if not already set by the cascade itself).
8. **Effective/expiration dates contributed:** `SAG05-D-SELL-ARR-EXP` and
   `SAG09-D-SELL-ASGN-EXPIRE` (individual/account path) or `BGG02-D-BGM-END-MEM` and the relevant
   `CUG10`/`CUG06` buy-group-priority-expiry (group path) — same "closest expiration wins" array
   mechanism as the cost side, added redundantly after *each* of the three branches (CONFIRMED,
   near-identical code blocks at lines 11873–11934, 11988–12047, 12071–12129 — this is the same
   date-expiry logic copy-pasted three times rather than factored into a shared paragraph; not a
   behavior difference, just a maintenance observation).
9. **Exclusions and fallbacks:** The historical JIT-exempt short-circuit (`GO TO 7180-EXIT` when
   the line has an `'A'`/`'C'`-type JIT service fee) is **entirely commented out** (`BM0523`, lines
   11818–11825) — only the outer `IF OMGPR-F-JIT-EXEMPT = 'Y' CONTINUE ELSE <nothing active>`
   remains, meaning **as currently written, no JIT condition skips sell-arrangement processing** —
   CONFIRMED by reading; if this comment-out was intentional (a prior behavior being retired) that
   is fine, but if it was accidental (commented out for debugging and never restored) that would be
   a live discrepancy between intended and actual behavior. Flagged for business confirmation, not
   assumed either way.
10. **Errors:** None raised directly by `7180` itself; errors belong to the underlying cascades.
11. **Confidence:** CONFIRMED for the three-way dispatch structure and its dependency on
    cost-side state. INFERRED that this coupling (rather than an independent sell-arrangement
    search) is intentional design, not an artifact of incremental modification — no comment
    explains *why* the sell search depends on the cost outcome rather than running independently.

---

## Rule R-SELL-002 — Individual-contract-linked sell-arrangement cascade

1. **Rule ID / name:** R-SELL-002 — When the line's cost came from an individual contract, its sell
   price is searched through up to 13 ordered levels (account/customer-number/corporate/sub-group
   direct levels, then a parent-group walk), first match wins.
2. **Program / paragraph:** `A6U01`, `0180-PRO-SELL-INDV-CON-010` (lines 2974–3444) and
   `0181-PRO-SELL-INDV-CON-01-010` (lines 3446–3642, the parent-group walk continuation).
3. **Preconditions:** `WS-INDIV-CNT` true (per R-SELL-001). Within `0180`, most levels are further
   gated on `WS-ACCT-BAS-SELL > 0`, `WS-CUST-NBR-BAS-SELL > 0`, or `WS-CORP-BAS-SELL > 0` (i.e., a
   given level is only attempted if its corresponding base-sell key was actually resolved by the
   unconditional `7730`/`7733` calls in `7180`, or by the corporate-sell lookup inside `0180`
   itself).
4. **Data/SQL dependencies — the full ordered level list (CONFIRMED, comment-numbered exactly as
   in source, `0180` lines 2988–3437 then `0181` lines 3493–3637):**

   | # | Level | Gate | Sub-paragraph | Table |
   |---|---|---|---|---|
   | 1 | Account product | `WS-ACCT-BAS-SELL > 0` | `7130-SEL-SELL-PRODUCT-010` | `SAG08` |
   | 1.25 | Customer-nbr product | `WS-CUST-NBR-BAS-SELL > 0` | `7130-SEL-SELL-PRODUCT-010` | `SAG08` |
   | 1.5 | Corporate product | `WS-CORP-BAS-SELL > 0` | (inline `7130`-style logic + `7240`/`7245` group-vs-corp reconciliation, see item 5) | `SAG07`? (INFERRED — corp-specific table not confirmed) |
   | 2 | Account vendor-contract-specific override | `WS-ACCT-BAS-SELL > 0` | `7135-SEL-SELL-VEND-CNT-010` | `SAG08` (reused) |
   | 2.5 | Customer-nbr vendor-contract override | `WS-CUST-NBR-BAS-SELL > 0` | `7135-SEL-SELL-VEND-CNT-010` | `SAG08` |
   | 3 | Account product category | `WS-ACCT-BAS-SELL > 0` and `OMGPR-S-PROD-CATEGORY > 0` | `7125-SEL-SELL-CAT-010` | `SAG06` |
   | 3.25 | Customer-nbr product category | same, keyed off `WS-CUST-NBR-BAS-SELL` | `7125-SEL-SELL-CAT-010` | `SAG06` |
   | 3.5 | Corporate category | `WS-CORP-BAS-SELL > 0` | `7160-PRO-CORP-CAT-SELL-010` + `7250-TRY-GROUP-CAT-010` (see item 5) | — |
   | 3.8 | Account special-service-code (2022 addn., `BM1022`) | `WS-ACCT-BAS-SELL > 0` | `7122-SEL-SELL-SSC-010` | `SASSC` |
   | 3.9 | Customer-nbr special-service-code | `WS-CUST-NBR-BAS-SELL > 0` | `7122-SEL-SELL-SSC-010` | `SASSC` |
   | 4 | Account vendor | `WS-ACCT-BAS-SELL > 0` | `7120-SEL-SELL-VENDOR-010` | `SAG04` |
   | 4.5 | Customer-nbr vendor | `WS-CUST-NBR-BAS-SELL > 0` | `7120-SEL-SELL-VENDOR-010` | `SAG04` |
   | 4.5(corp) | Corporate vendor | `WS-CORP-BAS-SELL > 0` | `7165-PRO-CORP-VEND-SEL-010` + `7255-TRY-GROUP-VENDOR-010` (see item 5) | — |
   | *(basic group sell info resolved here — `7230-PRO-SELL-PRI-BG-010`, gating levels 5–9 on `WS-GROUP-BAS-SELL > 0`, itself requiring buy-group membership via `7770-SEL-BGM-WITH-NBR-010`)* |
   | 5 | Sub-group product | `WS-GROUP-BAS-SELL > 0` | `7130-SEL-SELL-PRODUCT-010` | `SAG08` |
   | 6 | Sub-group product category | same + `OMGPR-S-PROD-CATEGORY > 0` | `7125-SEL-SELL-CAT-010` | `SAG06` |
   | 6.5 | Sub-group special-service-code | `WS-GROUP-BAS-SELL > 0` | `7122-SEL-SELL-SSC-010` | `SASSC` |
   | 7 | Sub-group vendor | `WS-GROUP-BAS-SELL > 0` | `7120-SEL-SELL-VENDOR-010` | `SAG04` |
   | 8 | Account default | `WS-ACCT-BAS-SELL > 0` | `7115-SEL-SELL-DFLT-010` | `SAG10` |
   | 8.5 | Customer-nbr default | `WS-CUST-NBR-BAS-SELL > 0` | `7115-SEL-SELL-DFLT-010` | `SAG10` |
   | 9 | Sub-group default | `WS-GROUP-BAS-SELL > 0` | `7115-SEL-SELL-DFLT-010` | `SAG10` |
   | *(if still not found, `0180` hands off to `0181`, which loops up the parent-buy-group chain via `7585-SEL-PARENT-SELL-010`/`BGG03`, repeating levels 10–13 once per parent level until a match or no more parents)* |
   | 10 | Parent-group product | `WS-GROUP-BAS-SELL > 0` (recomputed per parent) | `7130-SEL-SELL-PRODUCT-010` | `SAG08` |
   | 11 | Parent-group product category | same + `OMGPR-S-PROD-CATEGORY > 0` | `7125-SEL-SELL-CAT-010` | `SAG06` |
   | 11.5 | Parent-group special-service-code | `WS-GROUP-BAS-SELL > 0` | `7122-SEL-SELL-SSC-010` | `SASSC` |
   | 12 | Parent-group vendor | `WS-GROUP-BAS-SELL > 0` | `7120-SEL-SELL-VENDOR-010` | `SAG04` |
   | 13 | Parent-group default | `WS-GROUP-BAS-SELL > 0` | `7115-SEL-SELL-DFLT-010` | `SAG10` |

   All tables listed are BLOCKED for full DCLGEN, per the document header.
5. **Priority relative to competing rules:** Strictly sequential, first-match-wins via `GO TO
   0180-EXIT`/`GO TO 0181-EXIT` after every successful level (CONFIRMED — every level in the table
   above ends with exactly this pattern in the source). Two levels have extra internal logic worth
   calling out specifically:
   - **Level 1.5 (corporate product, lines 3027–3057):** if a corporate-level sell arrangement is
     found, the code does **not** immediately accept it — it first tries `7240-TRY-GROUP-PROD-010`
     (does the customer's buy-group *also* have a product-level arrangement?). If the group-level
     search succeeds, the **group** arrangement wins over the corporate one already found
     (`GO TO 0180-EXIT` inside the group-found branch). Only if the group search fails does the
     code fall back and re-accept the original corporate match (`7245-MOVE-CORP-SELL--D-010`
     restores it). **Group-level product arrangements silently take priority over
     corporate-level ones, even though corporate was checked first** — CONFIRMED, lines 3132–3151.
     The identical pattern repeats at level 3.5 (corp category vs. `7250-TRY-GROUP-CAT-010`, lines
     3129–3152) and level 4.5-corp (corp vendor vs. `7255-TRY-GROUP-VENDOR-010`, lines 3212–3236).
   - This means the *effective* priority order for corporate-tier customers is not simply
     "corporate before group" despite the numbering (1.5 before 5, 3.5 before 6, 4.5-corp before
     7) — it is **"corporate match found, then immediately re-checked against group; group wins
     ties."** A reader relying only on the numeric comment ordering would get this wrong.
6. **Calculation and rounding stage:** N/A — this rule selects the winning `SAG0x`/`SASSC` row;
   actual price computation happens in `R-SELL-003` (`7090`), using `OMGPR-I-BAS-SELL` and the
   winning row's `PRICING_METHOD`/percentage fields (not themselves traced to their exact column
   names in this pass — BLOCKED, `7130`/`7135`/`7125`/`7120`/`7115`/`7122` bodies were not read).
7. **Output fields:** `WS-F-SELL-ARR-TYPE-SW` (account / customer-nbr / corporate / group
   indicator), `WS-F-SELL-ARR-LEVEL-SW` (product / contract / category / special-service /
   vendor / default indicator), `OMGPR-I-BAS-SELL`, and — for any group-level win —
   `OMGPR-I-BUY-GROUP-SELL`/`OMGPR-S-BG-MEMBER-SELL`/`OMGPR-C-BG-TYPE-SELL` (priority vs.
   other-group, mirroring the same `WS-C-SELL-PRIORITY = +1` pattern seen on the cost side's
   `WS-C-COST-PRIORITY`)/`OMGPR-Q-PREF-TIER-LEVEL`/`OMGPR-D-BG-TIER-START`.
8. **Effective/expiration dates contributed:** Handled by the caller (`7180`, per R-SELL-001 item
   8), not inside `0180`/`0181` themselves.
9. **Exclusions and fallbacks:** No contract-exclusion-style filter exists in this cascade (unlike
   `R-COST-002`'s `7360-VERIFY-FOR-EXCL`) — CONFIRMED by absence; sell arrangements are not subject
   to the same exclusion-list concept as cost contracts in the paragraphs read. If no level (1
   through 13, across both direct and parent-chain search) matches, `0181`'s `PERFORM UNTIL`
   loop simply exhausts the parent chain (`WS-FIND-PARENT-SW` goes to `'N'` when `7585` finds no
   further parent) and control returns to `7180` with no sell arrangement found — `7090`
   (`R-SELL-003`) then falls back to its own list-price default (see `R-SELL-003` item 9).
10. **Errors:** None found raised directly in `0180`/`0181` in the paragraphs read — every branch
    is a data-driven continue/exit, not a fatal-error path. (Contrast with the cost side, where
    several branches raise fatal DB errors — this cascade appears to treat "not found at every
    level" as a normal, expected outcome rather than an error condition.)
11. **Confidence:** CONFIRMED for the level sequence, gating conditions, and the
    corporate-vs-group tie-break (item 5) — all directly read. BLOCKED for the exact column-level
    behavior of `7130`/`7135`/`7125`/`7120`/`7115`/`7122`/`7230`/`7585` (which fields they select,
    their own WHERE-clause preconditions) — these paragraph *bodies* were not read in this pass,
    only their call sites and pass/fail effect on `WS-SELL-ARR-FND`. Treat every row in the item-4
    table as "this level exists and runs in this position," not as "this level's exact SQL
    precondition is confirmed."

---

## Rule R-SELL-003 — Sell-method computation dispatch

1. **Rule ID / name:** R-SELL-003 — Once a sell arrangement (or its absence) is known, one of up
   to 9 mutually-exclusive-by-data computation methods produces `OMGPR-A-CUS-UOM-SELL-PRC`.
2. **Program / paragraph:** `A6U01`, `7090-PRO-SELL-AMTS-010`, lines 10171–10742 (methods), plus
   `7095-POPULATE-MARKUP-BKT`, lines 10744–10805 (markup-bucket side effect for methods #1/#2).
3. **Preconditions:** Called from `0040-PROCESS-SELL-N-ADJ` immediately after `R-SELL-001`/
   `R-SELL-002` (or the not-fully-extracted group/acquisition-cost cascades) have run and set
   `WS-SELL-ARR-FND`/`OMGPR-PRICING-METHOD`/`WS-C-SELL-PRC-METHOD`.
4. **Data/SQL dependencies:** No new SQL in `7090` itself for methods #1–#8 — they consume fields
   already populated by the winning sell-arrangement row (`OMGPR-PRICING-METHOD`,
   `WS-C-SELL-PRC-METHOD`, `WS-P-SELL-*` percentage fields — provenance of the `WS-P-SELL-*` working
   fields not traced in this pass, BLOCKED) plus already-resolved cost/price-list fields
   (`OMGPR-A-TOTAL-COST`, `OMGPR-A-VND-PRC-BEST-QTY`/`-LST-HOSP`/`-LST-DOC`,
   `OMGPR-A-CNT-LN-SUGG-SELL` from `R-COST-005`). One extra SQL-adjacent call: `7170-SEL-SPEC-VER-010`
   (usage-based pricing verification, methods #1/#2's "USAGE SECTION," not itself read this pass).
5. **Priority relative to competing rules:** **Not** a single `EVALUATE` (unlike `7085` on the cost
   side) — a sequence of independent `IF WS-SELL-ARR-FND AND <field> = <code> ...` blocks, each
   testing the same `OMGPR-PRICING-METHOD`/`WS-C-SELL-PRC-METHOD` field for a different literal
   value (CONFIRMED, lines 10289, 10423, 10547, 10561, 10601, 10625, 10653, 10679). Since that
   field holds exactly one value at a time, only one block's guard is true in practice — but, like
   `R-SELL-001` item 5, this is a data invariant, not something the `IF` sequence enforces
   structurally. A default (`OMGPR-C-BUSINESS`-keyed list price, lines 10258–10282) runs
   unconditionally **before** any of the 8 guarded blocks and is only overridden by whichever block
   fires — i.e., method computation always starts from a list-price baseline, then a matching
   method (if any) overwrites it. **After all 8 method blocks, one more unconditional override runs
   (lines 10719–10736, `SJ0821` 2021 addition): if `OMGPR-F-HC-SELL-FLAG = 'Y'` (a health-system
   override), the sell price and method are replaced again**, regardless of which of methods #0–#8
   fired — CONFIRMED, this is the true last-word rule, not any of the 8 named methods. This mirrors
   the `HC-*` override series already flagged as present in `A6U01`'s copybook set
   (`program-inventory.md` §1.4, `HCOVD*` copybooks) but not previously traced to an effect — this
   is that effect, now confirmed.
6. **Calculation and rounding stage:** Every method computes `OMGPR-A-CUS-UOM-SELL-PRC ROUNDED`
   (standard COBOL round-half-up), confirmed formulas:
   - **#1 Gross margin:** `OMGPR-A-TOTAL-COST / (1 - OMGPR-PRICING-PERCENTAGE)`, with an explicit
     guard clamping the percentage to `0.9999` if it would be `>= 1` (CONFIRMED, lines 10408–10410,
     preventing a divide-by-zero-or-negative — the comment explains this was added specifically to
     tolerate previously-allowed-in-error data, not a business rule per se).
   - **#2 Cost plus:** `OMGPR-A-TOTAL-COST + (OMGPR-A-TOTAL-COST * OMGPR-PRICING-PERCENTAGE)`.
   - **#3 List price:** no computation — uses the price-list value already in
     `OMGPR-A-CUS-UOM-SELL-PRC` from the unconditional default (item 5), just relabels the method.
   - **#4 List less:** `<price-list-field> - (<price-list-field> * WS-P-SELL-LIST-LESS)`, where
     `<price-list-field>` is chosen by `OMGPR-C-BUSINESS` (`01`→`OMGPR-A-VND-PRC-BEST-QTY`,
     `02`→`-LST-HOSP`, `03`→`-LST-DOC`, other→`-LST-DOC` as the default/highest price — same
     3-tier business-type selector as the unconditional list-price default in item 5).
   - **#5 Suggested sell:** `OMGPR-A-CNT-LN-SUGG-SELL` directly (from `R-COST-005`'s output),
     **only if** `WS-COST-SUGGESTED-SELL` is true and that value is `> 0`; otherwise falls back to
     `'LIST-DEF'` (list-price-default) instead — CONFIRMED, this is the concrete link where
     `R-COST-005`'s `WS-F-COST-SUGGESTED-SELL` output field (set only for cost-entry-methods
     `02/03/04/05/08`) gates whether sell-method #5 can even be attempted.
   - **#6 Suggested sell markup:** `OMGPR-A-CNT-LN-SUGG-SELL + (OMGPR-A-CNT-LN-SUGG-SELL *
     WS-P-SELL-CONT-SUGG)`, same suggested-sell-must-exist guard as #5, plus
     `WS-P-SELL-CONT-SUGG > 0`.
   - **#7 Suggested sell markdown:** same as #6 but subtracting instead of adding.
   - **#8 Stated price:** `(WS-A-SELL-PROD-PRC * WS-CA-CONVERT-UP-UMF) / WS-CA-CONVERT-DOWN-UMF`,
     after `PERFORM 0160-CVT-STATED-PRC-010` (the same UOM-conversion helper referenced in the cost
     document's control-flow map) — requires `WS-C-SELL-PROD-UM > SPACES`.
   - **HC override (last word):** either `OMGPR-A-TOTAL-COST + (OMGPR-A-TOTAL-COST *
     HC-PROD-OVRD-PERCENT)` (if `HC-PROD-OVRD-TYPE = 'COST+'`) or a UOM-converted
     `HC-PROD-OVRD-SELL-PRC` (via `9980-CVT-HC-STATED-PRC`, not itself read) otherwise.
   - Methods #1 and #2 additionally call `7095-POPULATE-MARKUP-BKT`, which — depending on which of
     five `SET WS-MARKUP-* TO TRUE` flags was set during the percentage-source sub-cascade (item
     7) — computes one `OMGPR-A-MKP-*` bucket as `sell price − total cost`, and, **only for the
     specific fee codes billed monthly** (`OMGPR-MONTHLY-AUTO-BILL-*`/`-MANUAL-BILL-*`, per
     product-category — `GS`/`GN`/`MI`/`MN`/`MC`), **subtracts that markup back out of
     `OMGPR-A-CUS-UOM-SELL-PRC`** — CONFIRMED, lines 10746–10802. This is a real, confirmed
     side-effect: for monthly-billed markup categories, the "markup" is computed and recorded but
     then excluded from the immediate sell price (presumably because it is billed separately, on a
     monthly cycle, rather than embedded per-order) — an important distinction for anyone
     reconciling `OMGPR-A-CUS-UOM-SELL-PRC` against `OMGPR-A-MKP-*` fields.
7. **Output fields:** `OMGPR-A-CUS-UOM-SELL-PRC`, `OMGPR-PRICING-METHOD` (a 10-char label —
   `'GM        '`, `'C(+)      '`, `'LIST      '`, `'LIST(-)   '`, `'CSS       '`, `'CSS(+)    '`,
   `'CSS(-)    '`, `'STATED PRC'`, `'LIST-DEF  '`, or `'NET DEL'` for the HC override),
   `OMGPR-C-SELL-PRC-METHOD`, `OMGPR-PRICING-PERCENTAGE`, and (methods #1/#2 only)
   `OMGPR-A-MKP-GRP-SAN-GS`/`-GRP-NSC-GN`/`-IND-MI`/`-NON-CON-MN`/`-CUS-MC` per `7095`.
8. **Effective/expiration dates contributed:** None in `7090` itself.
9. **Exclusions and fallbacks:** If `WS-SELL-ARR-NOT-FND` (no arrangement matched anywhere in
   `R-SELL-002`/the un-extracted group/acq-cost cascades) **and** no HC override applies, the
   unconditional item-5/item-6 default stands: business-type-keyed list price, method label
   `'LIST-DEF  '` — CONFIRMED, lines 10277–10282. This is the ultimate fallback for the entire sell
   side, structurally parallel to `R-COST-001`'s price-list fallback on the cost side.
10. **Errors:** Two fatal error paths inside the gross-margin/cost-plus percentage-source
    sub-cascade (item 7 pattern in each method's `IF OMGPR-C-ACCT-PRCE-METHOD = WS-COST-CONTRACT-IND`
    branch): if the account's pricing method is cost-contract-based but the code falls through every
    named sub-case (not custom, no cost contract found... contradiction with a contract having been
    found, wrong division for the individual/group distinction), it raises error `602` (gross
    margin, line 10331) or `603` (cost plus, line 10461), both fatal, `GO TO 0020-EXIT-PRICER`.
    These read as "should be unreachable" guards (defensive `ELSE` branches for a logically
    exhaustive-by-design `IF` chain) rather than expected business conditions — CONFIRMED as
    written, not confirmed whether they are ever actually reachable given the account-pricing-method
    values that exist in practice (BLOCKED, no DCLGEN for the domain of `OMGPR-C-ACCT-PRCE-METHOD`).
11. **Confidence:** CONFIRMED for all 8 named methods' guard conditions and formulas, and for the
    HC-override last-word behavior. BLOCKED for the gross-margin/cost-plus percentage-*source*
    sub-cascade's own precondition table (which `WS-P-SELL-*` field is used when) — item 6 above
    describes the branching *structure* (custom → group-fees/report-group → division-01-stock →
    individual-or-non-division-01 → error) as read, but the underlying `VNG02-C-CUSTOM-IND`,
    `CCG09-I-RPT-GRP`, `OMGPR-F-GRP-CNT-FEES`, `BGG01-I-DIVISION` fields' own provenance was not
    independently re-traced beyond what's already documented in `contract-cost-method.md`.

---

## What was scoped out of this pass (explicitly deferred, not silently skipped)

- **`7075-PRO-SELL-GRP-CONT-010`** (group-cost-contract-linked sell search, called when
  `WS-PRIM-GRP-CNT`/`WS-OTHER-GRP-CNT`) and its own "01" continuation
  **`7076-PRO-SELL-GRP-CONT-01-010`** — structurally similar to `R-SELL-002` (a numbered cascade of
  comparable length, confirmed by the same `#1`/`#2`/... comment style at lines 9415–10070), but
  with a *different* level ordering and table set (spot-checked: starts "ACCOUNT PRODUCT #1" at
  line 9415 like `0180`, but "SUB GROUP VENDOR CONTRACT SPECIFIED OVERRID #4" at line 9531 has no
  direct analogue in `0180`'s level list) — **not assumed identical to R-SELL-002**, genuinely not
  extracted this pass.
- **`0185-PRO-SELL-ACQ-COST-010`** / **`0186-PRO-SELL-ACQ-COST-01-010`** (acquisition-cost-linked
  sell search, called when `WS-COST-CONT-NOT-FND`) — a third parallel cascade (lines 3644–4365
  roughly), also not extracted.
- **`7730-SEL-ACCT-SELL-ARR-010`, `7733-FND-CUST-BASE-SELL-ARR`, `7230-PRO-SELL-PRI-BG-010`,
  `7770-SEL-BGM-WITH-NBR-010`, `7585-SEL-PARENT-SELL-010`, `7590-SEL-BGM-WITHOUT-N-010`** — the
  "resolve the base-sell key" and "resolve buy-group membership/parent" mechanics that every level
  of `R-SELL-002` depends on were referenced by call site and effect on `WS-*-BAS-SELL`/
  `WS-BG-MEMBER-FND`, not read internally.
- **`7130/7135/7125/7120/7115/7122`** (the actual `SAG0x`/`SASSC` SELECT paragraphs each cascade
  level calls) — referenced by name and table only, per `R-SELL-002` item 11.
- **`7170-SEL-SPEC-VER-010`** (usage-based pricing verification for sell-method #1/#2's "USAGE
  SECTION") and **`9980-CVT-HC-STATED-PRC`** (HC-override UOM conversion) — referenced, not read.

Any of the above would be a reasonable scope for a follow-up decision-table task, in the same way
`contract-cost-method.md` flagged blockers for its own follow-up.

## Assumptions

1. `WS-INDIV-CNT`, `WS-PRIM-GRP-CNT`, `WS-OTHER-GRP-CNT`, `WS-COST-CONT-NOT-FND` are assumed to be
   88-level conditions on `WS-F-CONTRACT-TYPE-SW`/`WS-F-COST-CONT-FND` (the same fields
   `contract-cost-method.md` documents being set) based on naming and usage pattern — their formal
   `88` declarations were not located and re-verified in this pass.
2. `7075`/`0185`'s cascades are assumed to follow the same "sequential IF, first-match-wins,
   `GO TO ...-EXIT`" idiom as `0180`/`R-SELL-002`, based on the visible comment-numbering style —
   not confirmed by reading their bodies.

## Blockers

1. `7075`/`7076` and `0185`/`0186` full extraction (see "What was scoped out").
2. Exact provenance of `WS-P-SELL-*` percentage fields and `OMGPR-PRICING-METHOD`/
   `WS-C-SELL-PRC-METHOD` values (which `SAG0x` column each comes from) — needed before this
   document's method guards can be turned into precise per-column preconditions.
3. Whether error `602`/`603` (R-SELL-003 item 10) are truly unreachable in practice, or represent a
   real data scenario this task didn't identify — needs either a full domain listing of
   `OMGPR-C-ACCT-PRCE-METHOD` values or confirmation from whoever maintains the account-pricing
   setup process.
4. Same execution-environment blocker as the characterization-scenarios document: no DB2/CICS/COBOL
   access, so nothing here has been run against real data.

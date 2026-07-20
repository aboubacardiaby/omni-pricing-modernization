# A6U01 — Sell Selection Decision Tables (T008)

**Scope:** Ordered decision-table extraction, per `CLAUDE.md`'s required 11-element rule format,
for the sell-price hierarchy, formulas, price lock, and list-price fallback named in T008's task
title and acceptance criteria: account, customer, contract, group, parent, corporate, product,
category, vendor, and default paths.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL` (26,657 lines) and `OMGPR.CPY`.
Line citations refer to `A6U01.CBL` unless a different file is named. No DCLGEN copybooks for
`SAG04/06/07/08/09/10`, `SASSC`, `CUG06/07/10/11/33/34`, `BGG01/02/03`, `ING01`, `VNG02`, `CUG31`
are supplied in `upload/` — every field sourced from these is marked BLOCKED or INFERRED at the
point it is used, never assumed.

**Depends on (per `tasks.md`):** T001 (`docs/cobol-analysis/program-inventory.md`, complete) and
T005 (`docs/cobol-analysis/sql-query-inventory.csv` + `db2-table-inventory.md`, complete).

**Relationship to `docs/rules/cost-selection-rules.md` (T007) and `docs/rules/fees-and-
adjustments.md`/`date-selection.md`/`rounding.md` (T009/T010):** the sell side cannot be
characterized in isolation from the cost side — which of the three sell-arrangement cascades in
this document runs is entirely determined by T007's cost-resolution outcome (R-COST-000/003), and
several formulas here consume T007's outputs directly (R-COST-005's `OMGPR-A-CNT-LN-SUGG-SELL`,
R-HC-002's healthcare-adjusted cost fields). This document also independently corroborates
`fees-and-adjustments.md`'s R-DISTMKP-001/002 (both extractions arrived at the identical
gross-margin formula and markup-bucket mechanism from different starting points) and resolves a
blocker left open by prior work: `OMGPR-F-HC-SELL-FLAG`'s setter (see R-SELL-005).

**Provenance:** three of the five rules below (R-SELL-001, R-SELL-002, R-SELL-003) carry forward
prior decision-table work from `omni-claude`'s `sell-arrangement-resolution.md`, built against the
identical (MD5-verified) `A6U01.CBL`, re-verified rather than re-derived. That prior work
explicitly scoped out two of the three sell-arrangement cascades and the entire price-lock
mechanism; this document fills both gaps (R-SELL-004, R-SELL-005, R-LOCK-001..003).

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Cross-cutting findings

1. **Three independently-coded sell-arrangement cascades exist, selected by the cost side's own
   resolution outcome, not a fresh sell-side decision** (R-SELL-001) — individual-contract-linked
   (R-SELL-002, 13 levels), group-contract-linked (R-SELL-004, ~14 levels), and
   acquisition-cost-linked (R-SELL-005, ~12 levels plus a healthcare-override check). They share
   a common "first-match-wins, sequential IF, GO TO exit" idiom and substantially overlapping
   level names (product/category/special-service/vendor/default, at account/customer/sub-group/
   parent-group scope), but are NOT identical: only the individual-contract cascade has a
   corporate tier, and only the individual- and group-contract cascades have a vendor-contract-
   specific-override tier (the acquisition-cost cascade has neither, consistent with there being
   no contract to attach an override to).
2. **Corporate-tier matches are not final** — in the individual-contract cascade, a found
   corporate-level sell arrangement is immediately re-checked against the customer's buy-group;
   if the group also has a matching arrangement, the group wins despite corporate having been
   checked first (R-SELL-002 item 5). The numeric level ordering in the source comments does not
   reflect actual effective priority for corporate-tier customers.
3. **A healthcare sell-override can replace the computed price twice, independently, at two
   different points in the pipeline** — once mid-cascade (R-SELL-005's `9955-FIND-HC-SELL-OVERRIDE`,
   only reachable via the acquisition-cost-linked path) and once as the unconditional last word
   after all 8 named sell methods have already run (R-SELL-003's `OMGPR-F-HC-SELL-FLAG` check).
   Both ultimately feed the same flag/output fields; this document traces both the setter and the
   consumer for the first time in one place.
4. **Price-lock reconciliation runs AFTER normal sell-price computation, not instead of it**
   (R-LOCK-001/002) — a fresh price is always computed first; the lock mechanism then decides
   whether to keep it or overwrite it with a frozen historical value, based on whether the
   underlying cost/method/percentage-or-price changed since the lock was recorded.
5. **Only two paragraphs in the entire pricer are lock-aware** — surcharge and vendor-cost-
   adjustment skip themselves entirely for a locked price (R-LOCK-003); every fee area in
   `fees-and-adjustments.md` (freight, JIT, PANDAC, SurgiTrak, delivery, rebates, distribution/
   markup) runs unconditionally regardless of lock status. This was exhaustively verified by
   grepping every occurrence of `OMGPR-PRICE-LOCKED` in the source.
6. **The gross-margin formula and markup-bucket mechanism were independently confirmed by two
   separate extraction passes** (T009's `R-DISTMKP-001/002` and this document's `R-SELL-003`,
   built from different entry points into the same code) and arrived at byte-identical formulas —
   a rare opportunity for cross-corroboration in this project, called out explicitly rather than
   silently deduplicated.

---

## 1. Sell-arrangement dispatch, individual-contract cascade, and method computation

# Sell-arrangement dispatch, individual-contract cascade, and method computation

**Provenance note:** this section is carried over from `omni-claude`'s prior
`docs/cobol-analysis/decision-tables/sell-arrangement-resolution.md` (built against the identical
`A6U01.CBL` — MD5-verified match between the two repos' `upload/` copies), re-verified for T008
rather than re-derived from scratch. Content and line citations are unchanged; framing/
cross-references are adapted to sit alongside this document's new price-lock and
group/acquisition-cascade sections.

Control-flow (CONFIRMED):
```
0040-PROCESS-SELL-N-ADJ
  -> 7180-PRO-SELL-ARR-010          ("A6Z01-PRO SELL ARR MODULE")
       -> 7730-SEL-ACCT-SELL-ARR-010     (always: resolve WS-ACCT-BAS-SELL)
       -> 7733-FND-CUST-BASE-SELL-ARR    (always: resolve WS-CUST-NBR-BAS-SELL)
       [WS-INDIV-CNT]                    -> 0180-PRO-SELL-INDV-CON-010 -> 0181 (R-SELL-002)
       [WS-PRIM-GRP-CNT or WS-OTHER-GRP-CNT] -> 7075-PRO-SELL-GRP-CONT-010 (see R-SELL-004)
       [WS-COST-CONT-NOT-FND]            -> 0185-PRO-SELL-ACQ-COST-010 (see R-SELL-005)

7090-PRO-SELL-AMTS-010            ("A6Z01-PRO SELL AMTS MODULE")
  #0 default -> LIST PRICE (no sell arrangement found at all)
  #1 GROSS MARGIN | #2 COST PLUS | #3 LIST PRICE | #4 LIST LESS | #5 SUGGESTED SELL |
  #6 SUGGESTED SELL MARKUP | #7 SUGGESTED SELL MARKDOWN | #8 STATED PRICE
  HC override (SJ0821, 2021) -> net-delivered override, applied last, unconditional if
                                  OMGPR-F-HC-SELL-FLAG='Y'
```

**Key cross-link:** which of the three `7180` branches runs is entirely determined by the cost
side's own resolution outcome (`docs/rules/cost-selection-rules.md`'s R-COST-000/R-COST-003) —
`WS-INDIV-CNT`, `WS-PRIM-GRP-CNT`/`WS-OTHER-GRP-CNT`, and `WS-COST-CONT-NOT-FND` are read from
state already set by the cost cascade, never independently re-derived. The sell side cannot be
characterized in isolation from the cost side.

### R-SELL-001 Sell-arrangement source dispatch
1. ID/Name: R-SELL-001 Which sell-arrangement cascade runs, gated by cost-resolution outcome
2. Program/paragraph: 7180-PRO-SELL-ARR-010, lines 11807-12185
3. Preconditions: entered once per line from 0040-PROCESS-SELL-N-ADJ (reached only for price/
   cost-only/default pricing modes, not sell-only or JIT-on-cost, which have separate entry
   points per the top-level dispatcher already documented in `fees-and-adjustments.md`'s JIT
   section)
4. Data deps: 7730-SEL-ACCT-SELL-ARR-010 and 7733-FND-CUST-BASE-SELL-ARR (not read to formula
   depth — populate WS-ACCT-BAS-SELL/WS-CUST-NBR-BAS-SELL, the base-sell keys every subsequent
   level uses; BLOCKED for internal SQL)
5. Priority: three mutually exclusive branches, selected by data state carried over from the
   cost side (not a fresh independent check): WS-INDIV-CNT true -> 0180 (R-SELL-002);
   WS-PRIM-GRP-CNT/WS-OTHER-GRP-CNT true -> 7075 (R-SELL-004); WS-COST-CONT-NOT-FND true -> 0185
   (R-SELL-005). These are three independent IF blocks (lines 11855, 11940, 12052), not a single
   mutually-exclusive EVALUATE — since the underlying field can only hold one state, at most one
   fires in practice, but this is a data invariant carried from the cost side, not something 7180
   itself enforces structurally. A future change to the cost side's field-setting without
   updating these guards could break this invariant silently — flagged as a coupling risk between
   the two rule documents.
6. Calculation: N/A at dispatch level
7. Output: OMGPR-I-BAS-SELL (which specific sell-arrangement row won), OMGPR-C-SELL-TYPE/
   OMGPR-C-SELL-LEVEL, OMGPR-N-BUY-GROUP-SHORT-SELL/OMGPR-I-DIVISION-SELL (buy-group-level wins
   only), OMGPR-T-SELL-COMMENT
8. Dates: SAG05-D-SELL-ARR-EXP and SAG09-D-SELL-ASGN-EXPIRE (individual/account path) or
   BGG02-D-BGM-END-MEM and the relevant CUG10/CUG06 buy-group-priority-expiry (group path) — same
   closest-expiration array mechanism as `docs/rules/date-selection.md`'s R-DATE-003, added
   redundantly after each of the three branches (near-identical code blocks, not factored into a
   shared paragraph — a maintenance observation, not a behavior difference)
9. Exclusions/fallbacks: the historical JIT-exempt short-circuit (`GO TO 7180-EXIT` when the line
   has an 'A'/'C'-type JIT service fee) is entirely commented out (BM0523) — only the outer
   `IF OMGPR-F-JIT-EXEMPT = 'Y' CONTINUE ELSE <nothing active>` remains, meaning as currently
   written, no JIT condition skips sell-arrangement processing. Flagged for business confirmation
   whether this comment-out was intentional or accidental — not assumed either way.
10. Errors: none raised directly by 7180 itself; errors belong to the underlying cascades
11. Confidence: CONFIRMED for the three-way dispatch structure and its dependency on cost-side
    state. INFERRED that this coupling is intentional design, not an artifact of incremental
    modification — no comment explains why the sell search depends on the cost outcome rather
    than running independently.

### R-SELL-002 Individual-contract-linked sell-arrangement cascade
1. ID/Name: R-SELL-002 13-level ordered sell-arrangement search (individual-contract path)
2. Program/paragraph: 0180-PRO-SELL-INDV-CON-010 (2974-3444) and
   0181-PRO-SELL-INDV-CON-01-010 (3446-3642, parent-group walk continuation)
3. Preconditions: WS-INDIV-CNT true (R-SELL-001). Within 0180, most levels are further gated on
   WS-ACCT-BAS-SELL > 0, WS-CUST-NBR-BAS-SELL > 0, or WS-CORP-BAS-SELL > 0 (a level only runs if
   its base-sell key was actually resolved upstream)
4. Data deps — full ordered level list (CONFIRMED, comment-numbered exactly as in source, 0180
   lines 2988-3437 then 0181 lines 3493-3637):

   | # | Level | Gate | Sub-paragraph | Table |
   |---|---|---|---|---|
   | 1 | Account product | WS-ACCT-BAS-SELL > 0 | 7130-SEL-SELL-PRODUCT-010 | SAG08 |
   | 1.25 | Customer-nbr product | WS-CUST-NBR-BAS-SELL > 0 | 7130-SEL-SELL-PRODUCT-010 | SAG08 |
   | 1.5 | Corporate product | WS-CORP-BAS-SELL > 0 | inline + 7240/7245 group-vs-corp reconcile | SAG07? (INFERRED) |
   | 2 | Account vendor-contract override | WS-ACCT-BAS-SELL > 0 | 7135-SEL-SELL-VEND-CNT-010 | SAG08 |
   | 2.5 | Customer-nbr vendor-contract override | WS-CUST-NBR-BAS-SELL > 0 | 7135-SEL-SELL-VEND-CNT-010 | SAG08 |
   | 3 | Account product category | WS-ACCT-BAS-SELL>0 AND OMGPR-S-PROD-CATEGORY>0 | 7125-SEL-SELL-CAT-010 | SAG06 |
   | 3.25 | Customer-nbr product category | same, keyed off WS-CUST-NBR-BAS-SELL | 7125-SEL-SELL-CAT-010 | SAG06 |
   | 3.5 | Corporate category | WS-CORP-BAS-SELL > 0 | 7160-PRO-CORP-CAT-SELL-010 + 7250-TRY-GROUP-CAT-010 | — |
   | 3.8 | Account special-service-code (2022, BM1022) | WS-ACCT-BAS-SELL > 0 | 7122-SEL-SELL-SSC-010 | SASSC |
   | 3.9 | Customer-nbr special-service-code | WS-CUST-NBR-BAS-SELL > 0 | 7122-SEL-SELL-SSC-010 | SASSC |
   | 4 | Account vendor | WS-ACCT-BAS-SELL > 0 | 7120-SEL-SELL-VENDOR-010 | SAG04 |
   | 4.5 | Customer-nbr vendor | WS-CUST-NBR-BAS-SELL > 0 | 7120-SEL-SELL-VENDOR-010 | SAG04 |
   | 4.5(corp) | Corporate vendor | WS-CORP-BAS-SELL > 0 | 7165-PRO-CORP-VEND-SEL-010 + 7255-TRY-GROUP-VENDOR-010 | — |
   | *(basic group sell info resolved here — 7230-PRO-SELL-PRI-BG-010, gating levels 5-9 on WS-GROUP-BAS-SELL>0, requiring buy-group membership via 7770-SEL-BGM-WITH-NBR-010)* |
   | 5 | Sub-group product | WS-GROUP-BAS-SELL > 0 | 7130-SEL-SELL-PRODUCT-010 | SAG08 |
   | 6 | Sub-group product category | same + OMGPR-S-PROD-CATEGORY>0 | 7125-SEL-SELL-CAT-010 | SAG06 |
   | 6.5 | Sub-group special-service-code | WS-GROUP-BAS-SELL > 0 | 7122-SEL-SELL-SSC-010 | SASSC |
   | 7 | Sub-group vendor | WS-GROUP-BAS-SELL > 0 | 7120-SEL-SELL-VENDOR-010 | SAG04 |
   | 8 | Account default | WS-ACCT-BAS-SELL > 0 | 7115-SEL-SELL-DFLT-010 | SAG10 |
   | 8.5 | Customer-nbr default | WS-CUST-NBR-BAS-SELL > 0 | 7115-SEL-SELL-DFLT-010 | SAG10 |
   | 9 | Sub-group default | WS-GROUP-BAS-SELL > 0 | 7115-SEL-SELL-DFLT-010 | SAG10 |
   | *(if still not found, 0180 hands off to 0181, which loops up the parent-buy-group chain via 7585-SEL-PARENT-SELL-010/BGG03, repeating levels 10-13 once per parent level until a match or no more parents)* |
   | 10 | Parent-group product | WS-GROUP-BAS-SELL > 0 (recomputed per parent) | 7130-SEL-SELL-PRODUCT-010 | SAG08 |
   | 11 | Parent-group product category | same + OMGPR-S-PROD-CATEGORY>0 | 7125-SEL-SELL-CAT-010 | SAG06 |
   | 11.5 | Parent-group special-service-code | WS-GROUP-BAS-SELL > 0 | 7122-SEL-SELL-SSC-010 | SASSC |
   | 12 | Parent-group vendor | WS-GROUP-BAS-SELL > 0 | 7120-SEL-SELL-VENDOR-010 | SAG04 |
   | 13 | Parent-group default | WS-GROUP-BAS-SELL > 0 | 7115-SEL-SELL-DFLT-010 | SAG10 |

   All tables listed are BLOCKED for full DCLGEN.
5. Priority: strictly sequential, first-match-wins via `GO TO 0180-EXIT`/`GO TO 0181-EXIT` after
   every successful level. Two levels have extra internal logic:
   - **Level 1.5 (corporate product, lines 3027-3057):** if a corporate-level sell arrangement is
     found, the code does NOT immediately accept it — it first tries 7240-TRY-GROUP-PROD-010
     (does the customer's buy-group also have a product-level arrangement?). If the group-level
     search succeeds, the GROUP arrangement wins over the corporate one already found. Only if the
     group search fails does the code fall back and re-accept the original corporate match
     (7245-MOVE-CORP-SELL--D-010 restores it). **Group-level product arrangements silently take
     priority over corporate-level ones, even though corporate was checked first.** The identical
     pattern repeats at level 3.5 (corp category vs. 7250-TRY-GROUP-CAT-010) and level 4.5-corp
     (corp vendor vs. 7255-TRY-GROUP-VENDOR-010).
   - This means the effective priority order for corporate-tier customers is not simply
     "corporate before group" despite the numbering — it is "corporate match found, then
     immediately re-checked against group; group wins ties." A reader relying only on the numeric
     comment ordering would get this wrong.
6. Calculation: N/A — this rule selects the winning SAG0x/SASSC row; actual price computation
   happens in R-SELL-003 (7090), using OMGPR-I-BAS-SELL and the winning row's fields (not traced
   to exact column names — BLOCKED, 7130/7135/7125/7120/7115/7122 bodies not read)
7. Output: WS-F-SELL-ARR-TYPE-SW (account/customer-nbr/corporate/group indicator),
   WS-F-SELL-ARR-LEVEL-SW (product/contract/category/special-service/vendor/default indicator),
   OMGPR-I-BAS-SELL, and for any group-level win: OMGPR-I-BUY-GROUP-SELL/
   OMGPR-S-BG-MEMBER-SELL/OMGPR-C-BG-TYPE-SELL (priority vs. other-group, mirroring
   WS-C-COST-PRIORITY on the cost side)/OMGPR-Q-PREF-TIER-LEVEL/OMGPR-D-BG-TIER-START
8. Dates: handled by the caller (7180, R-SELL-001 item 8), not inside 0180/0181 themselves
9. Exclusions/fallbacks: no contract-exclusion-style filter exists in this cascade (unlike the
   cost side's 7360-VERIFY-FOR-EXCL, `cost-selection-rules.md`'s R-COST-001 item 9) — confirmed
   by absence; sell arrangements are not subject to the same exclusion-list concept as cost
   contracts. If no level (1-13, direct and parent-chain) matches, 0181's PERFORM UNTIL loop
   exhausts the parent chain and control returns to 7180 with no sell arrangement found — 7090
   (R-SELL-003) then falls back to its own list-price default (item 9 there).
10. Errors: none raised directly in 0180/0181 in the paragraphs read — every branch is a
    data-driven continue/exit, not a fatal-error path. Contrast with the cost side, where several
    branches raise fatal DB errors — this cascade treats "not found at every level" as a normal
    outcome, not an error condition.
11. Confidence: CONFIRMED for the level sequence, gating conditions, and the corporate-vs-group
    tie-break — all directly read. BLOCKED for the exact column-level behavior of
    7130/7135/7125/7120/7115/7122/7230/7585 (which fields they select, their own WHERE-clause
    preconditions) — these paragraph bodies were not read, only their call sites and pass/fail
    effect on WS-SELL-ARR-FND. Treat every row in the item-4 table as "this level exists and runs
    in this position," not as "this level's exact SQL precondition is confirmed."

### R-SELL-003 Sell-method computation dispatch
1. ID/Name: R-SELL-003 Nine-method (plus HC override) sell-price computation
2. Program/paragraph: 7090-PRO-SELL-AMTS-010, lines 10171-10742 (methods), plus
   7095-POPULATE-MARKUP-BKT, lines 10744-10805 (markup-bucket side effect for methods #1/#2)
3. Preconditions: called from 0040-PROCESS-SELL-N-ADJ immediately after R-SELL-001/002 (or
   R-SELL-004/005) have run and set WS-SELL-ARR-FND/OMGPR-PRICING-METHOD/WS-C-SELL-PRC-METHOD
4. Data deps: no new SQL in 7090 itself for methods #1-8 — consumes fields already populated by
   the winning sell-arrangement row (OMGPR-PRICING-METHOD, WS-C-SELL-PRC-METHOD, WS-P-SELL-*
   percentage fields — provenance not traced, BLOCKED) plus already-resolved cost/price-list
   fields (OMGPR-A-TOTAL-COST, OMGPR-A-VND-PRC-BEST-QTY/-LST-HOSP/-LST-DOC,
   OMGPR-A-CNT-LN-SUGG-SELL from `cost-selection-rules.md`'s R-COST-005). One extra call:
   7170-SEL-SPEC-VER-010 (usage-based pricing verification, methods #1/#2's "USAGE SECTION," not
   read this pass)
5. Priority: NOT a single EVALUATE (unlike the cost side's entry-method dispatch) — a sequence of
   independent `IF WS-SELL-ARR-FND AND <field> = <code> ...` blocks, each testing the same
   OMGPR-PRICING-METHOD/WS-C-SELL-PRC-METHOD field for a different literal value (lines 10289,
   10423, 10547, 10561, 10601, 10625, 10653, 10679). Since that field holds exactly one value at a
   time, only one block's guard is true in practice — a data invariant, not something the IF
   sequence enforces structurally. A default (OMGPR-C-BUSINESS-keyed list price, lines
   10258-10282) runs unconditionally BEFORE any of the 8 guarded blocks and is only overridden by
   whichever block fires — method computation always starts from a list-price baseline, then a
   matching method (if any) overwrites it. **After all 8 method blocks, one more unconditional
   override runs (lines 10719-10736, SJ0821 2021 addition): if OMGPR-F-HC-SELL-FLAG = 'Y' (a
   health-system override), the sell price and method are replaced again**, regardless of which of
   methods #0-8 fired — this is the true last-word rule, not any of the 8 named methods.
6. Calculation: every method computes OMGPR-A-CUS-UOM-SELL-PRC ROUNDED (standard COBOL
   round-half-up per `docs/rules/rounding.md`'s R-ROUND-002), confirmed formulas:
   - **#1 Gross margin:** `OMGPR-A-TOTAL-COST / (1 - OMGPR-PRICING-PERCENTAGE)`, with an explicit
     guard clamping the percentage to 0.9999 if it would be >= 1 (lines 10408-10410, preventing a
     divide-by-zero-or-negative — comment states this tolerates previously-allowed-in-error data,
     not a business rule per se). Cross-reference: this is the same formula and clamp already
     documented in `fees-and-adjustments.md`'s R-DISTMKP-001 (built independently during T009 —
     both extractions independently arrived at the identical formula/clamp, corroborating each
     other).
   - **#2 Cost plus:** `OMGPR-A-TOTAL-COST + (OMGPR-A-TOTAL-COST * OMGPR-PRICING-PERCENTAGE)`
   - **#3 List price:** no computation — uses the price-list value already in
     OMGPR-A-CUS-UOM-SELL-PRC from the unconditional default (item 5), just relabels the method
   - **#4 List less:** `<price-list-field> - (<price-list-field> * WS-P-SELL-LIST-LESS)`, where
     `<price-list-field>` is chosen by OMGPR-C-BUSINESS (`01`->OMGPR-A-VND-PRC-BEST-QTY,
     `02`->`-LST-HOSP`, `03`->`-LST-DOC`, other->`-LST-DOC` as the default/highest price — same
     3-tier business-type selector as the unconditional list-price default in item 5)
   - **#5 Suggested sell:** `OMGPR-A-CNT-LN-SUGG-SELL` directly (from
     `cost-selection-rules.md`'s R-COST-005 output), only if WS-COST-SUGGESTED-SELL is true and
     that value is > 0; otherwise falls back to 'LIST-DEF' (list-price-default) instead — this is
     the concrete link where the cost side's WS-F-COST-SUGGESTED-SELL output field (set only for
     cost-entry-methods 02/03/04/05/08) gates whether sell-method #5 can even be attempted
   - **#6 Suggested sell markup:** `OMGPR-A-CNT-LN-SUGG-SELL + (OMGPR-A-CNT-LN-SUGG-SELL *
     WS-P-SELL-CONT-SUGG)`, same suggested-sell-must-exist guard as #5, plus
     WS-P-SELL-CONT-SUGG > 0
   - **#7 Suggested sell markdown:** same as #6 but subtracting instead of adding
   - **#8 Stated price:** `(WS-A-SELL-PROD-PRC * WS-CA-CONVERT-UP-UMF) / WS-CA-CONVERT-DOWN-UMF`,
     after `PERFORM 0160-CVT-STATED-PRC-010` (the same UOM-conversion idiom used throughout this
     codebase) — requires WS-C-SELL-PROD-UM > SPACES
   - **HC override (last word):** either `OMGPR-A-TOTAL-COST + (OMGPR-A-TOTAL-COST *
     HC-PROD-OVRD-PERCENT)` (if HC-PROD-OVRD-TYPE = 'COST+') or a UOM-converted
     HC-PROD-OVRD-SELL-PRC (via 9980-CVT-HC-STATED-PRC, not read) otherwise
   - Methods #1 and #2 additionally call 7095-POPULATE-MARKUP-BKT, which — depending on which of
     five `SET WS-MARKUP-* TO TRUE` flags was set during the percentage-source sub-cascade —
     computes one OMGPR-A-MKP-* bucket as sell price minus total cost, and, only for the specific
     fee codes billed monthly, subtracts that markup back out of OMGPR-A-CUS-UOM-SELL-PRC (lines
     10746-10802). **This is the identical mechanism already fully documented in
     `fees-and-adjustments.md`'s R-DISTMKP-001/002** (built independently in T009); this document
     does not re-derive it, only cross-references it as the concrete producer of the classification
     R-SELL-003's gross-margin/cost-plus methods consume.
7. Output: OMGPR-A-CUS-UOM-SELL-PRC, OMGPR-PRICING-METHOD (a 10-char label — 'GM        ',
   'C(+)      ', 'LIST      ', 'LIST(-)   ', 'CSS       ', 'CSS(+)    ', 'CSS(-)    ',
   'STATED PRC', 'LIST-DEF  ', or 'NET DEL' for the HC override), OMGPR-C-SELL-PRC-METHOD,
   OMGPR-PRICING-PERCENTAGE, and (methods #1/#2 only) OMGPR-A-MKP-GRP-SAN-GS/-GRP-NSC-GN/-IND-MI/
   -NON-CON-MN/-CUS-MC per 7095
8. Dates: none in 7090 itself
9. Exclusions/fallbacks: if WS-SELL-ARR-NOT-FND (no arrangement matched anywhere in R-SELL-002 or
   R-SELL-004/005) AND no HC override applies, the unconditional item-5/item-6 default stands:
   business-type-keyed list price, method label 'LIST-DEF  ' (lines 10277-10282). **This is the
   ultimate list-price fallback for the entire sell side**, structurally parallel to the cost
   side's acquisition/dealer-cost fallback (`cost-selection-rules.md`'s R-ACQ-002).
10. Errors: two fatal error paths inside the gross-margin/cost-plus percentage-source
    sub-cascade: if the account's pricing method is cost-contract-based but the code falls through
    every named sub-case (not custom, no cost contract found, wrong division for the
    individual/group distinction), it raises error 602 (gross margin, line 10331) or 603 (cost
    plus, line 10461), both fatal, `GO TO 0020-EXIT-PRICER`. These read as "should be unreachable"
    defensive guards rather than expected business conditions — not confirmed whether they are
    ever actually reachable given the account-pricing-method values that exist in practice
    (BLOCKED, no DCLGEN for the domain of OMGPR-C-ACCT-PRCE-METHOD).
11. Confidence: CONFIRMED for all 8 named methods' guard conditions and formulas, and for the HC-
    override last-word behavior. BLOCKED for the gross-margin/cost-plus percentage-source
    sub-cascade's own precondition table (which WS-P-SELL-* field is used when) — the branching
    structure (custom -> group-fees/report-group -> division-01-stock -> individual-or-non-
    division-01 -> error) is confirmed as read, but the underlying VNG02-C-CUSTOM-IND,
    CCG09-I-RPT-GRP, OMGPR-F-GRP-CNT-FEES, BGG01-I-DIVISION fields' own provenance was not
    independently re-traced beyond what `cost-selection-rules.md` already documents.

---

## 2. Group-contract-linked and acquisition-cost-linked cascades

# Group-contract-linked and acquisition-cost-linked sell cascades (new extraction)

**Methodology note:** consistent with this project's established economy for large repetitive
cascades (see `fees-and-adjustments.md`'s surcharge tiers 5-16, `cost-selection-rules.md`'s
customer-side parity), the level LISTS below were extracted **mechanically** by grep-enumerating
every `** <LEVEL NAME> #<N> **`-style comment marker in each paragraph's line range (a legitimate,
verifiable technique — every level in this codebase's sell/cost cascades is comment-numbered by
the original authors) and spot-verifying the first and last few levels' actual code shape against
`R-SELL-002`'s already-confirmed pattern (first-match-wins, `GO TO <para>-EXIT` after success).
The per-level formula/output shape was directly re-read for `7075`'s first three levels and
`0185`'s sub-group tier (used to resolve an ambiguous grep gap, see R-SELL-005) and found
identical in structure to `R-SELL-002`'s corresponding levels; the remaining levels are assumed
(INFERRED) to follow the same shape, not individually re-transcribed.

### R-SELL-004 Group-cost-contract-linked sell-arrangement cascade
1. ID/Name: R-SELL-004 Ordered sell-arrangement search when cost came from a group contract
2. Program/paragraph: 7075-PRO-SELL-GRP-CONT-010 (9373-9849) and
   7076-PRO-SELL-GRP-CONT-01-010 (9852-10073, parent-group walk continuation)
3. Preconditions: WS-PRIM-GRP-CNT or WS-OTHER-GRP-CNT true (R-SELL-001) — the line's cost was
   resolved via a GROUP cost contract (`cost-selection-rules.md`'s R-COST-003/004/005), not an
   individual one
4. Data deps: same base-sell-key mechanism as R-SELL-002 (WS-ACCT-BAS-SELL/WS-CUST-NBR-BAS-SELL/
   WS-GROUP-BAS-SELL, resolved by 7180's unconditional calls plus this paragraph's own
   7580-SEL-GRP-SELL-ARR-010 for the group-level key); additionally seeds its buy-group context
   from `WS-I-BUY-GROUP-LOWEST`/`WS-S-BG-MEMBER-LOWEST` if those were set by the cost side's own
   parent-climb (`cost-selection-rules.md`'s R-COST-005 item 7 — "the lowest-level buy group with
   membership," saved specifically so the sell search can restart there even if the cost contract
   was ultimately found higher up the parent chain), falling back to `OMGPR-I-BUY-GROUP`/
   `OMGPR-S-BG-MEMBER` if not
5. Priority: strictly sequential, first-match-wins, identical `GO TO 7075-EXIT`/`GO TO 7076-EXIT`
   idiom to R-SELL-002 (spot-verified on levels 1, 1.5, and 2). **CONFIRMED level list
   (mechanically enumerated, source-numbered exactly as follows — note the source's own numbering
   has two visible duplicate/out-of-sequence labels, reproduced faithfully rather than silently
   corrected):**

   | # (as labeled in source) | Level | Table (by sub-paragraph reuse) |
   |---|---|---|
   | 1 | Account product | SAG07 (via 7130-SEL-SELL-PRODUCT-010) |
   | 1.5 | Customer-nbr product | SAG07 |
   | 2 | Sub-group product | SAG07 |
   | 3½ | Account vendor-contract-specific override | SAG08? (via 7135, INFERRED same as R-SELL-002's level 2) |
   | 3.5 | Customer-nbr vendor-contract-specific override | same |
   | 4 | Sub-group vendor-contract-specific override | same |
   | 5 | Account product category | SAG06 (via 7125-SEL-SELL-CAT-010) |
   | 5.5 | Customer-nbr product category | SAG06 |
   | 6 | Sub-group product category | SAG06 |
   | 6 *(source labels this "#6" again, not "6.5" — a numbering slip in the original comments, reproduced as-is)* | Sub-group special-service-code | SASSC (via 7122-SEL-SELL-SSC-010) |
   | 7 | Account vendor | SAG04 (via 7120-SEL-SELL-VENDOR-010) |
   | 7.5 | Customer-nbr vendor | SAG04 |
   | 8 | Sub-group vendor | SAG04 |
   | 9 | Account default | SAG10 (via 7115-SEL-SELL-DFLT-010) |
   | 9.5 | Customer-nbr default | SAG10 |
   | 9 *(source labels this "#9" again instead of continuing the sequence — a second numbering slip)* | Sub-group default | SAG10 |
   | 10 *(7076, physically appears AFTER the "#11" block below it in the source — the file's physical order does not match its own comment numbering here)* | Parent-group vendor-contract-specific override | same as level 4 |
   | 11 | Parent-group product | SAG07 |
   | 12 | Parent-group product category | SAG06 |
   | 12.5 | Parent-group special-service-code | SASSC |
   | 13 | Parent-group vendor | SAG04 |

   **CONFIRMED BY ABSENCE, a genuine structural difference from R-SELL-002:** this cascade has
   **no corporate tier at all** — no "CORP" level-comment appears anywhere in `7075`/`7076`'s
   entire line range (exhaustively grepped), unlike R-SELL-002's individual-contract cascade which
   has corporate sub-levels at 1.5/3.5/4.5-corp. A group-cost-contract-linked line never checks a
   corporate-level sell arrangement.
6. Calculation: N/A — row selection only, formula happens in R-SELL-003 exactly as for the
   individual-contract path (both cascades feed the same downstream `7090`)
7. Output: same field shape as R-SELL-002 (WS-F-SELL-ARR-TYPE-SW, WS-F-SELL-ARR-LEVEL-SW,
   OMGPR-I-BAS-SELL, and for group-level wins: OMGPR-I-BUY-GROUP-SELL/OMGPR-S-BG-MEMBER-SELL/
   OMGPR-C-BG-TYPE-SELL/OMGPR-Q-PREF-TIER-LEVEL/OMGPR-D-BG-TIER-START, spot-verified identical at
   levels 1/1.5/2)
8. Dates: same closest-expiration contribution mechanism as R-SELL-001/R-SELL-002 (not
   individually re-verified at every one of the levels above, consistent with this section's
   stated economy)
9. Exclusions/fallbacks: if no level matches through both the direct search (7075) and the
   parent-group climb (7076), control returns to the caller with no sell arrangement found —
   R-SELL-003 then applies the same list-price default as for the individual-contract path
10. Errors: not individually re-verified at every level (INFERRED to follow R-SELL-002's pattern
    of "not-found is a normal outcome, not a fatal error," based on the spot-verified levels)
11. Confidence: CONFIRMED for the level list (mechanically enumerated, including the two source
    numbering slips reproduced faithfully) and the no-corporate-tier finding (confirmed by
    exhaustive grep absence). INFERRED for the exact per-level SQL/formula detail beyond the three
    spot-verified levels — treat this rule as "these levels exist and run in this position," the
    same caveat R-SELL-002 already carries for its own non-spot-verified levels.

### R-SELL-005 Acquisition-cost-linked sell-arrangement cascade (no cost contract found)
1. ID/Name: R-SELL-005 Ordered sell-arrangement search when no cost contract was found at all
2. Program/paragraph: 0185-PRO-SELL-ACQ-COST-010 (3644-4113) and
   0186-PRO-SELL-ACQ-COST-01-010 (4116-4332, parent-group walk continuation)
3. Preconditions: WS-COST-CONT-NOT-FND true (R-SELL-001) — the cost side found no individual or
   group contract at all, falling back to acquisition/dealer cost or the healthcare-override
   mechanism (`cost-selection-rules.md`'s R-HC-001/002/R-ACQ-002)
4. Data deps: same base-sell-key mechanism as R-SELL-002/004
5. Priority: strictly sequential, first-match-wins, same `GO TO 0185-EXIT`/`GO TO 0186-EXIT`
   idiom (spot-verified on the sub-group product/category/SSC/vendor block, lines 3899-4009).
   **CONFIRMED level list:**

   | # | Level | Table |
   |---|---|---|
   | 1 | Account product | SAG07 |
   | 1.25 | Customer-nbr product | SAG07 |
   | 2 | Account product category | SAG06 |
   | 2.25 | Customer-nbr product category | SAG06 |
   | 3 | Account vendor | SAG04 |
   | 3.25 | Customer-nbr vendor | SAG04 |
   | 4 | Sub-group product | SAG07 |
   | 5 | Sub-group product category | SAG06 |
   | 5.5 | Sub-group special-service-code | SASSC |
   | 6 | Sub-group vendor | SAG04 |
   | *(healthcare sell-override check — see item 6)* |
   | 7 | Account default | SAG10 |
   | 7.5 | Customer-nbr default | SAG10 |
   | 8 | Sub-group default | SAG10 |
   | 09 (0186) | Parent-group product | SAG07 |
   | 10 | Parent-group product category | SAG06 |
   | 10.5 | Parent-group special-service-code | SASSC |
   | 11 | Parent-group vendor | SAG04 |
   | 12 | Parent-group default | SAG10 |

   **CONFIRMED BY ABSENCE, two structural differences from both R-SELL-002 and R-SELL-004:** (a)
   no corporate tier (same as R-SELL-004); (b) **no vendor-contract-specific-override tier at
   any scope** — neither `0185` nor `0186` contains an "OVERRID" level-comment anywhere
   (exhaustively grepped), unlike both R-SELL-002 (levels 2/2.5) and R-SELL-004 (levels 3½/3.5/4).
   This makes sense on reflection: a vendor-CONTRACT-specific override is inherently tied to
   having a contract to override in the first place — this cascade only runs when NO cost
   contract exists at all, so there is nothing for such an override to attach to.
6. Calculation: N/A for the direct/parent-walk levels themselves (same as R-SELL-002/004). **New
   finding, not present in the prior extraction of this rule area:** immediately after the direct
   levels (1 through 6) exhaust with no match, and only `IF WS-COST-CONT-NOT-FND` (lines
   4011-4025), this paragraph calls `9955-FIND-HC-SELL-OVERRIDE` (not itself read to formula depth
   this pass) — **this is the confirmed setter of `OMGPR-F-HC-SELL-FLAG`**, the field
   `fees-and-adjustments.md`'s R-SELL-003 (in this same document, ported from prior work) already
   documented as the "last word" override in `7090-PRO-SELL-AMTS-010`, but whose setter was
   previously BLOCKED/untraced. If `WS-F-HC-SELL-REC-FND` (an eligible healthcare sell-override
   record exists), `OMGPR-D-HC-SELL-EFF` is populated (via the same MM/DD/YYYY-to-timestamp
   reformat idiom used elsewhere in this codebase) and `OMGPR-F-HC-SELL-FLAG='Y'` is set, then the
   paragraph exits immediately (`GO TO 0185-EXIT`) — **the healthcare sell-override check only
   ever runs when the acquisition-cost-linked cascade's own direct levels (1-6) found nothing**,
   it is not attempted for lines whose cost came from an individual or group contract
   (R-SELL-002/004 have no equivalent call). Only if `9955` also finds nothing does the search
   continue to the account/customer/sub-group DEFAULT levels (7/7.5/8) and then the parent-group
   walk (0186).
7. Output: same field shape as R-SELL-002/004 for levels 1-8 and the parent walk;
   OMGPR-F-HC-SELL-FLAG/OMGPR-D-HC-SELL-EFF for the healthcare sell-override case
8. Dates: same closest-expiration mechanism (not individually re-verified per level); healthcare
   override contributes OMGPR-D-HC-SELL-EFF as a distinct, separately-tracked date (parallel to
   the healthcare COST override's OMGPR-D-HC-COST-EFF already documented in
   `cost-selection-rules.md`'s R-HC-001/002)
9. Exclusions/fallbacks: if neither the direct levels, the healthcare sell-override, the default
   levels, nor the parent-group walk finds anything, control returns with no sell arrangement
   found — R-SELL-003's list-price default applies, exactly as for the other two cascades
10. Errors: not individually re-verified beyond the spot-verified sub-group block (same INFERRED
    caveat as R-SELL-004)
11. Confidence: CONFIRMED for the level list, the two structural-absence findings, and the
    healthcare-sell-override discovery (directly read, lines 4011-4025). INFERRED for per-level
    SQL/formula detail beyond the spot-verified block. This rule resolves a blocker the prior
    extraction of this rule area left open ("the same HC-* override series... not previously
    traced to an effect" — now traced to both its setter, here, and its consuming effect, in
    R-SELL-003).

---

## 3. Price lock

# Price-lock rules — extracted (new extraction, not in prior omni-claude work)

Program/paragraph: A6U01.CBL `7205-PRO-PRICE-LOCKED-010` (12779-12865), calling
`7765-SEL-PRC-LOCK-010` (18101-18137, locked-price row lookup against table CUG31) and
`0210-CVT-PRC-LOCK-010` (4740-4764, UOM conversion of the locked amounts). Called from
`0040-PROCESS-SELL-N-ADJ` immediately after sell-arrangement search (7180, R-SELL-001) and
sell-method computation (7090, R-SELL-003) — i.e. **price-lock reconciliation runs AFTER a fresh
sell price has already been computed normally**, not instead of it.

### R-LOCK-001 Locked-price row lookup
1. ID/Name: R-LOCK-001 Prior locked-sell-price row lookup (CUG31)
2. Program/paragraph: 7765-SEL-PRC-LOCK-010, lines 18101-18137
3. Preconditions: called unconditionally as the first step of 7205, once per line, after normal
   sell-price computation has already run
4. Data deps: SELECT against CUG31 ("ACCT LOCKED SELL PRC") — exact WHERE-clause keying not
   re-transcribed this pass (deferred to the T005 SQL CSV for 9475-SQL-SELECT-010), but the
   table's own name and the surrounding field names (`CUG31-A-LP-TOTAL-SELL`,
   `-A-LP-TOTAL-COST`, `-A-LP-TOT-SELL-ADJ`, `-A-LP-TOT-COST-ADJ`, `-A-LP-UNADJ-UNT-CST`,
   `-C-LP-SELL-PRC-METH`, `-P-LP-SELL-PRC-PCT`, `-D-LOCKED-PRICE-EXP`) confirm this is a
   per-account (at minimum) snapshot of a previously-computed sell price, its cost basis, its
   sell-method/percentage, and its own expiration date — i.e. a price quote frozen at some earlier
   point in time for later reuse.
5. Priority: this lookup itself has no priority contention — it either finds a row or doesn't
6. Calculation: none — pure lookup
7. Output: `OMGPR-F-PRICE-LOCKED` set to 'Y' (SQLCODE=0, a locked-price row exists) or 'N'
   (SQLCODE=100, no row) — this is a TENTATIVE value, immediately re-evaluated by R-LOCK-002 if
   'Y' (see item 9)
8. Dates: `CUG31-D-LOCKED-PRICE-EXP` contributed to the closest-expiration array
   (`docs/rules/date-selection.md`'s R-DATE-003) only when a row is found
9. Exclusions/fallbacks: **CONFIRMED — finding a CUG31 row does not, by itself, mean the locked
   price will actually be used.** `OMGPR-F-PRICE-LOCKED='Y'` here only means "a lock record
   exists and R-LOCK-002 must check whether it's still valid" — the final, authoritative value of
   `OMGPR-F-PRICE-LOCKED` (and therefore whether downstream paragraphs skip themselves, R-LOCK-003)
   is decided by R-LOCK-002, not by this lookup alone. A reader who stops at "SQLCODE=0 means
   price is locked" would be wrong about the actual end-to-end behavior.
10. Errors: fatal DB error #123 (WHEN OTHER, table CUG31)
11. Confidence: CONFIRMED for control flow; INFERRED/deferred for the exact SELECT WHERE-clause
    keying (not re-transcribed, deferred to T005 SQL CSV)

### R-LOCK-002 Lock validity reconciliation: cost/method/percentage-or-price must be unchanged
1. ID/Name: R-LOCK-002 Locked price validity check and price-freeze override
2. Program/paragraph: 7205-PRO-PRICE-LOCKED-010, lines 12793-12860 (the body of
   `IF OMGPR-PRICE-LOCKED`, i.e. only runs when R-LOCK-001 found a row)
3. Preconditions: R-LOCK-001 found a CUG31 row (`OMGPR-PRICE-LOCKED` true from R-LOCK-001's
   tentative setting)
4. Data deps: the CUG31 row's own stored values (converted to the ordered UOM first via
   `0210-CVT-PRC-LOCK-010`, using the same UOM-conversion idiom used throughout this codebase —
   "THE PRICE LOCKING AMOUNTS ARE IN THE BASE UNIT OF MEASURE," per its header comment) compared
   against the just-computed current values: `OMGPR-A-CNT-LN-UNIT-COST`,
   `OMGPR-C-SELL-PRC-METHOD`, `OMGPR-PRICING-PERCENTAGE`, `OMGPR-A-CUS-UOM-SELL-PRC` (all outputs
   of R-SELL-001/002/003, already computed by the time this rule runs)
5. Priority: **two different comparison rule sets, selected by `OMGPR-C-SELL-PRC-METHOD`**
   (CONFIRMED, lines 12817-12821 vs. 12839-12843):
   - **Percentage-based methods** ('1' gross margin, '2' cost plus, '4' list less, '6' suggested-
     sell markup, '7' suggested-sell markdown): the lock is considered still valid if NONE of
     `CUG31-A-LP-UNADJ-UNT-CST` (locked cost basis), `CUG31-C-LP-SELL-PRC-METH` (locked method),
     or `CUG31-P-LP-SELL-PRC-PCT` (locked percentage) differ from their current equivalents.
   - **Price-based methods** ('3' list, '5' suggested, '8' stated, or any other/blank code): there
     is no meaningful "percentage" for these methods, so the comparison instead checks the locked
     cost basis, the locked method, and a recomputed locked SELL PRICE
     (`CUG31-A-LP-TOTAL-SELL - CUG31-A-LP-TOT-SELL-ADJ`) against the just-computed current sell
     price.
   Both branches are structurally identical in shape (cost-changed OR method-changed OR
   percentage/price-changed => lock invalidated), just comparing a different third field depending
   on which category the sell method falls into — a genuinely deliberate design choice, not an
   oversight, since percentage and price are not interchangeable concepts for methods 3/5/8.
6. Calculation: when the lock IS still valid: `OMGPR-A-CUS-UOM-SELL-PRC = CUG31-A-LP-TOTAL-SELL -
   CUG31-A-LP-TOT-SELL-ADJ` — **the freshly-computed sell price from R-SELL-003 is discarded and
   overwritten with the frozen historical price**, adjusted by subtracting the locked row's own
   recorded sell-adjustment amount. When the lock is NOT valid (something changed): no override —
   the freshly-computed R-SELL-003 price stands as-is.
7. Output: `OMGPR-A-CUS-UOM-SELL-PRC` (conditionally overwritten), `OMGPR-F-PRICE-LOCKED`
   (finalized to 'Y' if still valid, 'N' if invalidated — this is the authoritative value R-LOCK-003
   downstream reads)
8. Dates: none new beyond R-LOCK-001's contribution
9. Exclusions/fallbacks: if R-LOCK-001 found no row at all, this entire rule is skipped and
   `OMGPR-F-PRICE-LOCKED` simply stays 'N' from R-LOCK-001 — a line with no prior lock record is
   never subject to any of this reconciliation logic
10. Errors: none in this fragment
11. Confidence: CONFIRMED

### R-LOCK-003 Downstream adjustment bypass for locked prices
1. ID/Name: R-LOCK-003 Surcharge and vendor-cost-adjustment are skipped entirely for locked prices
2. Program/paragraph: `7210-PRO-SURCHARGE-010` line 12872-12874 (`IF OMGPR-PRICE-LOCKED GO TO
   7210-EXIT`) and `7215-PRO-VNDRCOSTADJ-010` line 12928-12929 (identical shape) — **CONFIRMED,
   exhaustively grep-verified: these are the ONLY two paragraphs in the entire pricer that check
   `OMGPR-PRICE-LOCKED` besides `7205` itself and the initial `7765` lookup** — freight, JIT,
   PANDAC, SurgiTrak, delivery, and every other fee area documented in
   `docs/rules/fees-and-adjustments.md` runs unconditionally regardless of lock status; only
   surcharge and vendor-cost-adjustment are lock-aware.
3. Preconditions: `OMGPR-F-PRICE-LOCKED = 'Y'` per R-LOCK-002's final determination
4. Data deps: none new
5. Priority: this check is the very first statement in both paragraphs — an immediate bypass, not
   a partial skip of some internal logic
6. Calculation: N/A — pure bypass
7. Output: `OMGPR-A-SURCHARGE`/`OMGPR-P-SURCHARGE`/`OMGPR-C-SURCHARGE` (R-SURCHARGE-001/002 in
   `fees-and-adjustments.md`) and `OMGPR-P-VNDCOSTADJ`/`OMGPR-C-VNDCOSTADJ`/`OMGPR-A-VNDCOSTADJ`
   remain at whatever they were initialized to (zero/spaces, per each paragraph's own top-of-
   paragraph MOVE statements, which run BEFORE the lock check in 7215 but the lock check in 7210
   is the very first statement with no preceding initialization — confirmed by re-reading both
   paragraphs' exact statement order)
8. Dates: none
9. Exclusions/fallbacks: this bypass is unconditional once the precondition holds — there is no
   partial-surcharge or partial-vendor-cost-adjustment for a locked-price line, it is all-or-
   nothing
10. Errors: none
11. Confidence: CONFIRMED (exhaustively verified by grepping every occurrence of
    `OMGPR-PRICE-LOCKED` in `A6U01.CBL` — six total: the two SET sites in R-LOCK-001, the four IF
    checks split across R-LOCK-002's own gate and these two downstream bypasses)

---

## What was scoped out of this pass

- **`7130`/`7135`/`7125`/`7120`/`7115`/`7122`/`7230`/`7770`/`7585`/`7590`/`7580`** (the actual
  `SAG0x`/`SASSC` SELECT paragraphs and buy-group-membership/parent-walk mechanics every level of
  R-SELL-002/004/005 depends on) — referenced by call site and pass/fail effect on
  `WS-SELL-ARR-FND`, not read internally. Same standing deferral as the prior extraction of
  R-SELL-002.
- **`9955-FIND-HC-SELL-OVERRIDE`'s own internals** (R-SELL-005) — its existence, precondition, and
  output fields are confirmed; its own table/WHERE-clause detail was not re-transcribed (by
  analogy to the parallel healthcare-cost mechanism in `cost-selection-rules.md`'s R-HC-001, it
  likely follows the same multi-table date-windowed join shape, but this was not independently
  verified).
- **`7765-SEL-PRC-LOCK-010`'s exact CUG31 WHERE-clause keying** (R-LOCK-001) — deferred to the
  T005 SQL inventory CSV.
- **The two source-comment numbering slips in R-SELL-004's level list** (duplicate `#6` and `#9`
  labels, and `7076`'s physically-out-of-order `#10`/`#11` blocks) — reproduced faithfully as
  found rather than silently renumbered; their cause (a level inserted later without renumbering
  everything after it, most likely) was not confirmed from any comment.

## Assumptions

1. `WS-INDIV-CNT`, `WS-PRIM-GRP-CNT`, `WS-OTHER-GRP-CNT`, `WS-COST-CONT-NOT-FND` are assumed to be
   88-level conditions on the same fields `cost-selection-rules.md` documents being set — carried
   forward from the prior extraction, not independently re-verified by locating their formal `88`
   declarations in this pass.
2. R-SELL-004's and R-SELL-005's levels beyond the spot-verified ones are assumed to follow
   R-SELL-002's confirmed per-level shape (SELECT via the named sub-paragraph, `IF WS-SELL-ARR-FND`,
   populate output fields, `GO TO ...-EXIT`) — consistent with every spot-verified level matching
   that shape exactly, but not proven for every row in either level table.
3. `9955-FIND-HC-SELL-OVERRIDE` is assumed to follow the same date-windowed multi-table join
   pattern as `cost-selection-rules.md`'s R-HC-001, by structural analogy (parallel naming,
   parallel output-field shape) — not independently confirmed.

## Blockers

1. The ten-plus SAG0x/SASSC/CUGxx/BGGxx SELECT and buy-group-mechanics paragraphs listed above —
   needed to turn every cascade's level table from "this level exists and runs in this position"
   into precise per-column preconditions.
2. `9955-FIND-HC-SELL-OVERRIDE`'s own table/column detail.
3. `7765-SEL-PRC-LOCK-010`'s exact CUG31 keying.
4. Whether sell-method dispatch errors #602/#603 (R-SELL-003 item 10, carried from prior work) are
   truly unreachable in practice — needs a full domain listing of `OMGPR-C-ACCT-PRCE-METHOD` or
   confirmation from whoever maintains account-pricing setup, same standing blocker as the prior
   extraction.
5. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set.


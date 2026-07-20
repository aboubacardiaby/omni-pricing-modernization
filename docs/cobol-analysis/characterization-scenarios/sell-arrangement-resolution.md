# A6U01 — Sell Arrangement Resolution & Sell-Method Computation: Characterization Scenarios (T005)

**Scope:** Characterization scenarios and expected decision paths for
`docs/cobol-analysis/decision-tables/sell-arrangement-resolution.md` (rules `R-SELL-001`–`003`).
Same format, method, and caveats as `docs/cobol-analysis/characterization-scenarios/
contract-cost-method.md` (T003) — read that document's "How to read a scenario" section if this is
the first document from this pair you're opening.

**What this document is NOT:** numeric golden-value tests. No DB2/CICS/COBOL execution access is
available in this environment; every scenario is a structural characterization (paragraph sequence
+ field provenance), not an executed/observed result. Formulas are stated symbolically; no example
numbers are invented.

**Scope boundary carried over from the decision-table document:** `7075`/`7076`
(group-contract-linked cascade) and `0185`/`0186` (acquisition-cost-linked cascade) were not
extracted to full depth in T004. Scenarios below that touch those branches state only what `R-SELL-
001` confirms (that the branch runs) and do not claim to characterize their internal level-by-level
behavior.

**Source of truth:** `docs/cobol-analysis/decision-tables/sell-arrangement-resolution.md` (cited as
`R-SELL-00x`) and `upload/A6U01.CBL` directly. Same confidence key as prior documents.

---

## Group A — Sell-arrangement source dispatch (`R-SELL-001`)

### SCN-SELL-001 — Individual cost contract won → individual sell cascade runs, others don't
- **Given:** `R-COST-001`/`R-COST-002` resolved the line's cost via an individual contract
  (`WS-INDIV-CNT` true) — e.g. the same data shape as `contract-cost-method.md`'s SCN-COST-001.
- **Decision path:** `7180-PRO-SELL-ARR-010` → `7730-SEL-ACCT-SELL-ARR-010` →
  `7733-FND-CUST-BASE-SELL-ARR` → `IF WS-INDIV-CNT` (true) → `0180-PRO-SELL-INDV-CON-010` (→
  `0181` if no direct-level match). **The `IF WS-PRIM-GRP-CNT OR WS-OTHER-GRP-CNT` and
  `IF WS-COST-CONT-NOT-FND` blocks are both skipped** — CONFIRMED, these are independent `IF`s (not
  an `EVALUATE`), but the data invariant from `R-COST-001` means at most one is ever true.
- **Expected outcome:** whatever `R-SELL-002`'s cascade produces (see Group B); `OMGPR-C-BG-TYPE`
  is **not** set from `WS-F-CONTRACT-TYPE-SW` in this branch (that assignment only happens in the
  group-cost branch, line 11942–11943 — CONFIRMED by its absence from the `WS-INDIV-CNT` block).
- **Confidence:** CONFIRMED. Citation: `R-SELL-001` item 5; `A6U01.CBL` 11855–11936.

### SCN-SELL-002 — Group cost contract won → group sell cascade runs (not extracted to full depth)
- **Given:** `R-COST-001`/`R-COST-004` resolved the line's cost via a group contract
  (`WS-PRIM-GRP-CNT` or `WS-OTHER-GRP-CNT` true) — e.g. SCN-COST-002's data shape.
- **Decision path:** `7180` → (individual block skipped) → `IF WS-PRIM-GRP-CNT OR
  WS-OTHER-GRP-CNT` (true) → `OMGPR-C-BG-TYPE ← WS-F-CONTRACT-TYPE-SW` → `7075-PRO-SELL-GRP-
  CONT-010`. **This task cannot state which of `7075`'s internal levels would match** — that
  cascade was not extracted in T004 (see that document's "What was scoped out").
- **Expected outcome:** BLOCKED beyond the fact that `7075` runs and, if it finds a match,
  populates the same `OMGPR-C-SELL-TYPE`/`-LEVEL`/`-I-BAS-SELL` fields via the same
  `WS-F-SELL-ARR-TYPE-SW`/`-LEVEL-SW` pattern (CONFIRMED at the `7180` call-site level, lines
  11940–12048), just sourced from `7075`'s own unread internals.
- **Confidence:** CONFIRMED for the dispatch; BLOCKED for the cascade's internal behavior.

### SCN-SELL-003 — No cost contract at all → acquisition-cost sell cascade runs (not extracted)
- **Given:** `R-COST-001` fell through to the price-list fallback (`WS-COST-CONT-NOT-FND` true) —
  SCN-COST-003's data shape.
- **Decision path:** `7180` → (individual and group blocks both skipped) → `IF
  WS-COST-CONT-NOT-FND` (true) → `0185-PRO-SELL-ACQ-COST-010`. Same BLOCKED caveat as SCN-SELL-002
  for `0185`'s internals.
- **Expected outcome:** BLOCKED beyond dispatch confirmation, same field-population pattern as
  SCN-SELL-002 (lines 12052–12131).
- **Confidence:** CONFIRMED for the dispatch; BLOCKED for the cascade's internal behavior.

### SCN-SELL-004 — Every one of the three branches misses → no sell arrangement found
- **Given:** the branch selected by `R-SELL-001` (whichever of the three it is) exhausts its
  cascade without a match — for the individual branch, this means all 13 `R-SELL-002` levels
  (direct + parent-walk) fail.
- **Decision path:** `7180` completes with `WS-SELL-ARR-FND` never set to `'Y'`
  (`WS-SELL-ARR-NOT-FND` true) → control returns to `0040-PROCESS-SELL-N-ADJ` → `7090-PRO-SELL-
  AMTS-010` runs with no method guard (item 5 of `R-SELL-003`) satisfied.
- **Expected outcome:** the unconditional business-type-keyed list price stands as the final sell
  price, `OMGPR-PRICING-METHOD = 'LIST-DEF  '` (CONFIRMED, `R-SELL-003` item 9; `A6U01.CBL`
  10277–10282), unless an HC override applies (SCN-SELL-017).
- **Confidence:** CONFIRMED.

---

## Group B — Individual-contract-linked sell cascade (`R-SELL-002`)

### SCN-SELL-005 — Account-level product match (level 1) short-circuits everything after it
- **Given:** `WS-ACCT-BAS-SELL > 0`; a `SAG08` row matches at the account+product level.
- **Decision path:** `0180-PRO-SELL-INDV-CON-010` → level 1 gate true → `7130-SEL-SELL-PRODUCT-
  010` (match) → `WS-SELL-ARR-FND` → `GO TO 0180-EXIT`. **Levels 1.25 through 13 (customer-nbr,
  corporate, all vendor/category/special-service/default levels, and the entire parent-group walk
  in `0181`) never execute.**
- **Expected outcome:** `WS-F-SELL-ARR-TYPE-SW = WS-ACCOUNT-IND`, `WS-F-SELL-ARR-LEVEL-SW =
  WS-PRODUCT-IND`, `OMGPR-I-BAS-SELL = WS-ACCT-BAS-SELL`.
- **Confidence:** CONFIRMED. Citation: `R-SELL-002` item 4 row 1; `A6U01.CBL` 2988–3008.

### SCN-SELL-006 — Corporate product match found, group product also matches → group wins
- **Given:** `WS-CORP-BAS-SELL > 0`; a corporate-level `SAG0x` product row matches (level 1.5);
  the customer's buy-group **also** has a product-level match for the same product.
- **Decision path:** `0180` → level 1 (no account match, `WS-ACCT-BAS-SELL` presumed 0 or its
  search failed) → level 1.25 (same, customer-nbr) → level 1.5: corp product search succeeds
  (`WS-SELL-ARR-FND = 'Y'`) → `MOVE 'N' TO WS-F-SELL-ARR-FND` (line 3133, deliberately resets the
  flag) → `7240-TRY-GROUP-PROD-010` → **group search also succeeds** → `WS-SELL-ARR-FND` true
  again → `GO TO 0180-EXIT` **without ever calling `7245-MOVE-CORP-SELL--D-010`** (the
  corp-restore paragraph is only reached in the `ELSE` branch, line 3138–3150).
- **Expected outcome:** the arrangement actually used is the **group** one from `7240`, not the
  corporate one originally found — `WS-F-SELL-ARR-TYPE-SW`/`OMGPR-I-BAS-SELL`/etc. reflect
  whatever `7240` set, not the corporate row. **This is the finding flagged in `R-SELL-002` item 5:
  the numeric comment ordering (corporate at 1.5, group at 5) does not reflect the actual effective
  priority for this specific case.**
- **Confidence:** CONFIRMED. Citation: `R-SELL-002` item 5 first bullet; `A6U01.CBL` 3129–3151.

### SCN-SELL-007 — Corporate product match found, group product does NOT match → corp match stands
- **Given:** same as SCN-SELL-006, but the buy-group has no product-level match.
- **Decision path:** `0180` → level 1.5 corp search succeeds → `7240-TRY-GROUP-PROD-010` fails
  (`WS-SELL-ARR-FND` false again) → `ELSE` branch: `MOVE 'Y' TO WS-F-SELL-ARR-FND` (restore) →
  `7245-MOVE-CORP-SELL--D-010` (re-populate the corp row's data, since `7240`'s attempt may have
  overwritten working storage) → `WS-F-SELL-ARR-TYPE-SW = WS-CORPORATE-IND` → `GO TO 0180-EXIT`.
- **Expected outcome:** the original corporate arrangement is used. This is the "normal" case the
  numeric ordering (1.5 before 5) suggests — it only holds when the group-level re-check fails.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 3138–3150.

### SCN-SELL-008 — No direct-level match, sub-group product match (level 5) via buy-group membership
- **Given:** no account/customer-nbr/corporate match at levels 1–4.5; the customer's buy-group
  has membership (`7770-SEL-BGM-WITH-NBR-010` finds `WS-BG-MEMBER-FND`) and a product-level
  `SAG08` row exists for that group.
- **Decision path:** `0180` → levels 1 through 4.5 all fail → `7230-PRO-SELL-PRI-BG-010`
  (resolve `WS-I-BUY-GROUP`/`WS-S-BG-MEMBER`) → `IF WS-ACCT-BG-PRI-FND OR WS-CUST-BG-PRI-FND` →
  `7770-SEL-BGM-WITH-NBR-010` → `WS-BG-MEMBER-FND` → `7580-SEL-GRP-SELL-ARR-010` →
  `WS-GROUP-BAS-SELL > 0` → level 5: `7130-SEL-SELL-PRODUCT-010` (match) → `GO TO 0180-EXIT`.
  **Note: buy-group *membership* is a precondition for even attempting levels 5–9** — CONFIRMED,
  `R-COST-003`'s comment at line 6250–6251 ("THE BUY GROUP MUST HAVE MEMBERSHIP BEFORE THE SELL
  ARRANGEMENT CAN BE USED") states this explicitly.
- **Expected outcome:** `WS-F-SELL-ARR-TYPE-SW = WS-GROUP-IND`, `OMGPR-I-BUY-GROUP-SELL`/
  `OMGPR-S-BG-MEMBER-SELL` populated, `OMGPR-C-BG-TYPE-SELL` set from `WS-C-SELL-PRIORITY = +1`
  (priority group) or the other-group indicator otherwise.
- **Confidence:** CONFIRMED. Citation: `R-SELL-002` item 4 row 5; `A6U01.CBL` 3238–3286.

### SCN-SELL-009 — No sub-group match, parent-group walk finds a match one level up
- **Given:** levels 1 through 9 (all direct + sub-group levels) fail; the sub-group's immediate
  parent buy-group (`BGG03-I-BUY-GROUP-PARENT`) has a product-level match.
- **Decision path:** `0180` falls through to `0181-PRO-SELL-INDV-CON-01-010` (`MOVE 'Y' TO
  WS-FIND-PARENT-SW`, `MOVE WS-I-BUY-GROUP TO BGG03-I-BUY-GROUP`) → `PERFORM UNTIL NOT
  (WS-FIND-PARENT-SW = 'Y')` → `7585-SEL-PARENT-SELL-010` finds a parent → re-resolves buy-group
  membership for that parent (`7590-SEL-BGM-WITHOUT-N-010` or direct `7580`) → level 10:
  `7130-SEL-SELL-PRODUCT-010` (match against the parent's `SAG08`) → `GO TO 0181-EXIT`. **The loop
  runs at most once in this scenario** — it would run again (levels 10–13 re-attempted against the
  *next* parent up) only if this parent also failed all four of its levels.
- **Expected outcome:** same field pattern as SCN-SELL-008, sourced from the parent buy-group
  instead of the original sub-group.
- **Confidence:** CONFIRMED. Citation: `R-SELL-002` item 4 rows 10–13; `A6U01.CBL` 3435–3527.

### SCN-SELL-010 — Grandparent walk: two parent levels both miss, third matches
- **Given:** neither the sub-group's immediate parent nor *that* parent's own parent has a match
  at any of levels 10–13; the next buy-group up the chain does.
- **Decision path:** `0181`'s `PERFORM UNTIL` loop body runs three times: iteration 1 (immediate
  parent, all four levels fail, `7585` advances to the next parent), iteration 2 (same, advances
  again), iteration 3 (match at whichever of levels 10–13 first succeeds, `GO TO 0181-EXIT`). If
  `7585-SEL-PARENT-SELL-010` ever returns "no more parents" (`WS-FIND-PARENT-SW = 'N'`) before a
  match, the loop exits via `GO TO 0181-EXIT` at line 3452–3454 with nothing found — this is the
  path that feeds SCN-SELL-004 when the individual branch is the one that exhausts.
- **Expected outcome:** same field pattern as SCN-SELL-009, from whichever ancestor buy-group
  first matches; **this task does not know how many buy-group parent levels can exist in practice**
  (BLOCKED — `BGG03`'s parent-chain depth is a data question, not a COBOL-logic question).
- **Confidence:** CONFIRMED for the loop mechanism; BLOCKED for how deep real data ever goes.

### SCN-SELL-011 — Product category absent skips every category-specific level
- **Given:** `OMGPR-S-PROD-CATEGORY = ZEROES` (same condition as `contract-cost-method.md`'s
  SCN-COST-012, and for the same upstream reason).
- **Decision path:** levels 3, 3.25, 3.5 (partially — corp-category is gated on `WS-CORP-BAS-SELL`
  not category, but its *inner* group-category re-check `7250-TRY-GROUP-CAT-010` still fires
  unconditionally once corp is entered — only the account/customer-nbr *category* gates at lines
  3093 and 3112 are skipped), 6, and 11 are skipped wherever their `IF OMGPR-S-PROD-CATEGORY > 0`
  guard is false (CONFIRMED gates at lines 3093, 3112, 3290, 3529). The cascade effectively loses
  its category-matching levels for this product, falling through directly from vendor-contract
  levels to vendor levels.
- **Expected outcome:** whichever of the remaining (non-category) levels matches first, per normal
  cascade order.
- **Confidence:** CONFIRMED.

---

## Group C — Sell-method computation dispatch (`R-SELL-003`)

### SCN-SELL-012 — Suggested-sell method, valid cost-side suggested value (cross-link to R-COST-005)
- **Given:** a sell arrangement was found with `OMGPR-PRICING-METHOD` = suggested-sell;
  **and**, on the cost side, `R-COST-005` resolved the contract's entry method to one of
  `02/03/04/05/08` (so `WS-F-COST-SUGGESTED-SELL = 'Y'` and `OMGPR-A-CNT-LN-SUGG-SELL > 0`, per
  `contract-cost-method.md` SCN-COST-016).
- **Decision path:** `7090-PRO-SELL-AMTS-010` → method #5 guard true
  (`WS-COST-CONT-FND AND WS-SELL-ARR-FND AND OMGPR-PRICING-METHOD = WS-SUGG-SELL-IND`) → inner
  guard `WS-COST-SUGGESTED-SELL AND OMGPR-A-CNT-LN-SUGG-SELL > 0` also true →
  `OMGPR-A-CUS-UOM-SELL-PRC ← OMGPR-A-CNT-LN-SUGG-SELL` directly (no further formula).
- **Expected outcome:** `OMGPR-PRICING-METHOD = 'CSS       '`. **This is the one sell method whose
  correctness this task can trace all the way back through both decision-table documents** — cost
  entry method 02 (say) → `CCG13` suggested-sell amount → `OMGPR-A-CNT-LN-SUGG-SELL` → sell method
  #5 → `OMGPR-A-CUS-UOM-SELL-PRC`, unchanged in value, just relabeled and re-homed.
- **Confidence:** CONFIRMED. Citation: `R-SELL-003` item 6 (method #5); `R-COST-005` item 7.

### SCN-SELL-013 — Suggested-sell method requested, but cost side didn't produce a suggested value
- **Given:** sell arrangement says suggested-sell, but the resolved contract's cost entry method
  was `01`/`06`/`07` (no suggested-sell lookup ever ran on the cost side, per SCN-COST-015/019) —
  or it was `02`-`05`/`08` but the `CCG13`-family value happened to be exactly zero.
- **Decision path:** method #5's outer guard is true, but the inner guard
  (`WS-COST-SUGGESTED-SELL AND OMGPR-A-CNT-LN-SUGG-SELL > 0`) is false → `ELSE` branch:
  `OMGPR-PRICING-METHOD = 'LIST-DEF  '`, `OMGPR-C-SELL-PRC-METHOD = SPACES`,
  `OMGPR-PRICING-PERCENTAGE = ZEROES`.
- **Expected outcome:** the line silently reverts to the list-price-default method **even though a
  sell arrangement matched** — the sell arrangement's own guard (`WS-SELL-ARR-FND`) was satisfied,
  but the *cost*-side prerequisite for method #5 specifically was not. This is a second concrete
  cross-link finding: **a sell arrangement configured for "suggested sell" produces a silently
  different result (plain list price, not an error) depending on what cost-entry-method the
  matched contract happens to use** — worth flagging to the business as a possibly-surprising
  interaction between two independently-configured settings (the sell arrangement's method choice,
  and the contract's cost entry method).
- **Confidence:** CONFIRMED. Citation: `R-SELL-003` item 6 (method #5), item 9; `A6U01.CBL`
  10601–10617.

### SCN-SELL-014 — Gross margin method, cost-contract-based percentage source, customer-specific override
- **Given:** sell arrangement says gross-margin; `OMGPR-C-ACCT-PRCE-METHOD` = cost-contract;
  `VNG02-C-CUSTOM-IND = 'Y'` and a customer-specific gross-margin percentage exists
  (`WS-P-SELL-GRSSMGN-CUS-NI NOT = -1`, i.e. not SQL-NULL).
- **Decision path:** `7090` → method #1 guard true → `OMGPR-C-ACCT-PRCE-METHOD =
  WS-COST-CONTRACT-IND` → `VNG02-C-CUSTOM-IND = 'Y' AND ...NI NOT = -1` → `OMGPR-PRICING-
  PERCENTAGE ← WS-P-SELL-GRSSMGN-CUS`, `SET WS-MARKUP-CUS-MC TO TRUE` → (falls through the
  STOCK/USAGE sections' `IF`s, which don't apply since `OMGPR-C-ACCT-PRCE-METHOD` is
  cost-contract, not stock/usage) → `OMGPR-PRICING-METHOD = 'GM        '` → percentage clamp check
  → `COMPUTE OMGPR-A-CUS-UOM-SELL-PRC ROUNDED = OMGPR-A-TOTAL-COST / (1 -
  OMGPR-PRICING-PERCENTAGE)` → `7095-POPULATE-MARKUP-BKT` (writes `OMGPR-A-MKP-CUS-MC`, since
  `WS-MARKUP-CUS-MC` was set).
- **Expected outcome:** `OMGPR-A-MKP-CUS-MC = OMGPR-A-CUS-UOM-SELL-PRC − OMGPR-A-TOTAL-COST`
  (computed, always); if this line's fee-billing frequency for the `MC` category is monthly
  (`OMGPR-MONTHLY-AUTO-BILL-MC` or `-MANUAL-BILL-MC`), that same markup amount is **then
  subtracted back out** of `OMGPR-A-CUS-UOM-SELL-PRC` (CONFIRMED, `R-SELL-003` item 6 last
  paragraph) — the immediate sell price ends up *lower* than the gross-margin formula alone would
  produce, with the difference presumably billed separately later.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 10301–10344, 10769–10778.

### SCN-SELL-015 — Gross margin method, percentage would exceed 100% → clamped, not rejected
- **Given:** whichever percentage-source branch fires for method #1 resolves to a value `>= 1`
  (i.e., `>= 100%`) — the comment (lines 10403–10406) attributes this to "data that was allowed in
  error due to a poorly thought out change," implying this is a data-quality guard, not a
  legitimate business value.
- **Decision path:** after the percentage-source sub-cascade sets `OMGPR-PRICING-PERCENTAGE`, the
  unconditional check `IF OMGPR-PRICING-PERCENTAGE >= 1 MOVE +0.9999 TO OMGPR-PRICING-PERCENTAGE`
  fires before the `COMPUTE`.
- **Expected outcome:** the sell price is computed using `0.9999` instead of the stored (invalid)
  percentage — silently corrected, no error raised, no diagnostic. Since gross margin's formula is
  `cost / (1 − pct)`, an uncapped percentage of `1.0` would divide by zero (an abend) and anything
  `> 1.0` would produce a negative sell price; the clamp prevents both. **This scenario exists to
  flag that any migration must reproduce this exact clamp value (`0.9999`), not merely "handle the
  edge case somehow" — a different clamp (e.g., `0.99`) would produce a materially different price
  for these already-anomalous rows.**
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 10403–10414.

### SCN-SELL-016 — Cost-contract-based percentage source, but scenario matches none of the named sub-cases → fatal error
- **Given:** `OMGPR-C-ACCT-PRCE-METHOD = WS-COST-CONTRACT-IND`; not custom; a cost contract *was*
  found but `CCG09-I-RPT-GRP <= ZEROS` and `OMGPR-F-GRP-CNT-FEES NOT = 'Y'` (fails the first
  sub-case); `BGG01-I-DIVISION NOT = '01'` is false and `OMGPR-F-GRP-CNT-FEES NOT = 'N'` is also
  arranged to fail the stock sub-case; and the contract is **not** individual (`WS-INDIV-CNT`
  false) while `BGG01-I-DIVISION = '01'` (fails the final "individual or non-division-01" catch-all)
  — i.e., every named sub-case's guard is false simultaneously.
- **Decision path (gross margin):** falls to the defensive `ELSE`, error `602`
  (`'INVALID WS-COST-CONTRACT SCENARIO FOR GRSSMGN'`), fatal, `GO TO 0020-EXIT-PRICER`. The
  cost-plus method (`#2`) has the parallel error `603` for the identical shape of unreachable-guard
  failure.
- **Expected outcome:** pricing aborts entirely for this line, no sell price produced.
  **Whether this combination of conditions can actually occur with real data is unknown to this
  task** (`R-SELL-003` item 10/11's BLOCKED note) — it reads as a "this shouldn't happen" defensive
  branch, but defensive branches in 25+ year old COBOL sometimes do fire in production on data the
  original author didn't anticipate. Treat as a scenario to specifically ask the business/QA team
  about before assuming it is unreachable in the new system.
- **Confidence:** CONFIRMED for the COBOL mechanism; BLOCKED for real-world reachability.

### SCN-SELL-017 — HC (health-system) override supersedes whatever method #0–#8 computed
- **Given:** `OMGPR-F-HC-SELL-FLAG = 'Y'` (set upstream, per `program-inventory.md` §1.4's
  `HCOVD*` copybook family) — regardless of which of methods #0 through #8 fired above.
- **Decision path:** after the entire method #1–#8 sequence completes (whichever fired, if any),
  the unconditional `IF OMGPR-F-HC-SELL-FLAG = 'Y'` block runs (lines 10719–10736) → `IF
  HC-PROD-OVRD-TYPE = 'COST+'` → `OMGPR-A-CUS-UOM-SELL-PRC ← OMGPR-A-TOTAL-COST + (OMGPR-A-TOTAL-
  COST * HC-PROD-OVRD-PERCENT)`; `ELSE` → `9980-CVT-HC-STATED-PRC` then a UOM-converted
  `HC-PROD-OVRD-SELL-PRC`. Either way, `OMGPR-PRICING-METHOD ← 'NET DEL'`
  (net-delivered-price label), `OMGPR-Q-PREF-TIER-LEVEL ← HC-GROUP-ID`,
  `OMGPR-T-SELL-COMMENT ← HC-GROUP-COMMENT`.
- **Expected outcome:** **whatever the sell price was before this block (list-default, gross
  margin, suggested-sell, stated-price, doesn't matter) is completely replaced.** This is the true
  last-word rule for sell price on this line — any characterization test suite must check this flag
  *after* asserting a method #1–#8 result, since its presence changes the answer regardless of
  which method "should" have applied.
- **Confidence:** CONFIRMED. Citation: `R-SELL-003` item 5 last paragraph; `A6U01.CBL` 10719–10736.

### SCN-SELL-018 — Stated-price method, UOM conversion required
- **Given:** sell arrangement says stated-price; `WS-C-SELL-PROD-UM > SPACES`; the stated price's
  UOM differs from the order line's UOM (`OMGPR-C-ORD-LIN-CUST-UOM`).
- **Decision path:** method #8 guard true → `0160-CVT-STATED-PRC-010` (shared with the cost side's
  control-flow map — same UOM-conversion helper referenced in `contract-cost-method.md`'s map) →
  since UOMs differ, `7595-CVT-UM-010` actually runs (not the `NEXT SENTENCE` no-op) → `COMPUTE
  OMGPR-A-CUS-UOM-SELL-PRC ROUNDED = (WS-A-SELL-PROD-PRC * WS-CA-CONVERT-UP-UMF) /
  WS-CA-CONVERT-DOWN-UMF`.
- **Expected outcome:** `OMGPR-PRICING-METHOD = 'STATED PRC'`; the numeric result depends on
  `7595-CVT-UM-010`'s conversion-factor lookup, which — per both this document's and the cost
  document's blockers list — was never read in either task. **Any numeric test built from this
  scenario is blocked on that paragraph specifically.**
- **Confidence:** CONFIRMED for the control flow; BLOCKED for the numeric conversion itself.

---

## Coverage summary

| Rule | Scenarios |
|---|---|
| R-SELL-001 (source dispatch) | SCN-SELL-001–004 |
| R-SELL-002 (individual-contract cascade) | SCN-SELL-005–011 |
| R-SELL-003 (sell-method dispatch) | SCN-SELL-012–018 |

18 scenarios across 3 rules. As with the cost-side scenarios document, not exhaustive and not
combinatorial — each scenario isolates one branch/finding rather than stacking multiple
simultaneous edge conditions.

## Assumptions

Same three assumptions as `characterization-scenarios/contract-cost-method.md` apply here
(upstream validation already passed; un-read helper paragraphs are marked INFERRED not CONFIRMED
where a scenario depends on them; numeric conversions are stated as formulas, not examples) — not
repeated in full here.

## Blockers

1. `7075`/`7076` and `0185`/`0186` internal behavior — SCN-SELL-002/003 cannot go deeper than
   "this branch runs" until those paragraphs are read (T004's deferred scope, carried forward).
2. `7595-CVT-UM-010`'s actual conversion arithmetic (affects SCN-SELL-018 and the cost-side
   suggested-sell/stated-cost scenarios equally).
3. Real-world reachability of the `602`/`603` defensive error branches (SCN-SELL-016).
4. No DB2/CICS/COBOL execution environment — same standing blocker as the cost-side scenarios
   document.

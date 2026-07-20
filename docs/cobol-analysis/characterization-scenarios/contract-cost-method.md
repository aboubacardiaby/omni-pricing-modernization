# A6U01 — Contract Cost-Method Resolution & Dispatch: Characterization Scenarios (T003)

**Scope:** Characterization scenarios and expected decision paths for the same rule area covered by
`docs/cobol-analysis/decision-tables/contract-cost-method.md` (rules `R-COST-001`–`R-COST-005`).
Each scenario states a concrete combination of input conditions, the exact ordered sequence of
`A6U01.CBL` paragraphs COBOL will execute for that combination, and the qualitative expected effect
on output fields. These are meant to become the acceptance criteria for a Codex/C# implementation
of this rule area and, later, the basis for side-by-side (COBOL vs. C#) regression comparison.

**What this document is NOT:** a set of numeric golden-value test cases. This environment has no
DB2 access, no CICS region, and no COBOL compiler/interpreter — there is no way to load contract
rows and observe an actual `A6U01` run. Every scenario below is a **structural** characterization
(which paragraphs execute, in what order, and which fields get written from which source) derived
purely from reading the source, not from executing it. Numeric expectations (e.g., "the computed
suggested sell price") are stated only as formulas, never as concrete numbers. Anyone building a
real regression suite from this document still needs either sample DB2 rows or a mainframe test
run to turn these into pass/fail assertions with actual values.

**Source of truth:** `docs/cobol-analysis/decision-tables/contract-cost-method.md` (rule
definitions, cited as `R-COST-00x`) and `upload/A6U01.CBL` directly. Same confidence key as that
document: `CONFIRMED` / `INFERRED` / `BLOCKED`.

---

## How to read a scenario

- **Given:** the minimum set of data conditions needed to force this exact path. Conditions not
  mentioned are assumed "whatever would normally allow processing to reach this point" (e.g., valid
  division/account/vendor/product already passed upstream validation, per
  `program-inventory.md` §1.4's `7185-PRO-PASSED-DATA-010`).
- **Decision path:** ordered paragraph names, exactly as they would appear in a call-stack trace.
- **Rule(s) exercised:** cross-reference to `R-COST-00x` in the decision-table document.
- **Expected outcome:** which `OMGPR-*`/`WS-*` fields are set and from which source column —
  qualitative, not numeric, per the caveat above.
- **Confidence / citation:** as in prior documents.

---

## Group A — Cost-contract source priority (`R-COST-001`)

### SCN-COST-001 — Individual contract exists → group is never searched
- **Given:** exactly one active, non-excluded individual cost contract matches
  customer+vendor+product+date (`0235`'s single-row path, `SQLCODE = +0` on `9040`).
- **Decision path:** `0195-PRO-COST-CONT-010` → `0235-SEL-INDV-CNT-010` → `9037-SQL-ROW-CNT-010`
  (`SQLCODE = 0` or `100`, i.e. not `-811`) → `9040-SQL-SELECT-010` → `9045-SQL-SELECT-010` →
  `WS-F-COST-CONT-FND = 'Y'` → `0195` moves individual-contract fields → `7080-CVT-COST-CONT-010`
  → `7560-PRO-CONT-MOVES-010` → `7085-PRO-COST-DISC-010` → `GO TO 0195-EXIT`. **`0240` /
  `0275` / `0280` (group search) are never entered** — CONFIRMED by the unconditional `GO TO
  0195-EXIT` at line 4425.
- **Expected outcome:** `WS-F-CONTRACT-TYPE-SW = WS-INDIVIDUAL-IND`; `OMGPR-I-CNT-VEND` ←
  `CCG09-I-IND-VND-CNT-NBR`; `OMGPR-I-BUY-GROUP`/`OMGPR-S-BG-MEMBER` forced to zero/spaces
  (CONFIRMED, lines 4391–4393 — an individual contract never carries buy-group identity, even if
  the customer also belongs to a buy-group that happens to have its own contract for the same
  product); `OMGPR-A-CNT-LN-UNIT-COST` ← `CCG03-A-CNT-LN-UNIT-COST` (via `9040`/`9045`).
- **Confidence:** CONFIRMED. Citation: `R-COST-001` item 5(a); `A6U01.CBL` 4371–4426.

### SCN-COST-002 — No individual, group contract found at the first level tried
- **Given:** no individual contract row matches (`9040`/`9045` return `SQLCODE = 100`, or `9037`
  finds duplicates but all are excluded — see SCN-COST-008); an account-level product-category
  override (`CUG13`) exists for this account+vendor+category.
- **Decision path:** `0195` → `0235` (fails, `WS-COST-CONT-NOT-FND`) → `7795-FIND-WHICH-JOIN-010`
  → `0240-PRO-GRP-CNT-010` → `0275-PRO-ACCT-BG-010` → `0310-SEL-ACCT-PRD-CAT-010` (match) →
  `7855-SEL-ACCT-PRI-VER-010` → `7900-SEL-ACCT-PRI-010` → `WS-ACCT-BG-PRI-FND = 'Y'` →
  `GO TO 0275-EXIT` → `0240` sees `WS-COST-CONT-FND` → moves group-contract fields → `7080` →
  `7560` → `7085` → `GO TO 0195-EXIT`. **Levels 2–6 of `R-COST-003` are never reached.**
- **Expected outcome:** `WS-F-CONTRACT-TYPE-SW` set to priority-group or other-group indicator
  depending on `WS-C-COST-PRIORITY = +1`; `OMGPR-I-CNT-VEND` ← `CCG10-I-GRP-VND-CNT-NBR`;
  `OMGPR-I-BUY-GROUP` ← `CCG05-I-BUY-GROUP` (nonzero, unlike the individual-contract case);
  `OMGPR-A-CNT-LN-UNIT-COST` ← `CCG03-A-CNT-LN-UNIT-COST`.
- **Confidence:** CONFIRMED. Citation: `R-COST-003` item 5; `A6U01.CBL` 6495–6534.

### SCN-COST-003 — No individual, no group at any of the six levels → price-list fallback
- **Given:** no individual contract; all six `R-COST-003` levels fail to find a match.
- **Decision path:** `0195` → `0235` (fails) → `0240` → `0275` (all three sub-checks fail) →
  `0280` (all three sub-checks fail) → `0240-EXIT` with `WS-COST-CONT-FND` still `'N'` →
  `0195-EXIT` with `WS-COST-CONT-NOT-FND` → back in `7190-PRO-COST-AMTS-010`, the
  `IF WS-COST-CONT-NOT-FND` branch executes (lines 12333–12350).
- **Expected outcome:** `OMGPR-A-CNT-LN-UNIT-COST` ← `OMGPR-A-VND-PRC-DEALER` (already-resolved
  vendor price-list dealer cost, not re-derived here); `OMGPR-C-CNT-LN-UM` ← `OMGPR-C-VND-PRC-UM`;
  `OMGPR-F-JIT-EXEMPT = 'N'` and `OMGPR-F-FRT-EXEMPT = 'N'` **unconditionally**, regardless of any
  upstream exemption flag — CONFIRMED, this is a hard override, not a default. `7085` (the
  cost-method dispatch) is **never called** in this path — CONFIRMED by its absence from
  `7190`'s fallback branch; a line priced entirely off the price list has no
  `C_CNT_ENTRY_METHOD` concept at all.
- **Confidence:** CONFIRMED. Citation: `R-COST-001` item 9; `A6U01.CBL` 12322–12350.

---

## Group B — Individual-contract duplicate resolution (`R-COST-002`)

### SCN-COST-004 — Two individual contracts, no priority flag, different costs
- **Given:** `9037` returns `SQLCODE = -811` (duplicates exist); two `MIN_INDV` candidate rows for
  this customer+vendor+product, neither excluded, neither `CCG27-F-CNT-PRIORITY = 'Y'`, with unit
  costs C1 ≠ C2.
- **Decision path:** `0235` → `9037` (`-811`) → `0270-SEL-MIN-INDV-CNT-010` → `9065-SQL-OPEN-MIN-010`
  → loop: `9070-SQL-FETCH-FROM-MIN-010` → (not excluded) → `0272-COMPARE-COST` (no priority flag →
  the plain lowest-cost branch) → repeat for second row → `SQLCODE = +100` → loop ends →
  `WS-PRIORITY-I-CONTRACT` is zero (no priority-flagged candidate ever recorded) so the
  end-of-loop `IF WS-PRIORITY-I-CONTRACT > ZEROES` block is skipped → whichever of C1/C2 is lower
  (already written into `OMGPR-A-CNT-LN-UNIT-COST`/`OMGPR-I-CONTRACT` by `0272`) stands.
- **Expected outcome:** `OMGPR-I-CONTRACT` = the contract number of whichever candidate has the
  strictly lower `A_CNT_LN_UNIT_COST` (converted to the order UOM before comparison, per `7080`).
- **Confidence:** CONFIRMED. Citation: `R-COST-002` item 5.3; `A6U01.CBL` 6373–6467.

### SCN-COST-005 — Two individual contracts, one priority-flagged with a *higher* cost
- **Given:** same as SCN-COST-004, except candidate A is not priority-flagged with cost 5.00,
  candidate B **is** priority-flagged (`CCG27-F-CNT-PRIORITY = 'Y'`) with cost 8.00 (higher).
- **Decision path:** same loop structure as SCN-COST-004, but candidate B's fetch routes through
  `0273-CHECK-PRIORITY-COST` instead of the plain-cost branch of `0272`, recording it into
  `WS-PRIORITY-I-CONTRACT`/`WS-PRIORITY-LN-UNIT-COST` rather than `OMGPR-I-CONTRACT` directly. At
  loop end (`SQLCODE = 100`), since `WS-PRIORITY-I-CONTRACT > ZEROES`, the priority winner
  **overwrites** whatever the non-priority comparison had selected.
- **Expected outcome:** `OMGPR-I-CONTRACT` = candidate B (the priority-flagged one), **even though
  its cost (8.00) is higher than candidate A's (5.00)**. This is the single most important
  behavior to preserve exactly in any migration — a naive "always pick lowest cost" reimplementation
  would silently produce the wrong contract here.
- **Confidence:** CONFIRMED. Citation: `R-COST-002` item 5.2; `A6U01.CBL` 6390–6395, 6472–6485.

### SCN-COST-006 — Three individual contracts, two priority-flagged with different costs
- **Given:** candidate A (no flag, cost 3.00), candidate B (flagged, cost 8.00), candidate C
  (flagged, cost 6.00).
- **Decision path:** same as SCN-COST-005, but the priority pool now has two members (B, C); C
  replaces B in `WS-PRIORITY-I-CONTRACT`/`WS-PRIORITY-LN-UNIT-COST` when fetched (6.00 < 8.00, via
  `0273`'s own strictly-less-than comparison), regardless of fetch order between B and C.
- **Expected outcome:** `OMGPR-I-CONTRACT` = candidate C (lowest cost **within the priority pool**,
  not lowest cost overall — A's 3.00 is never even considered once any priority-flagged candidate
  exists).
- **Confidence:** CONFIRMED. Citation: `R-COST-002` item 5.2–5.3; `A6U01.CBL` 6474–6485.

### SCN-COST-007 — Two individual contracts, exact cost tie, no priority flags
- **Given:** candidates A and B, both cost 5.00, neither flagged, neither excluded.
- **Decision path:** same loop as SCN-COST-004; whichever of A/B is fetched **first** by `MIN_INDV`
  is selected (`WS-A-CNT-LN-UNIT-COST < OMGPR-A-CNT-LN-UNIT-COST` is false on the second fetch since
  5.00 is not `<` 5.00, and the zero-tie escape clause only applies when the running best is still
  at its unset zero state) — the second-fetched candidate is discarded even though its cost is
  identical.
- **Expected outcome:** `OMGPR-I-CONTRACT` = whichever of A/B `MIN_INDV` happens to fetch first.
  **This is a genuine non-determinism risk**: the cursor's `DECLARE` (lines 2342–2385) has no
  `ORDER BY`, so fetch order is whatever DB2's optimizer chooses for that query plan — it may not
  be stable across DB2 versions, statistics updates, or even repeated runs with unchanged data. A
  migrated implementation that instead sorts candidates by, say, contract number for determinism
  would be **a different, not necessarily wrong, but definitely different** result whenever a real
  tie occurs. Flag for explicit business sign-off before choosing a tie-break rule for the new
  system.
- **Confidence:** CONFIRMED for the COBOL mechanism. BLOCKED for what DB2 would actually fetch
  first on real data (needs a DB2 access path or execution-plan capture, per `R-COST-002` item 11).
  Citation: `R-COST-002` item 5.3.

### SCN-COST-008 — Duplicate individual contracts, all excluded → falls through to group search
- **Given:** `9037` returns `-811` (duplicates exist); every `MIN_INDV` candidate is on the
  account's contract-exclusion list (`OMGPR-F-CONT-EXCL-SW` set by `7360-VERIFY-FOR-EXCL` for each
  one).
- **Decision path:** `0270`'s fetch loop runs to `SQLCODE = 100` without ever executing `0272`'s
  cost-comparison branch (every candidate hits the `WS-COST-CONT-EXCLUDED → CONTINUE` path,
  line 6382–6383) → `WS-PRIORITY-I-CONTRACT` and `OMGPR-I-CONTRACT` both remain zero →
  `WS-F-COST-CONT-FND` is never set to `'Y'` by this path → `0235-EXIT` returns
  `WS-COST-CONT-NOT-FND` → `0195` proceeds to the group-contract search (`R-COST-003`), exactly as
  if no individual contract had existed at all.
- **Expected outcome:** identical decision path and outcome to SCN-COST-002/003 depending on
  whether a group contract is subsequently found — the individual-contract search leaves **no
  trace** in `OMGPR` fields when every candidate is excluded.
- **Confidence:** CONFIRMED. Citation: `R-COST-002` item 9; `A6U01.CBL` 6382–6384.

### SCN-COST-009 — Special-contract flag changes which bypass value is searched
- **Given:** `OMGPR-F-SPECIAL-CONTRACT = 'Y'` on input.
- **Decision path:** `0235-SEL-INDV-CNT-010`'s very first statement (line 5866–5868) moves `'B'`
  into `WS-WS-NOT-BYPASSED` before any SQL runs, changing the `F_BYPASS` predicate value used by
  every subsequent contract-search query in this rule area (`9037`, `9040`, `9045`, `MIN_INDV`,
  and — since `WS-WS-NOT-BYPASSED` is shared working storage, not reset for the group search —
  `MIN_GRP`/`MIN_GRP_CONT` as well, per their `WHERE ... F_BYPASS = :WS-WS-NOT-BYPASSED` predicate).
- **Decision path (not-found sub-case):** if no contract has `F_BYPASS = 'B'`, `9040`/`9045` return
  `SQLCODE = 100` and, because `OMGPR-F-SPECIAL-CONTRACT = 'Y'`, the program raises error `601`
  (`'#601-SPECIAL CONTRACT NOTFOUND FOR PRODUCT'`) and **abends immediately**
  (`GO TO 0020-EXIT-PRICER`, lines 5911–5918) — this is different from the normal not-found case
  (SCN-COST-003), which silently falls back to the price list. **A special-contract request that
  finds no bypass-flagged contract is a hard error, not a soft fallback.**
- **Expected outcome:** either a contract found under the `'B'`-bypass predicate (proceeds
  normally through the rest of this rule area), or a fatal `OMGPR-Q-ERROR-NBR = 601` /
  `OMGPR-F-PRICER-ERROR = 'Y'` with no price computed at all.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 5866–5868, 5910–5920.

---

## Group C — Group-contract search cascade (`R-COST-003`)

### SCN-COST-010 — Account-level match short-circuits before any customer-level check runs
- **Given:** no individual contract; account-vendor override (level 2) matches.
- **Decision path:** `0240` → `0275-PRO-ACCT-BG-010` → level 1 skipped (`OMGPR-S-PROD-CATEGORY =
  ZEROES`, gate at line 6495 not entered) → `0320-SEL-ACCT-VEND-BG-010` (match) → priority
  verification (`7855`) → `7900-SEL-ACCT-PRI-010` → `GO TO 0275-EXIT` → `0240` sees
  `WS-COST-CONT-FND` → **`0280-PRO-CUST-BG-010` (all of levels 4–6) is never entered.**
- **Expected outcome:** same field-population pattern as SCN-COST-002, sourced from `CUG12`
  (account-vendor override) instead of `CUG13`.
- **Confidence:** CONFIRMED. Citation: `R-COST-003` item 5; `A6U01.CBL` 5992, 6573–6613.

### SCN-COST-011 — All three account levels fail, customer product-category override matches
- **Given:** no individual contract; no account-level match at any of the three account sub-levels;
  a customer × product-category override (`CUG09`) exists.
- **Decision path:** `0240` → `0275` (all three sub-checks fail, falls through to `0275-EXIT`
  without a `GO TO`) → `0240`'s `IF WS-COST-CONT-NOT-FND` gate (line 5992) is true → `0280` →
  `0330-SEL-CUST-PRD-CAT-010` (match) → priority verification (`7865`) → `7905-SEL-CUST-PRI-010` →
  `GO TO 0280-EXIT`.
- **Expected outcome:** same pattern as SCN-COST-002, sourced from `CUG09`.
- **Confidence:** CONFIRMED. Citation: `R-COST-003` item 4 (level 4), item 5; `A6U01.CBL` 5992,
  6666–6705.

### SCN-COST-012 — Product category absent skips both product-category override levels
- **Given:** `OMGPR-S-PROD-CATEGORY = ZEROES` (no category resolved for this product — e.g., the
  upstream category lookup found nothing, see `program-inventory.md` §1.7's `A6O013U`/`A6O016U`
  chain, which can return no category row).
- **Decision path:** both `0275`'s level 1 (`0310-SEL-ACCT-PRD-CAT-010`) and `0280`'s level 4
  (`0330-SEL-CUST-PRD-CAT-010`) are skipped entirely — the `IF OMGPR-S-PROD-CATEGORY NOT = ZEROES`
  gates at lines 6495 and 6666 are both false. The cascade effectively becomes a 4-level search
  (acct-vendor → acct-BG-priority → cust-vendor → cust-BG-priority) for this product.
  **This is a real, data-dependent variation in how many levels actually execute — not a fixed
  6-step cascade for every product.**
- **Expected outcome:** whichever of the remaining 4 levels matches first, per normal cascade
  order; if none match, falls to price-list per SCN-COST-003.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 6495, 6666.

---

## Group D — Group-contract duplicate resolution (`R-COST-004`)

### SCN-COST-013 — Group-contract duplicates, priority flag on `CCG05` (not `CCG27`)
- **Given:** a buy-group match from `R-COST-003` has more than one eligible contract line; one
  candidate has `CCG05-F-GRP-CNT-PRIORITY = 'Y'` (note: fetched as `CCG05-F-CNT-PRIORITY` per the
  `MIN_GRP`/`MIN_GRP_CONT` cursor's `FETCH INTO` list) with a higher cost than a non-flagged
  candidate.
- **Decision path:** `0240` (post-match) → `0360-SEL-GRP-CNT-MIN-010` → `7795`-selected cursor
  (`MIN_GRP` or `MIN_GRP_CONT`, functionally equivalent per `R-COST-004` Note N-1) → fetch loop →
  same priority-pool-wins-over-cost pattern as SCN-COST-005, but reading `CCG05`'s flag instead of
  `CCG27`'s, and additionally calling `0362-VERIFY-ELIGIBILITY` per candidate (BLOCKED — internal
  logic not traced, see `R-COST-004` item 5).
- **Expected outcome:** the `CCG05`-priority-flagged candidate wins regardless of its relative cost
  — same qualitative behavior as SCN-COST-005, different source table.
- **Confidence:** INFERRED (the tie-break *shape* is CONFIRMED by direct code parallel to
  `0272`/`0273`; the `0362-VERIFY-ELIGIBILITY` interaction is BLOCKED, so this scenario cannot rule
  out an additional filter this task didn't see). Citation: `R-COST-004` item 5; `A6U01.CBL`
  8628–8651.

### SCN-COST-014 — 7795's row-count heuristic picks a different cursor for the same logical query
- **Given:** two customers/products where, for one, `CCG03`'s candidate-row count is lower than
  `CCG10`'s, and for the other, the reverse is true (or either is zero).
- **Decision path:** `7795-FIND-WHICH-JOIN-010` sets `WS-CNT-LINE-OUTER-SW` differently per case
  (`'Y'` vs. `'S'`/unset), causing `0360` to open/fetch/close `MIN_GRP_CONT` in one case and
  `MIN_GRP` in the other.
- **Expected outcome:** **identical** candidate set and identical tie-break result in both cases —
  this scenario exists specifically to document that the choice of cursor is *not* expected to be
  an observable behavior difference. If a characterization test built from this scenario ever
  observes a different winning contract depending solely on which cursor was chosen, that would
  indicate the two cursors are **not** actually row-set-equivalent (contradicting the INFERRED
  assumption in `R-COST-004` item 11) and should be escalated immediately, not silently accepted.
- **Confidence:** INFERRED (this is a "should produce the same result" scenario, not a
  CONFIRMED-identical one — see `R-COST-004`'s explicit caveat about not having proven
  set-equivalence).

---

## Group E — Cost-method dispatch (`R-COST-005`)

### SCN-COST-015 — Code `01`, stated cost: no suggested-sell lookup at all
- **Given:** resolved contract's `C_CNT_ENTRY_METHOD = '01'`.
- **Decision path:** `7085-PRO-COST-DISC-010` → `EVALUATE` falls to `WHEN OTHER` (since `01` is not
  one of the five explicitly coded `WHEN`s) → `CONTINUE` → `7085-EXIT`. No `CCG13/14/15/16/21`
  table is ever touched for this line.
- **Expected outcome:** `WS-F-COST-SUGGESTED-SELL` is **not** set to `'Y'` (remains whatever it was
  initialized to); `OMGPR-A-CNT-LN-SUGG-SELL` is **not** computed by this paragraph. The line's
  cost is whatever `CCG03-A-CNT-LN-UNIT-COST` already held from the resolving select (`9040`/
  `9045`/`MIN_INDV`/`MIN_GRP`/`MIN_GRP_CONT`) — i.e., code `01`'s "no extra table to read" comment
  (line 10105–10108) is accurate: the stated cost was already fetched as part of the base contract
  row.
- **Confidence:** CONFIRMED. Citation: `R-COST-005` item 4 row `01`; `A6U01.CBL` 10162–10164.

### SCN-COST-016 — Code `02`, suggested sell with brokerage: `CCG13` lookup, found
- **Given:** resolved contract's `C_CNT_ENTRY_METHOD = '02'`; a matching `CCG13` row exists for
  `(I_CONTRACT, L_CNT_LINE)`.
- **Decision path:** `7085` → `WHEN '02'` → `WS-F-COST-SUGGESTED-SELL = 'Y'` →
  `0135-SEL-SUGG-BROKERAG-010` → `9005-SQL-SELECT-010` (`SQLCODE = 0`) →
  `7175-CVT-SUGG-010` (UOM conversion, only does work if `CCG03-C-CNT-LN-UM ≠
  OMGPR-C-ORD-LIN-CUST-UOM`) → `COMPUTE OMGPR-A-CNT-LN-SUGG-SELL ROUNDED = ((CCG13-A-CNT-LN-
  SUGG-SELL * WS-CA-CONVERT-UP-UMF) / WS-CA-CONVERT-DOWN-UMF)` → `OMGPR-P-CNT-LN-BROKER` ←
  `CCG13-P-CNT-LN-BROKER` → `GO TO 7085-EXIT`.
- **Expected outcome:** `OMGPR-A-CNT-LN-SUGG-SELL` = `CCG13-A-CNT-LN-SUGG-SELL` unchanged if UOMs
  match (conversion factors both `1`), or proportionally converted otherwise; `OMGPR-P-CNT-LN-
  BROKER` set; `WS-F-COST-SUGGESTED-SELL = 'Y'`.
- **Confidence:** CONFIRMED. Citation: `R-COST-005` item 4 row `02`, item 6; `A6U01.CBL`
  2642–2678, 20241–20256.

### SCN-COST-017 — Code `02`, but no matching `CCG13` row → fatal error, not a soft fallback
- **Given:** resolved contract's `C_CNT_ENTRY_METHOD = '02'`; no `CCG13` row exists for
  `(I_CONTRACT, L_CNT_LINE)` (`SQLCODE = 100`).
- **Decision path:** `0135-SEL-SUGG-BROKERAG-010` → `9005-SQL-SELECT-010` (`SQLCODE = 100`) →
  `EVALUATE SQLCODE`'s `WHEN OTHER` branch fires (there is **no** `WHEN +100` case) → error `63`,
  `OMGPR-Q-ERROR-CODE = 70` (fatal) → `MOVE WS-YES-IND TO OMGPR-F-PRICER-ERROR` →
  `GO TO 0020-EXIT-PRICER`. **Pricing aborts entirely for this line; no cost or sell price is
  produced.**
- **Expected outcome:** `OMGPR-F-PRICER-ERROR = 'Y'`, `OMGPR-Q-ERROR-NBR = 63`,
  `OMGPR-ERROR-MESSAGE` = formatted `WS-DB-OPERATION-MSG` referencing `CCG13`/`SELECT`/the raw
  `SQLCODE`. This is the scenario most likely to be under-tested in a straightforward reading of
  the "happy path" — a contract header promising method `02` with no matching `CCG13` detail row
  is a **data-integrity failure that the legacy system treats as fatal**, and any migration must
  reproduce that fatality rather than silently falling back to stated cost.
- **Confidence:** CONFIRMED. Citation: `R-COST-005` item 10; `A6U01.CBL` 2650, 2659–2671.

### SCN-COST-018 — Codes `03`/`04`/`05`/`08`: same shape as `02`, different table/fields
- **Given:** analogous to SCN-COST-016/017 for each of `03` (`CCG14`), `04` (`CCG15`), `05`
  (`CCG16`), `08` (`CCG21`).
- **Decision path / outcome:** identical control-flow shape to SCN-COST-016 (found) and
  SCN-COST-017 (not-found → fatal), substituting the table and paragraph per `R-COST-005`'s
  dispatch table. Notable field differences: code `04` zeroes `OMGPR-P-CNT-LN-BROKER` instead of
  setting a percent field (CONFIRMED, line 2733 — `CCG15` carries no broker/discount percent of its
  own); code `08` moves no secondary percent field at all (CONFIRMED by its absence, lines
  2801–2807 — a fixed rebate has no percentage to carry).
- **Confidence:** CONFIRMED. Citation: `R-COST-005` item 4; `A6U01.CBL` 2680–2827.

### SCN-COST-019 — Code `06` or `07`: no lookup, same as code `01`
- **Given:** resolved contract's `C_CNT_ENTRY_METHOD = '06'` (fixed rebate amount) or `'07'`
  (cost-discount percent from price list).
- **Decision path / outcome:** identical to SCN-COST-015 — both fall to `WHEN OTHER` in `7085`,
  `CONTINUE`, no table lookup, `WS-F-COST-SUGGESTED-SELL` untouched.
- **Confidence:** CONFIRMED. Citation: `R-COST-005` item 4 rows `06`/`07`.

### SCN-COST-020 — Unrecognized code (e.g., blank, `'09'`, or any value outside `01`–`08`)
- **Given:** resolved contract's `C_CNT_ENTRY_METHOD` holds a value the `EVALUATE` doesn't
  explicitly test — e.g. spaces (uninitialized/corrupt data) or a new code added to the contract
  table but never added to this `EVALUATE`.
- **Decision path:** `7085` → `WHEN OTHER` → `CONTINUE` → `7085-EXIT`. **No error is raised.**
- **Expected outcome:** identical to SCN-COST-015/019 — the line silently proceeds with whatever
  cost was already in `CCG03-A-CNT-LN-UNIT-COST`, no suggested-sell data, no diagnostic. This is
  flagged in `R-COST-005` item 9 as a genuine risk: a data-entry error in the contract's entry-method
  column, or a future 9th code introduced on the mainframe side without a corresponding COBOL
  change, produces **no observable symptom** at this layer — it would only surface downstream (or
  not at all) as a line silently priced as if it were a stated-cost contract. **This scenario
  exists specifically so a migration decision can be made deliberately**: should the C# port
  reproduce this silent fallthrough exactly (for parity), or should it raise a diagnostic (a
  behavior improvement requiring explicit business sign-off, since CLAUDE.md/AGENTS.md both
  require COBOL behavior to be the source of truth unless a documented decision changes it)?
- **Confidence:** CONFIRMED for what COBOL does. This is a decision point for the business, not
  something this task can resolve unilaterally.

---

## Coverage summary

| Rule | Scenarios |
|---|---|
| R-COST-001 (source priority) | SCN-COST-001, 002, 003 |
| R-COST-002 (individual duplicate resolution) | SCN-COST-004–009 |
| R-COST-003 (group search cascade) | SCN-COST-002, 010, 011, 012 |
| R-COST-004 (group duplicate resolution) | SCN-COST-013, 014 |
| R-COST-005 (cost-method dispatch) | SCN-COST-015–020 |

20 scenarios across 5 rules. Not exhaustive — in particular, no scenario combines *multiple*
simultaneous edge conditions (e.g., special-contract flag **and** a group-level duplicate tie
**and** an unrecognized entry-method code all at once); combinatorial scenarios were judged lower
value than covering each rule's individual branches at least once, given this task's scope.

## Assumptions

1. "Upstream validation already passed" (division/account/vendor/product present, per
   `program-inventory.md` §1.4) is assumed true for every scenario unless stated otherwise — none
   of these scenarios re-tests that earlier validation.
2. Where a scenario depends on a paragraph this task did not fully read (`0362-VERIFY-ELIGIBILITY`,
   `7360-VERIFY-FOR-EXCL`, `7560-PRO-CONT-MOVES-010`), the scenario is marked INFERRED rather than
   CONFIRMED and the gap is called out explicitly, per the same standard set in the decision-table
   document.
3. Numeric UOM-conversion outcomes are described only as formulas (`(<field> * WS-CA-CONVERT-UP-UMF)
   / WS-CA-CONVERT-DOWN-UMF`), never as example numbers — `7595-CVT-UM-010`, the actual conversion
   worker, was not read in this task.

## Blockers (in addition to those already listed in the decision-table document)

1. No DB2/CICS/COBOL execution environment available — every scenario above is structural
   (path + field provenance), not a numeric golden-value test. Turning these into an executable
   regression suite requires either sample data extracted from the real DB2 tables or a mainframe
   test run.
2. `7595-CVT-UM-010` (the shared UOM-conversion worker used by every suggested-sell paragraph and
   by `7080-CVT-COST-CONT-010`) was referenced but not read — its rounding/precision behavior is
   unconfirmed and would materially affect any numeric test derived from SCN-COST-016/018.

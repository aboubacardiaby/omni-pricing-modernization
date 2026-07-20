# A6U01 — Contract Cost-Method Resolution & Dispatch (T002)

**Scope:** Decision-table extraction for the "contract cost-method dispatch" rule area in
`A6U01.CBL`: how the pricer selects *which contract* (individual vs. group), *which row* when
duplicates exist, and *which of the 8 cost-entry methods* to apply — paragraphs `0195`, `0235`,
`0240`, `0270`–`0280`, `7085`, `0135`–`0180`, and the `MIN_INDV`/`MIN_GRP`/`MIN_GRP_CONT` cursors
flagged in `docs/cobol-analysis/program-inventory.md` §4.3.
**Not in scope:** dealer/acquisition cost (`7065-PRO-DEAL-COST-010`), sell-side arrangement
resolution (`0180`'s *sibling* sell-arrangement chain at `7070`/`7076`, which is a different,
larger cascade despite similar paragraph-numbering proximity — do not confuse the two), JIT/PANDAC/
freight/surcharge adjustments, and everything downstream of a resolved unit cost. Those are
candidate scopes for a follow-up decision-table task.
**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`. No DCLGEN copybook for any table
referenced here (`CCG01/03/04/05/06/07/09/10/11/13/14/15/16/21/27`, `CUG06/07/08/09/10/11/12/13`,
`BGG01/02/03`) is supplied in `upload/` — every column named below is confirmed only at the
SQL-text level (visible in the `SELECT`/`WHERE` clauses actually read), not from a full table
definition. Per CLAUDE.md, no omitted column or omitted table behavior is asserted as fact.
**Confidence key:** `CONFIRMED` = directly read in the supplied source and traced to its actual
effect. `INFERRED` = a reasonable reading (e.g., inferred equivalence between two SQL variants)
that was not exhaustively proven. `BLOCKED` = cannot be determined from supplied files.

---

## Control-flow map (CONFIRMED)

```text
0030-PROCESS-COST
  └─ 7190-PRO-COST-AMTS-010
       ├─ 7065-PRO-DEAL-COST-010            (OUT OF SCOPE — dealer/acq cost baseline)
       ├─ 0195-PRO-COST-CONT-010
       │    ├─ 0235-SEL-INDV-CNT-010        (search INDIVIDUAL cost contract)
       │    │    ├─ [dup?] 0270-SEL-MIN-INDV-CNT-010  (MIN_INDV cursor, lowest-cost tie-break)
       │    │    └─ found → 7080-CVT-COST-CONT-010 → 7560-PRO-CONT-MOVES-010
       │    │               → 7085-PRO-COST-DISC-010 (cost-method dispatch) → 0195-EXIT
       │    └─ [not found] → 7795-FIND-WHICH-JOIN-010 (query-plan choice only, see Note N-1)
       │                    → 0240-PRO-GRP-CNT-010     (search GROUP cost contract)
       │                         ├─ 0275-PRO-ACCT-BG-010  (account-level, 3 sub-checks)
       │                         ├─ 0280-PRO-CUST-BG-010  (customer-level, 3 sub-checks)
       │                         └─ [BG priority match found] 0360-SEL-GRP-CNT-MIN-010
       │                              (MIN_GRP / MIN_GRP_CONT cursor, lowest-cost tie-break)
       │                    → found → 7080-CVT-COST-CONT-010 → 7560-PRO-CONT-MOVES-010
       │                               → 7085-PRO-COST-DISC-010 → 0195-EXIT
       └─ [WS-COST-CONT-NOT-FND after 0195] → fall back to OMGPR-A-VND-PRC-DEALER (price-list cost)

7085-PRO-COST-DISC-010  (EVALUATE CCG01-C-CNT-ENTRY-METHOD)
  01 → (no sub-paragraph; uses CCG01/CCG03 fields already fetched)
  02 → 0135-SEL-SUGG-BROKERAG-010   (CCG13)
  03 → 0140-SEL-SUGG-COST-PLU-010   (CCG14)
  04 → 0145-SEL-SUGG-STATED-010     (CCG15)
  05 → 0150-SEL-SUGG-COST-DIS-010   (CCG16)
  06 → (no sub-paragraph)
  07 → (no sub-paragraph)
  08 → 0155-SEL-SUGG-FIXED-RE-010   (CCG21)
```

---

## Rule R-COST-001 — Cost-contract source priority

1. **Rule ID / name:** R-COST-001 — Individual contract beats group contract beats price-list fallback.
2. **Program / paragraph:** `A6U01`, `0195-PRO-COST-CONT-010` (lines 4366–4495), fallback in
   `7190-PRO-COST-AMTS-010` (lines 12322–12350).
3. **Preconditions:** Always evaluated once per priced line (unless the line is sell-only or
   JIT-on-cost, per the dispatch already documented in `program-inventory.md` §1.4).
4. **Data/SQL dependencies:** `0235-SEL-INDV-CNT-010` (individual search, see R-COST-002) and
   `0240-PRO-GRP-CNT-010` (group search, see R-COST-003) as gates; fallback reads
   `OMGPR-A-VND-PRC-DEALER` / `OMGPR-C-VND-PRC-UM` (already-resolved vendor price-list fields, set
   earlier in the program — not re-derived here).
5. **Priority relative to competing rules:** This *is* the top-level priority rule for cost source.
   CONFIRMED order: (a) individual contract, checked first, unconditionally short-circuits via
   `GO TO 0195-EXIT` if found (line 4425) — group is **never even searched** when an individual
   contract exists; (b) group contract, searched only if (a) fails; (c) vendor price-list dealer
   cost, used only if both (a) and (b) fail (`WS-COST-CONT-NOT-FND`, lines 12333–12350).
6. **Calculation and rounding stage:** N/A at this level — this rule selects *which* cost row
   feeds `OMGPR-A-CNT-LN-UNIT-COST` (or falls back to `OMGPR-A-VND-PRC-DEALER`); actual
   computation happens in R-COST-004/§7085 and downstream (`0030-PROCESS-COST`'s later paragraphs,
   out of scope).
7. **Output fields:** `WS-F-CONTRACT-TYPE-SW` (individual vs. group indicator), `OMGPR-I-CNT-VEND`,
   `OMGPR-I-GRP-CONTROL-NBR`, and (fallback only) `OMGPR-A-CNT-LN-UNIT-COST` ←
   `OMGPR-A-VND-PRC-DEALER`, `OMGPR-C-CNT-LN-UM` ← `OMGPR-C-VND-PRC-UM`, plus
   `OMGPR-F-JIT-EXEMPT` / `OMGPR-F-FRT-EXEMPT` forced to `'N'` in the fallback case only
   (CONFIRMED, lines 12348–12349 — i.e., when there is no cost contract at all, a line is *never*
   JIT- or freight-exempt regardless of any other flag, because there is no contract line to carry
   an exemption flag).
8. **Effective/expiration dates contributed:** Individual path adds `CCG01-D-CNT-PROT-END` always,
   plus `CCG03-D-CNT-LINE-EXPIRE` and `CCG06-D-ASGN-EXPIRE` when their null-indicator is not `-1`
   (i.e., not SQL-NULL) — CONFIRMED lines 4406–4424. Group path adds the same `CCG01`/`CCG03` dates
   plus `CCG05-F-GRP-CNT-ELIG-LIS`-gated `CCG07-D-ELIG-EXPIRE` and `BGG02-D-BGM-END-MEM` — CONFIRMED
   lines 4460–4488. All feed a shared "closest expiration wins" array via
   `7695-ADD-EXP-DATE-ARRA-010` (not itself traced in this pass — BLOCKED for its internal
   comparison logic, flagged for follow-up).
9. **Exclusions and fallbacks:** The price-list fallback (item 5c) IS the exclusion/fallback path
   for this rule; it is unconditional whenever no contract (individual or group) is found — there
   is no further tier below it.
10. **Errors:** None raised directly by this priority rule; errors belong to the underlying
    individual/group search rules (R-COST-002/R-COST-003).
11. **Confidence:** CONFIRMED.

---

## Rule R-COST-002 — Individual-contract duplicate resolution

1. **Rule ID / name:** R-COST-002 — When a product matches more than one active individual cost
   contract for the same customer, resolve to exactly one via exclusion filter → priority flag →
   lowest cost.
2. **Program / paragraph:** `A6U01`, `0235-SEL-INDV-CNT-010` (lines 5833–5971, duplicate-count
   check `9037-SQL-ROW-CNT-010` lines 20385–20453) and `0270-SEL-MIN-INDV-CNT-010` /
   `0272-COMPARE-COST` / `0273-CHECK-PRIORITY-COST` (lines 6313–6488).
3. **Preconditions:** Only invoked when `9037-SQL-ROW-CNT-010`'s `EXISTS`-based duplicate probe
   against `CCG01`×`CCG04`×`CCG06`×`CCG09` returns `SQLCODE = -811` ("more than one row") —
   CONFIRMED lines 20429–20435 (`WHEN -811 → MOVE 'Y' TO WS-DUP-CONT-EXISTS-SW`), a deliberate
   reuse of the DB2 "ambiguous cursor/singleton SELECT returned >1 row" error code as a business
   signal rather than a fatal error (same non-standard pattern already flagged for `CUP100` in
   `program-inventory.md` §1.5). If `SQLCODE = 0` or `100`, exactly zero or one contract exists and
   the simpler `9040-SQL-SELECT-010` path is used instead (not this rule).
4. **Data/SQL dependencies:** Fetch loop over cursor `MIN_INDV` (declared `A6U01.CBL` lines
   2342–2385; joins `CCG01`,`CCG03`,`CCG04`,`CCG06`,`CCG09`,`CC_CNT_PRT_FLG CCG27`, keyed by
   `CCG06.I_CUSTOMER = :OMGPR-I-CUSTOMER`, `CCG01.I_VENDOR = :OMGPR-I-PARENT-VENDOR`,
   `CCG03.I_VENDOR = :OMGPR-I-VENDOR`, `CCG03.I_VND_PRODUCT = :OMGPR-I-VND-PRODUCT`,
   `CCG01.F_BYPASS = :WS-WS-NOT-BYPASSED`, plus every date-range predicate active
   `<= :WS-CURRENT-DB2-DATE <=` on `CCG01`/`CCG03`/`CCG06`/`CCG09`/`CCG27`). Fetched columns:
   `CCG01.I_CONTRACT`, `CCG03.C_CNT_LN_UM`, `CCG03.A_CNT_LN_UNIT_COST`, `CCG03.F_JIT_EXEMPT`,
   `CCG03.F_FRT_EXEMPT`, `CCG27.F_CNT_PRIORITY`.
5. **Priority relative to competing rules:** Runs *inside* R-COST-001's individual-contract branch.
   Internal tie-break order (all CONFIRMED, `0272-COMPARE-COST`/`0273-CHECK-PRIORITY-COST`, lines
   6442–6488):
   1. **Exclusion filter first:** each fetched candidate is checked via `7360-VERIFY-FOR-EXCL`
      (only if `OMGPR-ACT-CNT-EXCL-ABSENT` is false, i.e. the account has at least one contract
      exclusion on file — see `program-inventory.md` §1.5's `1000-VERFIFY-CONTRACT-EXCL` for how
      that flag gets set upstream in `CUP100`); an excluded candidate is skipped entirely
      (`CONTINUE`, line 6383/8638) — it never competes on cost.
   2. **Priority flag beats cost:** if the surviving candidate has `CCG27-F-CNT-PRIORITY = 'Y'`,
      it is compared only against other priority-flagged candidates via a *separate* running-best
      pair (`WS-PRIORITY-I-CONTRACT`/`WS-PRIORITY-LN-UNIT-COST`, `0273-CHECK-PRIORITY-COST`) —
      this separate pool is checked and, if non-empty, its winner **overwrites** whatever the
      non-priority pool selected, at the very end of the fetch loop (`WHEN +100`, lines
      6390–6395) — i.e., a priority-flagged contract always wins over a non-priority one,
      regardless of relative cost.
   3. **Lowest cost wins within the same pool** (priority or non-priority): a candidate replaces
      the current running best only if its UOM-converted unit cost (`WS-A-CNT-LN-UNIT-COST`,
      converted via `7080-CVT-COST-CONT-010` first) is strictly less than the current best, OR the
      current best is still at its unset zero state (first candidate ever considered) — CONFIRMED
      lines 6456–6466. **Tie-break: first-fetched-wins** — a later candidate with a cost exactly
      equal to the current best (including an exact zero/zero tie) never replaces it, per the
      explicit guard and its comment ("...WE DO NOT WANT TO OVERRIDE IT WITH ANOTHER CONTRACT").
      Cursor fetch order is therefore itself a de facto tie-break rule; this document does not
      trace `MIN_INDV`'s `ORDER BY` (not visible in the cursor `DECLARE`, lines 2342–2385 — no
      `ORDER BY` clause present, so fetch order is **whatever DB2's optimizer chooses** — flagged
      as a genuine non-determinism risk for any migration that must reproduce exact legacy
      tie-breaking; BLOCKED without a DB2 access path or execution-plan capture).
6. **Calculation and rounding stage:** UOM conversion (`7080-CVT-COST-CONT-010`) happens once per
   candidate, before the cost comparison — i.e., candidates are compared in the *ordering* UOM, not
   the contract's native UOM. No rounding is applied at this stage (rounding happens later, in
   `7715-PRO-ROUNDING-010`, out of scope).
7. **Output fields:** `OMGPR-I-CONTRACT`, `OMGPR-A-CNT-LN-UNIT-COST` (both set from whichever
   candidate wins), `WS-DUP-CONT-SELECTED-SW`.
8. **Effective/expiration dates contributed:** None directly in this comparison loop (dates are
   added by the caller, `0195`, per R-COST-001 item 8, using the *final* winning contract's row).
9. **Exclusions and fallbacks:** The exclusion filter (item 5.1) is itself the primary
   exclusion mechanism; there is no further fallback within this rule — if every duplicate is
   excluded, no contract is selected and control returns to R-COST-001 to fall through to the
   group-contract search.
10. **Errors:** `9037` SQL failure (any `SQLCODE` other than `0`/`100`/`-811`) → error 888,
    `OMGPR-Q-ERROR-CODE = 70` (fatal), abends via `GO TO 0020-EXIT-PRICER` (line 20441–20448).
    `0270`'s cursor `OPEN`/`FETCH`/`CLOSE` failures → errors 41/42/43 respectively (lines 6354,
    6403, 6427), all fatal, same abend path.
11. **Confidence:** CONFIRMED for the control flow and tie-break logic as written. INFERRED that
    "no `ORDER BY`" genuinely means DB2-optimizer-dependent fetch order (a fair reading of the
    cursor `DECLARE`, but not something this task can execute/verify).

---

## Rule R-COST-003 — Group-contract candidate search cascade

1. **Rule ID / name:** R-COST-003 — Group cost contract is searched through six ordered override
   levels; first match wins.
2. **Program / paragraph:** `A6U01`, `0240-PRO-GRP-CNT-010` (lines 5973–6019, header comment lines
   5977–5987), `0275-PRO-ACCT-BG-010` (lines 6490–6659), `0280-PRO-CUST-BG-010` (lines 6661–6824+).
3. **Preconditions:** Only reached when R-COST-002's individual-contract search returns
   `WS-COST-CONT-NOT-FND` (`0195`, line 4428 area).
4. **Data/SQL dependencies (per level, all BLOCKED for full DCLGEN, SQL-text-confirmed only):**
   1. `0310-SEL-ACCT-PRD-CAT-010` — account × product-category override (`CUG13`), gated on
      `OMGPR-S-PROD-CATEGORY NOT = ZEROES`.
   2. `0320-SEL-ACCT-VEND-BG-010` — account × vendor override (`CUG12`).
   3. Account buy-group priority walk — `7855-SEL-ACCT-PRI-VER-010` (verify a priority list
      exists) → `0315-SEL-ALL-ACCT-PRI-010` (walk it), backed by `CUG10`/`CUG11`.
   4. `0330-SEL-CUST-PRD-CAT-010` — customer × product-category override (`CUG09`), same
      product-category gate as level 1.
   5. `0335-CUST-VEND-BG-010` — customer × vendor override (`CUG08`).
   6. Customer buy-group priority walk — `7865-SEL-CUST-PRI-VER-010` → (customer analogue of
      `0315`), backed by `CUG06`/`CUG07`.
5. **Priority relative to competing rules:** Sequential, first-match-wins, CONFIRMED by the
   explicit `GO TO 0275-EXIT` / `GO TO 0280-EXIT` after each successful level (e.g. lines 6534,
   6613, 6705, 6784) — every level after a match is skipped entirely for that price request.
   **Account-level (`0275`) always runs before customer-level (`0280`)** — CONFIRMED, `0240`
   performs `0275` unconditionally then `0280` only `IF WS-COST-CONT-NOT-FND` (line 5992). Within
   each of account/customer, the three sub-levels (prod-cat override → vendor override → BG
   priority walk) are likewise strictly sequential with the same short-circuit pattern.
6. **Calculation and rounding stage:** N/A — this rule selects the candidate row(s), computation
   happens after via R-COST-004/`7085`.
7. **Output fields:** `WS-F-CONTRACT-TYPE-SW` (which level matched — priority-group vs.
   other-group, per `WS-C-COST-PRIORITY = +1` check at each level, e.g. lines 6505–6511),
   `WS-F-COST-BG-LEVEL` (always `WS-CHILD-IND` at override levels — CONFIRMED, no parent/grandparent
   walk is performed for *cost* group contracts, unlike the sell-arrangement search elsewhere in
   the program which does walk parent/grandparent groups — BLOCKED/OUT-OF-SCOPE detail, flagged so
   it is not assumed the two cascades are identical), `WS-F-COST-OVERRIDE`.
8. **Effective/expiration dates contributed:** Each override level adds its own override-expiry
   date plus the corresponding priority-expiry date when not SQL-NULL (e.g. lines 6518–6533 for
   level 1) — same "closest expiration wins" array mechanism as R-COST-001.
9. **Exclusions and fallbacks:** No exclusion filter is applied at the *candidate-search* stage
   here (unlike R-COST-002/R-COST-004, which apply `7360-VERIFY-FOR-EXCL` per duplicate row) —
   exclusion is applied later, at the duplicate-resolution stage (R-COST-004), once a contract
   number is actually being evaluated for the minimum-cost comparison. If all six levels fail to
   find a matching buy-group/contract, `0240` returns with `WS-COST-CONT-NOT-FND` still true, and
   R-COST-001's price-list fallback applies.
10. **Errors:** Each level's `EVALUATE SQLCODE ... WHEN OTHER` branch raises its own DB-error
    number (58, 67, 68, 97, 98, 99, 100, 101 — one per `CUG10`/`CUG11`/`CUG06`/`CUG07` SELECT
    failure across the six levels, CONFIRMED at each site) — all fatal, `GO TO 0020-EXIT-PRICER`.
11. **Confidence:** CONFIRMED for the six-level order and short-circuit behavior. The internal
    priority-walk paragraphs (`0315-SEL-ALL-ACCT-PRI-010`, its customer analogue, and the
    `7855`/`7865` "verify a priority list exists" gates) were not fully read in this pass — their
    own internal ordering (e.g., "process level 1 first, then continue thru higher levels," per the
    `0240` header comment) is reported as CONFIRMED-from-comment only, not CONFIRMED-from-logic;
    flagged for follow-up if this rule area is extended.

---

## Rule R-COST-004 — Group-contract duplicate resolution

1. **Rule ID / name:** R-COST-004 — Same exclusion → priority-flag → lowest-cost tie-break as
   R-COST-002, applied to group contracts, via one of two functionally-equivalent cursors.
2. **Program / paragraph:** `A6U01`, `0360-SEL-GRP-CNT-MIN-010` (lines 8564–8700+), gated by
   `7795-FIND-WHICH-JOIN-010` (lines 18444–18513).
3. **Preconditions:** Invoked once a buy-group has been identified as a match by R-COST-003 (any
   of its six levels) and more than one contract line is a candidate for that buy-group/product.
4. **Data/SQL dependencies:** Two cursors with equivalent join semantics, chosen for query-plan
   reasons only (see Note N-1): `MIN_GRP` (lines 2261–2297, explicit join to `CCG10`) and
   `MIN_GRP_CONT` (lines 2299–2339, `EXISTS`-subquery against `CCG10`, added under change-tag
   `DP0620`). Both join `CCG01`,`CCG03`,`CCG05`, keyed by `:CCG05-I-BUY-GROUP`,
   `:OMGPR-I-PARENT-VENDOR`/`:OMGPR-I-VENDOR`, `:OMGPR-I-VND-PRODUCT`, `:WS-WS-CNT-TYPE`,
   `:WS-WS-NOT-BYPASSED`, and the same style of active-date-range predicates as `MIN_INDV`. Fetched
   columns: `CCG01.I_CONTRACT`, `CCG03.C_CNT_LN_UM`, `CCG03.A_CNT_LN_UNIT_COST`,
   `CCG05.F_GRP_CNT_ELIG_LIS`, `CCG05.F_CNT_PRIORITY` — note this cursor pulls its priority flag
   from `CCG05`, not `CCG27` (contrast with `MIN_INDV`, R-COST-002, which uses `CCG27`) — CONFIRMED
   difference in priority-flag source table between the individual and group paths.
5. **Priority relative to competing rules:** Identical three-step tie-break as R-COST-002 (exclusion
   → priority-pool-wins → lowest-cost-with-first-wins-on-tie), CONFIRMED by near-identical code at
   lines 8630–8651 vs. `0272`/`0273`'s lines 6375–6467 (the group path additionally calls
   `0362-VERIFY-ELIGIBILITY`, line 8641, which the individual path does not — BLOCKED/not traced in
   this pass, flagged for follow-up: this likely checks `CCG05-F-GRP-CNT-ELIG-LIS` against an
   eligible-customer list, given the fetched column name, but that is INFERRED from naming, not
   confirmed from the paragraph body).
6. **Calculation and rounding stage:** Same as R-COST-002 (UOM conversion before comparison, no
   rounding at this stage).
7. **Output fields:** Same as R-COST-002: `OMGPR-I-CONTRACT`, `OMGPR-A-CNT-LN-UNIT-COST`.
8. **Effective/expiration dates contributed:** None directly in this loop (added by `0195`'s group
   branch per R-COST-001 item 8).
9. **Exclusions and fallbacks:** Same exclusion mechanism as R-COST-002 (`7360-VERIFY-FOR-EXCL`).
10. **Errors:** Cursor open/close failures → errors 27/29 (lines 8606, 8688-ish), fetch failure →
    error 28 — all fatal.
11. **Confidence:** CONFIRMED for the tie-break logic (directly parallel to R-COST-002, verified by
    reading both). INFERRED (not exhaustively proven) that `MIN_GRP` and `MIN_GRP_CONT` are truly
    row-set-equivalent — they were written with parallel WHERE clauses by the same change-tag
    author (`DP0620`) explicitly as a query-plan alternative (T001 §4.3 already flagged this pair),
    but this task did not formally prove set-equivalence of a JOIN vs. an EXISTS-subquery under all
    data conditions (e.g., duplicate `CCG10` rows per contract could in principle affect a JOIN
    differently than an EXISTS — BLOCKED without DB2 schema/cardinality knowledge).

**Note N-1 (not a business rule):** `7795-FIND-WHICH-JOIN-010` counts candidate rows in `CCG03`
and `CCG10` (via `9505`/`9510-SQL-SELECT-010`, not otherwise detailed here) and sets 88-level
`WS-CNT-LINE-OUTER` to choose `MIN_GRP_CONT` (CCG03 has fewer candidate rows than CCG10, or either
is zero) vs. `MIN_GRP` (otherwise) — CONFIRMED lines 18503–18510, consumed by `0360` at lines 8581,
8591, 8620, 8673. This selects *which SQL text executes*, not *which contracts are eligible* — per
R-COST-004 item 11's caveat, this task treats the two cursors as producing the same candidate set
and differing only in performance, but flags that as an assumption, not a proof.

---

## Rule R-COST-005 — Suggested-cost-method dispatch (the "8 cost entry methods")

1. **Rule ID / name:** R-COST-005 — Once a contract (individual or group) is resolved, its
   `C_CNT_ENTRY_METHOD` code selects one of 8 mutually exclusive cost-computation paths.
2. **Program / paragraph:** `A6U01`, `7085-PRO-COST-DISC-010` (lines 10099–10169, `EVALUATE
   CCG01-C-CNT-ENTRY-METHOD` at line 10137), called identically from both the individual-contract
   branch (`0195`, line 4402) and the group-contract branch (`0195`, line 4457) — CONFIRMED this is
   the single shared dispatch point regardless of which contract source won R-COST-001.
3. **Preconditions:** `CCG01-C-CNT-ENTRY-METHOD` must already be populated (by whichever of `9040`,
   `9045`, `MIN_INDV`, `MIN_GRP`, or `MIN_GRP_CONT` supplied the winning contract row — note only
   `9040`/`9045` actually select `C_CNT_ENTRY_METHOD` explicitly per the SQL text read; the
   `MIN_INDV`/`MIN_GRP`/`MIN_GRP_CONT` cursors do **not** fetch `C_CNT_ENTRY_METHOD` in their
   `FETCH INTO` lists (CONFIRMED, lines 20701–20709, 20944–20952, 20959–20967) — meaning when a
   duplicate-contract tie-break path wins (R-COST-002/R-COST-004), a **second** read of `CCG01` for
   the winning `I_CONTRACT` must happen before `7085` can dispatch correctly. This second read was
   not located in the paragraphs traced in this pass — **flagged as a BLOCKED gap**: either it
   happens in `7560-PRO-CONT-MOVES-010` (performed immediately before `7085` in both branches, lines
   4400–4402 and 4455–4457, and never itself read in this task) or `CCG01-C-CNT-ENTRY-METHOD`
   retains whatever value the earlier `9037`/duplicate-count probe or prior single-row select left
   in working storage. This is the single most important follow-up for whoever extends this
   decision table, since it directly determines correctness of the dispatch after a tie-break.
4. **Data/SQL dependencies — one `WHEN` per code (CONFIRMED, lines 10137–10164, cross-referenced
   against the header comment's canonical list, lines 10103–10135):**

   | Code | Meaning (per in-source comment) | Sub-paragraph | Table read | Sets `WS-F-COST-SUGGESTED-SELL`? |
   |---|---|---|---|---|
   | `01` | Stated cost | *(none)* | *(none — uses `CCG01`/`CCG03` fields already in hand)* | No |
   | `02` | Suggested sell with brokerage % | `0135-SEL-SUGG-BROKERAG-010` | `CCG13` | Yes |
   | `03` | Suggested sell with cost-plus discount | `0140-SEL-SUGG-COST-PLU-010` | `CCG14` | Yes |
   | `04` | Suggested sell with stated cost | `0145-SEL-SUGG-STATED-010` | `CCG15` | Yes |
   | `05` | Suggested sell with cost discount | `0150-SEL-SUGG-COST-DIS-010` | `CCG16` | Yes |
   | `06` | Fixed rebate amount | *(none)* | *(none)* | No |
   | `07` | Cost-discount percent from price list | *(none)* | *(none)* | No |
   | `08` | Suggested sell with fixed rebate | `0155-SEL-SUGG-FIXED-RE-010` | `CCG21` | Yes |
   | *other* | — | `CONTINUE` (no-op) | — | No |

   Each of the four `CCG13`/`14`/`15`/`16` selects (and `CCG21`'s, by the same pattern) is keyed
   identically: `WHERE I_CONTRACT = :OMGPR-I-CONTRACT AND L_CNT_LINE = :OMGPR-L-CNT-LINE`
   (CONFIRMED, e.g. lines 20251–20252) — i.e., exactly the contract+line already resolved by
   R-COST-001–004, no further candidate search at this stage.
5. **Priority relative to competing rules:** Mutually exclusive by construction (`EVALUATE`) — not
   a priority order, a partition. Codes `02`–`05` and `08` (5 of 8) additionally call the shared
   UOM-conversion helper `7175-CVT-SUGG-010` before computing
   `OMGPR-A-CNT-LN-SUGG-SELL ROUNDED = ((<table>-A-CNT-LN-SUGG-SELL * WS-CA-CONVERT-UP-UMF) /
   WS-CA-CONVERT-DOWN-UMF)` — identical formula shape across all five, differing only in source
   table and which secondary field is also moved (`P_CNT_LN_BROKER` for `02`, `P_CNT_LN_COST_PLUS`
   for `03`, none extra for `04` beyond zeroing `OMGPR-P-CNT-LN-BROKER`, `P_CNT_LN_COST_DISC` for
   `05`; `08`/`CCG21` moves no secondary percent field at all, CONFIRMED by its absence in the
   `WHEN +0` branch, lines 2801–2807).
6. **Calculation and rounding stage:** `ROUNDED` is applied at the point `OMGPR-A-CNT-LN-SUGG-SELL`
   is computed (COBOL `COMPUTE ... ROUNDED`, standard round-half-up) for codes `02/03/04/05/08` —
   this is the *only* rounding this task confirmed within the scoped paragraphs; codes `01/06/07`
   perform no computation at all at this dispatch point (their cost math happens elsewhere, out of
   this task's traced scope).
7. **Output fields:** `OMGPR-A-CNT-LN-SUGG-SELL`, `WS-F-COST-SUGGESTED-SELL` (`'Y'` for
   `02/03/04/05/08` only — CONFIRMED this flag is never set for `01/06/07`, meaning downstream code
   gated on it can distinguish "this line's cost came from a suggested-sell-bearing contract" from
   "this line's cost came from a stated/fixed/discount-percent contract" — the downstream consumer
   of this flag was not traced in this pass), plus per-code secondary fields
   (`OMGPR-P-CNT-LN-BROKER`, `OMGPR-P-CNT-LN-COST-PLUS`, `OMGPR-P-CNT-LN-COST-DISC`) as listed above.
8. **Effective/expiration dates contributed:** None at this dispatch stage.
9. **Exclusions and fallbacks:** No fallback within `7085` itself — an unrecognized code (the
   `WHEN OTHER` branch) silently does nothing (`CONTINUE`) rather than raising an error; this means
   an unexpected `C_CNT_ENTRY_METHOD` value (anything other than `01`–`08`) is treated the same as
   `01`/`06`/`07` (no suggested-sell lookup) with **no diagnostic raised** — CONFIRMED as written;
   flagged as a silent-fallthrough risk worth surfacing to the business/QA team before migration,
   since a C# port that instead raised on an unrecognized code would be a **behavior change**, not
   a bug fix, unless explicitly approved.
10. **Errors:** Each of the five `CCG13`/`14`/`15`/`16`/`21` selects has its own `WHEN OTHER →`
    fatal error (63, 64, 65, 66, 94 respectively, all `GO TO 0020-EXIT-PRICER`) — CONFIRMED, no
    `WHEN +100` (not-found) branch is coded separately for any of the five; a not-found `SQLCODE`
    falls into the same `WHEN OTHER` fatal path as a genuine DB error (CONFIRMED — e.g. `0135`,
    lines 2659–2671 has only `WHEN +0` / `WHEN OTHER`, no `WHEN +100`). This means **a missing
    `CCG13`/`14`/`15`/`16`/`21` row for a contract whose header says it should have one is a fatal
    pricer error**, not a soft fallback — an important distinction from most other lookups in this
    codebase (which typically treat `SQLCODE +100` as tolerable).
11. **Confidence:** CONFIRMED for the dispatch table and rounding formula. BLOCKED for the item-3
    gap (how `C_CNT_ENTRY_METHOD` is repopulated after a tie-break win) — this is the most
    important open question for correctness and should be resolved (by reading
    `7560-PRO-CONT-MOVES-010` in full) before this rule area is implemented.

---

## Cross-cutting observations

- **Missing 88-level for code `08`:** `OMGPR.CPY` defines 88-level condition names for
  `C_CNT_ENTRY_METHOD` codes `01`–`07` only (`OMGPR-CNT-ENTRY-STATED-CO` ... `-FIXED-REB`,
  confirmed in `program-inventory.md` §1.4) but `A6U01` explicitly handles an 8th code, `'08'`
  ("suggested sell with fixed rebate," distinct from `'06'`'s plain "fixed rebate amount" — note
  the deceptively similar names). `7085`'s dispatch reads `CCG01-C-CNT-ENTRY-METHOD` directly
  (the raw table column), never `OMGPR-C-CNT-ENTRY-METHOD`'s 88-levels, so the missing 88-level is
  cosmetic for `A6U01` itself — **but any downstream code (including a migration target) that
  branches on `OMGPR-C-CNT-ENTRY-METHOD`'s named conditions instead of the raw `'08'` literal will
  silently miscategorize code-08 contracts**. Flagged as a concrete, actionable finding.
- **Priority-flag source table differs by contract type:** individual-contract duplicates check
  `CCG27.F_CNT_PRIORITY` (a separate `CC_CNT_PRT_FLG` table); group-contract duplicates check
  `CCG05.F_CNT_PRIORITY` (part of the group-contract-header row itself). These are two different
  tables encoding what reads as the same business concept ("this contract should win ties") —
  worth confirming with the business whether this is intentional (e.g., group priority is fixed at
  group-setup time, individual priority is a separate override table) or a historical artifact,
  before collapsing them into one concept in a migrated model.
- **No exclusion check on the search side, only the tie-break side:** R-COST-003's six-level
  search never calls `7360-VERIFY-FOR-EXCL`; only R-COST-002/R-COST-004's *duplicate-resolution*
  loops do. This means a *single*, non-duplicated contract match at any of the six group levels is
  **never checked against the exclusion list** — only when duplicates force a tie-break does
  exclusion get applied. CONFIRMED by absence (no call site found in `0275`/`0280`/`0310`/`0320`/
  `0330`/`0335` in the text read); flagged since it looks asymmetric and is worth confirming as
  intentional rather than assuming it is a gap — this task does not have enough evidence to call it
  a defect, only to note the asymmetry.

## Assumptions

1. `7560-PRO-CONT-MOVES-010` (performed immediately before every `7085` call) was not read in this
   pass; it is assumed — but not confirmed — to be where `CCG01-C-CNT-ENTRY-METHOD` gets
   (re-)populated after a tie-break win, per R-COST-005 item 3.
2. `7360-VERIFY-FOR-EXCL`, `0362-VERIFY-ELIGIBILITY`, `7695-ADD-EXP-DATE-ARRA-010`,
   `0315-SEL-ALL-ACCT-PRI-010` and its customer analogue, and the `9505`/`9510` row-count SELECTs
   behind `7795` were referenced by name and by their visible call-site effect, but their own
   paragraph bodies were not read in this pass — treat their described behavior above as
   INFERRED-from-usage, not CONFIRMED-from-definition, and re-verify before relying on it for
   implementation.
3. `WS-WS-CNT-TYPE` and `WS-WS-NOT-BYPASSED` (used as bind variables in the `MIN_GRP`/`MIN_GRP_CONT`
   WHERE clauses) were not traced to their assignment; their values are therefore unconfirmed for
   this document even though the *shape* of the predicate they participate in is confirmed.

## Blockers

1. How `C_CNT_ENTRY_METHOD` is repopulated after a `MIN_INDV`/`MIN_GRP`/`MIN_GRP_CONT` tie-break
   win (R-COST-005 item 3) — the single highest-priority gap for correctness.
2. Full DCLGEN layouts for every table referenced (`CCG01/03/04/05/06/07/09/10/11/13/14/15/16/21/27`,
   `CUG06/07/08/09/10/11/12/13`, `BGG01/02/03`) — none supplied in `upload/`, per
   `program-inventory.md` §4.2.
3. `0362-VERIFY-ELIGIBILITY`'s actual logic (group-path-only exclusion/eligibility check with no
   individual-path counterpart found) — not read in this pass.
4. Formal proof (or DB2 execution-plan evidence) that `MIN_GRP` and `MIN_GRP_CONT` are row-set
   equivalent, not just intended to be.

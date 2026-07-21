# A6U01 — Cost Selection Decision Tables (T007)

**Scope:** Ordered decision-table extraction, per `CLAUDE.md`'s required 11-element rule format,
for the cost-control cascade starting from `A6U01`'s cost paragraphs and all transitive
paragraphs they call. Covers every path named in T007's acceptance criteria: individual,
account/customer, group, parent, special, healthcare, and acquisition fallback.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL` (26,657 lines) and `OMGPR.CPY`.
Line citations refer to `A6U01.CBL` unless a different file is named. No DCLGEN copybooks for
`CCG01`, `CCG03`, `CCG05`, `CCG06`, `CCG07`, `CCG09`, `CCG10`, `CCG11`, `CCG27`, `CUG09`, `CUG12`,
`CUG13`, `BGG01`, `BGG02`, `VNG03` are supplied in `upload/` — every field sourced from these is
marked BLOCKED or INFERRED at the point it is used, never assumed.

**Updated — second upload batch (2026-07-20 pass):** DCLGEN copybooks for `CUG06`, `CUG07`,
`CUG10`, `CUG11`, `BGG03`, `CCG25`, and `VNG02` are now supplied and read in full
(`docs/cobol-analysis/program-inventory.md` §4.2). Field-level BLOCKED/INFERRED notes citing these
six tables below are upgraded to CONFIRMED where the field in question matches a field actually
present in the new copybook; see R-COST-001 item 9, R-COST-003, R-COST-004, and R-COST-005 for the
specific upgrades. This does not change any rule's *behavior* — only its evidence confidence — since
the field-level shapes now confirmed match what had already been INFERRED from usage.

**Depends on (per `tasks.md`):** T001 (`docs/cobol-analysis/program-inventory.md`, complete) and
T005 (`docs/cobol-analysis/sql-query-inventory.csv` + `db2-table-inventory.md`, complete). Table
names cited as "T005-confirmed" were cross-checked against the SQL inventory CSV built for T005.

**Relationship to `docs/rules/fees-and-adjustments.md` (T009) and `docs/rules/date-selection.md`/
`rounding.md` (T010):** this document resolves one blocker left open in T009 —
`7105-SEL-PRC-LST-010`'s internals, cited as BLOCKED in `fees-and-adjustments.md`'s `R-REBATE-000`,
are now fully documented here as `R-ACQ-001`. It also reuses T010's date-boundary and
closest-expiration conventions without re-deriving them; see `date-selection.md` for that shared
mechanism.

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Cross-cutting findings

1. **Three independent, mutually-inconsistent "duplicate row" resolution conventions exist in
   this codebase, each for its own specific scenario** — R-COST-002 (duplicate individual
   contracts) selects the LOWEST cost; R-HC-002 (healthcare-eligible price-list rows) selects the
   HIGHEST acquisition cost; R-ACQ-001 (price-list fallback with no direct match) selects by
   MOST RECENT effective date, then highest price level as tie-break. None of these generalizes
   to the others — a reimplementation must keep each duplicate-resolution rule scoped to its own
   caller, not factor them into one shared "pick the best row" utility.
2. **The top-level cost-selection order is strictly individual-then-group, first-match-wins, with
   no combining or re-checking** (R-COST-000): once an individual contract is found, group search
   never runs; once found by either path, the paragraph exits immediately.
3. **A contract found by SQL can still be rejected** by a separate exclusion check
   (`7360-VERIFY-FOR-EXCL`, R-COST-001 item 9) — "found" and "usable" are two distinct states in
   this codebase, both must be checked.
4. **Special-contract mode narrows individual-contract eligibility to bypass-flagged rows only**,
   and aborts fatally (rather than falling back to group/acquisition cost) if none exist
   (R-COST-001 item 5/9) — then, once a contract IS found, forces the sell price to the contract's
   own suggested-sell value and explicitly skips the account-level rounding stage that every other
   pricing path goes through (R-SPECIAL-001), for reasons no supplied comment explains.
5. **The buy-group priority walk is a nested two-level cascade, not a flat list**: outer loop by
   priority level (ascending), inner check per level is child-BG-then-full-ancestor-climb before
   advancing to the next priority level (R-COST-005) — a common simplification error would be to
   treat "priority" and "parent/child" as a single flat ordering, which this evidence contradicts.
6. **The healthcare-override mechanism only ever activates when NO contract (individual or group)
   was found** (R-HC-001 precondition) — it is a fallback layered before the final acquisition/
   dealer-cost fallback, not an independent parallel cost source.
7. **Acquisition/dealer cost is always computed, even when a contract IS found** (`7065-PRO-DEAL-
   COST-010` runs unconditionally at the top of `7190-PRO-COST-AMTS-010`, before any contract
   search) — it is not looked up lazily only when needed; the contract-selection cascade
   determines whether that already-computed value is ultimately USED (R-ACQ-002), not whether it
   exists at all.

---

## 1. Individual contract selection

# Individual cost-contract selection — extracted rules (source notes for cost-selection-rules.md)

Program/paragraph: A6U01.CBL `0195-PRO-COST-CONT-010` (4366-4494, top dispatcher) ->
`0235-SEL-INDV-CNT-010` (5833-5970, individual-contract selection, tried FIRST) ->
`9037-SQL-ROW-CNT-010` (duplicate-count check) -> `0270-SEL-MIN-INDV-CNT-010` (6313-6440,
lowest-cost-wins duplicate resolution) -> `0272-COMPARE-COST`/`0273-CHECK-PRIORITY-COST`
(6442-6487, per-row comparison).

### R-COST-000 Individual-vs-group top-level dispatch
1. ID/Name: R-COST-000 Individual contract tried before group contract
2. Program/paragraph: 0195-PRO-COST-CONT-010, lines 4366-4494
3. Preconditions: called from 7190-PRO-COST-AMTS-010 for every priced line, after acquisition/
   dealer cost is already computed (R-ACQ-001)
4. Data deps: none beyond what 0235/0240 themselves require
5. Priority: CONFIRMED, individual contract (0235) is tried unconditionally first; group contract
   (0240, via 7795-FIND-WHICH-JOIN-010 then 0240-PRO-GRP-CNT-010) is tried ONLY if
   WS-COST-CONT-FND is still false after the individual attempt (`IF WS-COST-CONT-FND ... GO TO
   0195-EXIT` immediately after 0235 — group search is unreachable code once an individual
   contract is found). Neither path is retried or combined; whichever is found first wins outright
   and the paragraph exits.
6. Calculation: N/A — dispatch only
7. Output: WS-COST-CONT-FND / WS-F-CONTRACT-TYPE-SW, plus (on either path) closest-expiration-date
   candidates contributed per R-DATE-003 (contract-protection-end date, unconditionally; contract
   line expiration and assignment/eligibility expiration, conditionally on non-null)
8. Dates: CCG01-D-CNT-PROT-END is ALWAYS added to the expiration-date array on either successful
   path (not gated by a null check, unlike every other date in this cascade) — confirmed at both
   4406-4408 (individual) and 4460-4462 (group), an apparent deliberate exception to the R-DATE-001
   null-check convention, though CCG01-D-CNT-PROT-END's own nullability was not independently
   confirmed (DCLGEN for CCG01 not supplied)
9. Exclusions/fallbacks: if NEITHER individual nor group contract is found, WS-COST-CONT-NOT-FND
   remains true and control returns to 7190, which then attempts the healthcare-override check
   (R-HC-001) before falling back to acquisition/dealer cost (R-ACQ-002)
10. Errors: none in this dispatcher itself (errors belong to 0235/0240's own SQL)
11. Confidence: CONFIRMED

### R-COST-001 Individual contract selection and duplicate-count check
1. ID/Name: R-COST-001 Individual customer cost-contract lookup
2. Program/paragraph: 0235-SEL-INDV-CNT-010, lines 5833-5970
3. Preconditions: none beyond R-COST-000's dispatch
4. Data deps: 9037-SQL-ROW-CNT-010 (a `SELECT COUNT(*)`-style row-count check, not itself
   re-transcribed this pass) determines WS-DUP-CONT-EXISTS; if not duplicated,
   9040-SQL-SELECT-010 does the direct single-row lookup; if duplicated, dispatches to
   R-COST-002 instead
5. Priority: **special-contract interaction (CONFIRMED, header comment + code, lines 5866-5868)**
   — when OMGPR-F-SPECIAL-CONTRACT='Y', WS-WS-NOT-BYPASSED is forced to `'B'` before the SQL
   lookup runs, which (per the comment) restricts the lookup's WHERE clause (embedded inside
   9037/9040, not independently re-transcribed) to ONLY individual contracts explicitly flagged
   with bypass-code `'B'` — i.e. special-contract pricing requests can only ever resolve to a
   specially-bypass-flagged individual contract, not any ordinary one. This is the confirmed
   precondition that makes R-SPECIAL-001 (in the special-contract section) reachable at all.
6. Calculation: not duplicated -> single SELECT (9040), success path evaluated at item 9;
   duplicated -> entire lookup deferred to R-COST-002
7. Output: WS-F-COST-CONT-FND, contract line/header fields (CCG01/CCG03/CCG06/CCG09, per
   R-COST-000's own MOVE list once found)
8. Dates: CCG01-D-CNT-EXPIRE contributed unconditionally (not null-gated) to the
   closest-expiration array on a successful single-row SELECT (line 5894-5897)
9. Exclusions/fallbacks: **contract-exclusion override (CONFIRMED, lines 5898-5908)** — even
   after a successful SELECT (SQLCODE 0), the contract can still be rejected: if
   `OMGPR-ACT-CNT-EXCL-ABSENT` (no exclusion configured for this account), accept it
   unconditionally; otherwise PERFORM `7360-VERIFY-FOR-EXCL` (an account-contract-exclusion
   check keyed additionally by ship-to-suffix, checking a default '000' ship-to first and then
   the specific ship-to if not '000'/blank) and if `OMGPR-CONTRACT-EXCLUDED` results,
   `WS-F-COST-CONT-FND` is forced back to 'N' — **a contract that was just found by SQL can be
   un-found by this exclusion check**, a genuine two-stage "found, then possibly rejected"
   pattern worth preserving exactly. SQLCODE=100 (no individual contract at all): if
   OMGPR-F-SPECIAL-CONTRACT='Y', this is a **hard fatal error** (#601, "SPECIAL CONTRACT
   NOTFOUND FOR PRODUCT") — a special-contract pricing request with no matching bypass-flagged
   individual contract does not fall back to group/acquisition cost, it aborts the whole pricer;
   for a normal (non-special) request, SQLCODE=100 is a quiet fallthrough to R-COST-000's group
   search.
10. Errors: fatal DB error #37 (WHEN OTHER on 9040); fatal error #601 (special-contract-not-found,
    item 9); fatal DB error #38 on the follow-up "remaining info" SELECT (9045, item 7's field
    completion step, lines 5943-5964) — a contract found via the row-count/single-select path
    still requires a second SELECT to pull the rest of its fields, and that second SELECT can
    itself fail fatally even though the first one succeeded
11. Confidence: CONFIRMED for control flow. INFERRED that 9037/9040's embedded WHERE clause
    literally filters on the bypass-code field per the comment's description — the SQL text
    itself was not independently re-transcribed in this pass (deferred to the T005 SQL CSV rows
    for 9037-SQL-ROW-CNT-010/9040-SQL-SELECT-010).

**Update (second upload batch):** `7360-VERIFY-FOR-EXCL`'s underlying table, `CCG25`, now has a
supplied DCLGEN (`CCG25.CPY`, CONFIRMED): `CCG25-I-CONTRACT` (S9(9) COMP), `CCG25-I-ACCOUNT`
(S9(9) COMP), `CCG25-C-SHIP-TO-SUFFIX` (X(3) — matches item 9's "default '000' ship-to first, then
the specific ship-to" description), `CCG25-D-EFFECT`/`CCG25-D-EXPIRE` (X(10), EXPIRE nullable),
`CCG25-T-COMMENT` (X(35)). This confirms the exclusion check's key shape but the exact WHERE-clause
predicate inside `7360-VERIFY-FOR-EXCL` remains unconfirmed (T005 SQL CSV, not re-transcribed here).

### R-COST-002 Duplicate individual contracts: lowest-cost-wins resolution
1. ID/Name: R-COST-002 Lowest-cost selection among duplicate individual contracts
2. Program/paragraph: 0270-SEL-MIN-INDV-CNT-010 (6313-6440), calling 0272-COMPARE-COST/
   0273-CHECK-PRIORITY-COST (6442-6487) once per fetched row
3. Preconditions: WS-DUP-CONT-EXISTS = true (the product is found on more than one active
   individual cost contract for the same customer/time period, per 9037's row count) — CONFIRMED
   by the paragraph's own header comment, lines 6317-6321
4. Data deps: cursor-based fetch loop (9060 close / 9065 open / 9070 fetch) over the duplicate
   contract rows; each row's cost is converted to the ordered UOM via the same convert-up/down-UMF
   pattern used throughout this codebase (`WS-A-CNT-LN-UNIT-COST ROUNDED = (CCG03-A-CNT-LN-UNIT-
   COST * CONVERT-UP-UMF) / CONVERT-DOWN-UMF`) before comparison
5. Priority (CONFIRMED, two distinct comparison modes selected by `CCG27-F-CNT-PRIORITY`):
   - **Non-priority mode** (CCG27-F-CNT-PRIORITY <> 'Y', the default): each fetched row's
     converted cost is compared directly against whichever cost is CURRENTLY held in
     `OMGPR-A-CNT-LN-UNIT-COST` — if strictly lower, OR if the currently-held cost is exactly zero
     AND no contract has been selected yet (`OMGPR-I-CONTRACT = 0`), this row's contract replaces
     the held one. **The zero-cost tie-break is explicit and deliberate** (comment: "IF
     I-CONTRACT > 0 AND IF PREVIOUSLY A CONTRACT WITH 0 COST MAY BE SELECTED, WE DO NOT WANT TO
     OVERRIDE IT WITH ANOTHER CONTRACT") — once a zero-cost contract has been selected
     (I-CONTRACT already non-zero), a *subsequent* zero-cost row does NOT replace it, preventing
     endless zero-cost rows from churning the selection; but the FIRST zero-cost row encountered,
     with no contract selected yet, DOES win over nothing.
   - **Priority mode** (CCG27-F-CNT-PRIORITY = 'Y'): the identical comparison logic runs against a
     separate working-storage holding area (`WS-PRIORITY-LN-UNIT-COST`/`WS-PRIORITY-I-CONTRACT`)
     instead of writing directly to OMGPR on each row; only after the fetch loop exhausts
     (SQLCODE=100) is the final winning priority contract's cost copied into OMGPR (lines
     6390-6395). **End result is the same "lowest cost wins" outcome as non-priority mode** — this
     is an implementation-detail difference (deferred/staged commit vs. immediate commit), not a
     different selection rule, as far as this pass could confirm; whether `CCG27-F-CNT-PRIORITY`
     can vary row-to-row within one duplicate set (which would make the staging meaningfully
     different) was not confirmed (CCG27 DCLGEN not supplied).
6. Calculation: `COMPUTE WS-A-CNT-LN-UNIT-COST ROUNDED = (CCG03-A-CNT-LN-UNIT-COST *
   WS-CA-CONVERT-UP-UMF) / WS-CA-CONVERT-DOWN-UMF` per row, then the comparison in item 5
7. Output: OMGPR-I-CONTRACT, OMGPR-A-CNT-LN-UNIT-COST, WS-DUP-CONT-SELECTED-SW
8. Dates: none additional beyond what R-COST-001 already contributes
9. Exclusions/fallbacks: each fetched row is also subject to the SAME contract-exclusion check
   as R-COST-001 (7360-VERIFY-FOR-EXCL, gated by OMGPR-ACT-CNT-EXCL-ABSENT) — an excluded row is
   skipped (CONTINUE) without being compared at all, confirmed at lines 6375-6387
10. Errors: fatal DB error #41 (cursor open failure), #42 (fetch failure), #43 (cursor close
    failure) — all on the cursor lifecycle around this comparison loop
11. Confidence: CONFIRMED for the comparison logic and zero-cost tie-break; INFERRED that
    priority-mode and non-priority-mode always produce an identical final answer (not proven for
    every possible input, only observed to use the same comparison formula)

---

## 2. Group, account, customer, and parent contract selection

# Group/account/customer/parent cost-contract selection — extracted rules (source notes)

Program/paragraph: A6U01.CBL `0240-PRO-GRP-CNT-010` (5973-6018, dispatcher) ->
`0275-PRO-ACCT-BG-010` (6490-6658, account path) / `0280-PRO-CUST-BG-010` (6661-6830, customer
path, tried only if account path fails) -> each path's three sub-mechanisms: product-category
override (`0310-SEL-ACCT-PRD-CAT-010`/`0330-SEL-CUST-PRD-CAT-010`), vendor override
(`0320-SEL-ACCT-VEND-BG-010`/its customer equivalent), and the priority-walk-with-parent-climb
(`0315-SEL-ALL-ACCT-PRI-010`/`0330`'s customer equivalent, calling `7315-SEL-GRP-CNT-010` at each
BG level and `7320-SEL-PARENT-COST-010`/`7321-SEL-PARENT-COST-01-010` to climb ancestors).

### R-COST-003 Group contract: account tried before customer
1. ID/Name: R-COST-003 Account-level group-contract search precedes customer-level
2. Program/paragraph: 0240-PRO-GRP-CNT-010, lines 5973-6018
3. Preconditions: reached only when R-COST-001 (individual contract) found nothing, per R-COST-000
4. Data deps: none beyond what 0275/0280 themselves require
5. Priority (CONFIRMED, explicit header comment matching the code exactly): account path
   (0275) always tried first; customer path (0280) tried ONLY `IF WS-COST-CONT-NOT-FND` after the
   account path (line 5992), and additionally re-initializes the bypassed-buy-group tracking array
   (`INITIALIZE WS-BG-BYPASSED-ARRAY`) before starting the customer search — i.e. a BG bypassed
   during the account search does not carry that bypass status into the customer search.
6. Calculation: N/A — dispatch only
7. Output: WS-COST-CONT-FND; additionally, once either path succeeds, a "preferred tier level"
   is captured (lines 5998-6014): if the account-level BG-priority row (CUG11) had a positive
   `CUG11-Q-PREF-TIER-LEVEL`, that (and its tier-start date) is used; otherwise the customer-level
   equivalent (CUG07) is used — this tier-level output is a side effect of whichever path actually
   succeeded, not an independent third path.
8. Dates: OMGPR-D-BG-TIER-START (from whichever of CUG11/CUG07 supplied the tier level)
9. Exclusions/fallbacks: if neither account nor customer group search succeeds,
   WS-COST-CONT-NOT-FND remains true and control returns up to R-COST-000/7190 for the
   healthcare-override/acquisition-cost fallback
10. Errors: none in this dispatcher itself
11. Confidence: CONFIRMED

### R-COST-004 Per-scope (account or customer) three-tier override/priority cascade
1. ID/Name: R-COST-004 Product-category override, then vendor override, then BG-priority walk
2. Program/paragraph: 0275-PRO-ACCT-BG-010 (account, 6490-6658) and 0280-PRO-CUST-BG-010
   (customer, 6661-6830) — **CONFIRMED structurally identical** (independently re-read both;
   0280 mirrors 0275 line-for-line in control flow, differing only in which CUGnn tables are
   used: CUG13/CUG12/CUG11/CUG10 for account, CUG09/(vendor-override table not re-transcribed)/
   CUG07/CUG06 for customer)
3. Preconditions: reached per R-COST-003's dispatch (account unconditionally, customer only if
   account found nothing)
4. Data deps: `OMGPR-S-PROD-CATEGORY` gates whether the product-category override is even
   attempted (`IF OMGPR-S-PROD-CATEGORY NOT = ZEROES`) — a line with no product category skips
   straight to the vendor-override check
5. Priority (CONFIRMED, three sequential tiers, first-match-wins, identical shape at both
   account and customer scope):
   1. **Product-category BG override** (0310/0330): only attempted if a product category is set
      on the line. A cursor loop (`PERFORM UNTIL SQLCODE <> 0 OR WS-COST-CONT-FND`) walks
      candidate override rows (CUG13 account / CUG09 customer), calling
      `0340-PRO-CUG13-010` (not independently re-traced this pass) per candidate row until either
      a contract is found or the candidate rows are exhausted. If found: resolve the winning BG's
      priority-verification row (`7855-SEL-ACCT-PRI-VER-010`/customer equivalent) then priority
      row (`7900-SEL-ACCT-PRI-010`/customer equivalent) to classify the contract type (see item
      6), mark `WS-F-COST-BG-LEVEL = CHILD` and `WS-F-COST-OVERRIDE = 'Y'`, done.
   2. **Vendor BG override** (0320/customer equivalent): tried only if tier 1 found nothing.
      Same resolve-priority-row-then-classify shape, same `WS-F-COST-OVERRIDE = 'Y'` marker.
   3. **BG-priority walk** (0315/customer equivalent, see R-COST-005): tried only if neither
      override tier found anything. This is the ONLY one of the three tiers where
      `WS-F-COST-OVERRIDE` stays `'N'` (no override was used — the contract came from the
      account/customer's ordinary buy-group membership priority, not a special override row).
6. Calculation: no cost arithmetic in this paragraph itself — it is purely a source-selection
   cascade. `WS-C-COST-PRIORITY = 1` (priority level 1) sets `WS-F-CONTRACT-TYPE-SW` to the
   "primary group" indicator; any other priority level sets it to "other group" — this
   classification happens identically at all three tiers (override tiers use whatever priority
   level the winning override's own BG happens to carry, not necessarily level 1).
7. Output: WS-COST-CONT-FND, WS-F-COST-BG-LEVEL (CHILD at the override tiers; CHILD or PARENT at
   the priority-walk tier, see R-COST-005), WS-F-COST-OVERRIDE, WS-F-CONTRACT-TYPE-SW,
   OMGPR-I-BUY-GROUP
8. Dates: each of the three tiers contributes its own override/priority-row expiration date to
   the closest-expiration array (R-DATE-003) upon success — CUG13/CUG10 (product-category tier),
   CUG12/CUG10 (vendor tier), CUG10 alone (priority-walk tier, captured inside R-COST-005)
9. Exclusions/fallbacks: if all three tiers fail, WS-COST-CONT-FND remains false and control
   returns to R-COST-003's dispatcher (account failure triggers the customer attempt; customer
   failure returns all the way to 7190's healthcare/acquisition fallback)
10. Errors: fatal DB errors at multiple points — #150/#151 (0310's cursor open/close), #5 (0310's
    fetch), #58/#67/#68/#97 (0275's own CUG10/CUG11 SELECT failures inside the override-resolution
    branches) — every SQLCODE OTHER in this cascade is fatal-to-the-whole-pricer, consistent with
    the general convention (contrast with Surcharge's documented exception in
    `fees-and-adjustments.md`)
11. Confidence: CONFIRMED for the three-tier structure and priority (independently verified by
    reading both the account and customer paragraphs in full). INFERRED/deferred for
    `0340-PRO-CUG13-010`'s and the vendor-override candidate-row-selection paragraphs' own
    internal logic (not re-traced to formula depth — their existence and role in the cascade is
    confirmed, their exact WHERE-clause/matching detail is not).

### R-COST-005 BG-priority walk: priority-level order, child-before-parent, and the ancestor climb
1. ID/Name: R-COST-005 Buy-group priority-level walk with parent/ancestor climb
2. Program/paragraph: 0315-SEL-ALL-ACCT-PRI-010 (account, 7362-7559) — customer equivalent not
   independently re-read this pass but confirmed structurally parallel by the same
   cross-reference pattern already established for R-COST-004; child-level group-contract check
   via `7315-SEL-GRP-CNT-010` (14668-14778, not independently re-transcribed to formula depth);
   ancestor climb via `7320-SEL-PARENT-COST-010` (14799-14847) ->
   `7321-SEL-PARENT-COST-01-010` (14850-14937, the actual loop)
3. Preconditions: reached only when neither the product-category nor vendor override tier
   (R-COST-004) found a contract
4. Data deps: a cursor over the account's (or customer's) buy-groups (CUG11/CUG07), fetched in
   the database's own priority order — CONFIRMED by the paragraph's comment "PROCESS THE #1
   PRIORITY FIRST," and the loop structure itself does no additional client-side sorting, so the
   priority ordering is entirely delegated to the SQL cursor's `ORDER BY` (not independently
   re-transcribed, deferred to the T005 CSV)
5. Priority (CONFIRMED, nested two-level cascade): **outer loop = priority level, ascending, per
   the cursor's own order; inner check, per priority level = child BG first, then that BG's full
   ancestor chain.** For each fetched priority-level row: skip if the BG is in the bypassed-array
   (`7910-CHK-BYP-ARRAY-010`); otherwise verify/refresh membership at that BG
   (`7770-SEL-BGM-WITH-NBR-010`) and attempt a group-cost-contract lookup at the CHILD (this
   priority level's own BG) via `7315-SEL-GRP-CNT-010`. If found: done, `WS-F-COST-BG-LEVEL =
   CHILD`. If NOT found at the child level: climb the parent chain from that same BG
   (`7320`/`7321`) — CONFIRMED, `7321`'s own header comment states this climb continues
   "UNTIL THERE ARE NO MORE PARENTS TO PROCESS OR A CONTRACT IS FOUND," i.e. it is not limited to
   one immediate parent, it walks arbitrarily far up the hierarchy (grandparent, great-grandparent,
   etc.) trying `7315` at every ancestor level. If found there: done,
   `WS-F-COST-BG-LEVEL = PARENT`. If the entire ancestor chain for this priority level is
   exhausted with no contract found: **the outer loop advances to the NEXT priority level and
   repeats the same child-then-full-ancestor-chain search from there** — it does not simply give
   up after the first priority level's ancestor chain fails.
6. Calculation: no cost arithmetic in this paragraph — pure source selection; the cost VALUE
   itself is populated by whichever `7315-SEL-GRP-CNT-010` invocation actually succeeds (that
   paragraph's own SELECT populates the CCG10/CCG11/etc. cost-contract-line fields, not
   independently re-transcribed to formula depth this pass).
7. Output: WS-COST-CONT-FND, WS-F-COST-BG-LEVEL (CHILD or PARENT), WS-C-COST-PRIORITY (the
   winning row's own priority-level number, 1 or otherwise), WS-F-CONTRACT-TYPE-SW (derived from
   whether that priority level equals 1, independently at both the child-found and
   parent-found outcomes), OMGPR-I-BUY-GROUP (the CHILD BG's own ID even when the contract was
   actually found at a PARENT level — confirmed at line 7501, `BGG03-I-BUY-GROUP-PARENT` is moved
   to `OMGPR-I-BUY-GROUP` in the parent-found branch, meaning OMGPR-I-BUY-GROUP always reflects
   whichever specific BG level the contract was actually attached to, not necessarily the
   original child)
8. Dates: CUG10-D-ACCT-BG-PRI-EXP contributed at both the child-found and parent-found outcomes;
   BGG03-D-PARTNER-EXPIRE contributed during the ancestor climb itself (7321, per-ancestor-level)
9. Exclusions/fallbacks: **membership-checking nuance (CONFIRMED, reconciling two comment blocks
   that read differently in isolation)** — 7320's own header comment states "MEMBERSHIP IS NOT
   CHECKED AT THE PARENT LEVEL... IF MEMBERSHIP IS OKAY AT THE CHILD LEVEL, THEN IT IS OKAY AT THE
   PARENT(S) LEVEL," but 7321's actual code (lines 14874-14898) DOES re-check membership at each
   ancestor level via `7590-SEL-BGM-WITHOUT-N-010` — specifically, and only, when the ORIGINAL
   child-level membership was NOT already confirmed (`WS-BG-MEMBER-NOT-FND`). Reconciled reading:
   if child membership was already confirmed, the code trusts that same membership through the
   whole ancestor chain without re-verifying at each level; if child membership was NOT confirmed
   to begin with, each ancestor level gets its own explicit membership check before the
   group-cost lookup is attempted there. The header comment describes the common case, not the
   only case.
10. Errors: fatal DB error #12/#13/#14 (0315's own cursor open/fetch/close), #44 (7321's BGG03
    parent-fetch failure)
11. Confidence: CONFIRMED for the priority-level/child-then-ancestor structure (directly read,
    including the membership-checking nuance reconciled from both paragraphs' actual code).
    INFERRED/deferred for `7315-SEL-GRP-CNT-010`'s own SELECT/formula detail and for the
    customer-side equivalent paragraphs' byte-for-byte parity with the account side (confirmed
    structurally parallel at the 0275/0280 level, not re-verified at this deeper 0315-equivalent
    level).

**Update (second upload batch) — field-level confirmation for R-COST-003/004/005's table
citations:** DCLGEN copybooks for `CUG10`, `CUG11` (account BG-priority), `CUG06`, `CUG07`
(customer BG-priority equivalents), and `BGG03` (parent buy-group) are now supplied and read in
full. Confirmed field shapes: `CUG10-I-ACCOUNT` (S9(8) COMP), `CUG10-D-ACCT-BG-PRI-EFF`/`-EXP`
(X(10), EXP nullable); `CUG11-I-ACCOUNT` (S9(8) COMP), `CUG11-D-ACCT-BG-PRI-EFF` (X(10)),
`CUG11-I-BUY-GROUP` (S9(8) COMP), `CUG11-S-BG-MEMBER` (S9(8) COMP), `CUG11-Q-ACCT-BG-PRIORITY`
(S9(4) COMP — the priority-level ordinal R-COST-005 walks), `CUG11-Q-PREF-TIER-LEVEL` (S9(4) COMP,
cited in R-COST-003 item 7), `CUG11-D-BG-TIER-START` (X(10)); `CUG06`/`CUG07` mirror
`CUG10`/`CUG11` exactly with `I_CUSTOMER` in place of `I_ACCOUNT`; `BGG03-I-BUY-GROUP` (S9(8) COMP,
nullable), `BGG03-I-BUY-GROUP-PARENT` (S9(8) COMP NOT NULL — the field R-COST-005 item 7 cites at
line 7501), `BGG03-D-PARTNER-EXPIRE`/`-EFFECT`/`-START` (X(10), EXPIRE nullable). This upgrades the
"table exists, fields used by name only" confidence to CONFIRMED field-level shape; the SQL
WHERE-clause/cursor-ORDER-BY predicates that select among rows in these tables remain deferred to
the T005 SQL CSV as before — only the target record layouts are newly confirmed, not the query
logic.

---

## 3. Special-contract cost/sell override

# Special-contract cost/sell rules — extracted rules (source notes for cost-selection-rules.md)

Program/paragraph: A6U01.CBL `0365-PROCESS-SPECIAL-C-010` (8792-8809) calling
`7960-PRO-SPECIAL-MOVES-010` (20034-20047). Gate: `OMGPR-F-SPECIAL-CONTRACT` — a COMMAREA input
field (`OMGPR.CPY` line 308, `PIC X(01)`, no 88-levels defined for it), i.e. the caller explicitly
requests special-contract pricing for this line; it is never computed or inferred within
`A6U01.CBL`.

### R-SPECIAL-001 Special-contract sell-price override
1. ID/Name: R-SPECIAL-001 Special-contract forces sell price to contract's suggested sell
2. Program/paragraph: 0365-PROCESS-SPECIAL-C-010 (8792-8809), calling 7960-PRO-SPECIAL-MOVES-010
   (20034-20047); dispatched from 0030-PROCESS-COST (`IF OMGPR-F-SPECIAL-CONTRACT = 'Y' PERFORM
   0365-PROCESS-SPECIAL-C-010 ... GO TO 0030-EXIT`, already documented in
   `fees-and-adjustments.md`'s rebate section as the caller context around line 2549-2553)
3. Preconditions: `OMGPR-F-SPECIAL-CONTRACT = 'Y'` AND a cost contract was actually found and
   selected — **CONFIRMED, special-contract mode does not change WHICH contract-selection cascade
   runs** (R-COST-001 through R-COST-005 all still apply unmodified); its only two confirmed
   effects are (a) restricting individual-contract selection to bypass-flagged rows only
   (R-COST-001 item 5) and, if that yields nothing, aborting with fatal error #601 rather than
   falling through to group/acquisition cost (R-COST-001 item 9); and (b) this rule's own
   sell-price override, reached only after a contract WAS successfully selected under that
   restriction.
4. Data deps: OMGPR-A-CNT-LN-UNIT-COST, OMGPR-A-CNT-LN-SUGG-SELL — both already populated by the
   ordinary individual-contract-selection cascade (R-COST-001), not looked up freshly by this
   rule
5. Priority: this rule runs INSTEAD of the normal sell-arrangement/markup/margin resolution
   cascade (T008's future scope) — `0030-PROCESS-COST`'s dispatch is `GO TO 0030-EXIT`
   immediately after 0365 returns, bypassing `7215-PRO-VNDRCOSTADJ-010` and
   `7200-PRO-ADJ-COST-010` (freight/overhead/JIT/surcharge adjustments, all of
   `fees-and-adjustments.md`) entirely for this line — **a special-contract line receives NONE of
   the fee/adjustment processing documented in `fees-and-adjustments.md`**, only the raw
   contract-line cost and suggested-sell values, moved directly.
6. Calculation: no arithmetic — direct MOVEs only: `OMGPR-A-TOTAL-COST = OMGPR-A-CNT-LN-UNIT-COST`
   and `OMGPR-A-CUS-UOM-SELL-PRC = OMGPR-A-TOTAL-SELL = OMGPR-A-CNT-LN-SUGG-SELL` (the same
   contract-supplied suggested-sell value populates BOTH the unit-price and total-sell output
   fields).
7. Output: OMGPR-A-TOTAL-COST, OMGPR-A-CUS-UOM-SELL-PRC, OMGPR-A-TOTAL-SELL
8. Dates: `7720-PRO-EXPIRE-DATES-010` (R-DATE-004) still runs for special contracts (called
   directly from 0365, line 8806-8807) — closest-expiration resolution is NOT skipped, only
   rounding is (see item 9)
9. Exclusions/fallbacks: **CONFIRMED, notable divergence from every other pricing path** — the
   call to `7715-PRO-ROUNDING-010` (R-ROUND-001, the account-configurable final rounding stage)
   is explicitly commented out in 0365 (`pt0106*****PERFORM 7715-PRO-ROUNDING-010`, lines
   8801-8802) and replaced with nothing — a special-contract line's sell price is **never passed
   through the account's configured rounding mode at all**, regardless of what
   `OMGPR-C-ACCT-ROUNDING` says for that account. It is returned at whatever native precision
   `OMGPR-A-CNT-LN-SUGG-SELL` itself carries. This is a confirmed, deliberate-looking removal (a
   change-ID comment prefix `pt0106` marks it, not a stray edit), not an oversight this task is
   inferring — but *why* rounding was removed specifically for special contracts is not
   explained by any comment, so the removal's business rationale itself is unknown (worth a
   product/business confirmation before assuming it should carry over unchanged into a
   reimplementation).
10. Errors: none in this paragraph itself (the fatal #601 case belongs to R-COST-001, upstream of
    this rule ever running)
11. Confidence: CONFIRMED for the control flow, the sell-price override formula, and the
    rounding-skip. BLOCKED for the business rationale behind skipping rounding specifically for
    special contracts.

---

## 4. Healthcare override

# Healthcare override cost rules — extracted rules (source notes for cost-selection-rules.md)

Program/paragraph: A6U01.CBL `9940-CHECK-HC-COST-OVERRIDE` (26045-26061) ->
`9945-CHECK-HC-COST-ACCOUNT` (26063-26158, account-keyed eligibility, tried first) ->
`9950-CHECK-HC-COST-CID` (26160-..., customer-ID-keyed fallback, tried only if account misses) ->
(if eligible and not product-overridden) `9970-SELECT-HC-COST-VNG03` (26441-26505) ->
`9975-CONVERT-VNG03-FIELDS` (26507-26538). Called from `7190-PRO-COST-AMTS-010`
(lines 12327-12350), only inside the `IF WS-COST-CONT-NOT-FND` branch — i.e. **the healthcare
override mechanism only ever runs when NO individual or group cost contract was found**
(R-COST-000/R-COST-003 both failed). Tables T005-confirmed: `HC_OVRD_GROUP_HEADER`,
`HC_OVRD_GROUP_ACCOUNT`, `HC_OVRD_GROUP_CUSTOMER`, `HC_OVRD_GROUP_DETAIL`, `HC_OVRD_PRODUCT`.

### R-HC-001 Healthcare-override eligibility (account-then-CID cascade)
1. ID/Name: R-HC-001 Healthcare cost-override eligibility lookup
2. Program/paragraph: 9940-CHECK-HC-COST-OVERRIDE (26045-26061), dispatching to
   9945-CHECK-HC-COST-ACCOUNT (account-keyed) then 9950-CHECK-HC-COST-CID (customer-ID-keyed,
   fallback only)
3. Preconditions: WS-COST-CONT-NOT-FND (reached only when the entire individual+group
   contract-selection cascade, R-COST-001 through R-COST-005, found nothing)
4. Data deps: a 4-table join, identical shape at both account and CID scope:
   `HC_OVRD_GROUP_HEADER` (group-level date window) joined to `HC_OVRD_GROUP_ACCOUNT` (account
   scope, keyed `HC_DIVISION`+`HC_ACCOUNT` = `OMGPR-I-DIVISION`+`OMGPR-S-ACCOUNT`) or
   `HC_OVRD_GROUP_CUSTOMER` (CID scope, keyed `HC_CUSTOMER_NBR` = `OMGPR-CUSTOMER-NBR`) joined to
   `HC_OVRD_GROUP_DETAIL` (group-level override-flag/date window) joined to `HC_OVRD_PRODUCT`
   (vendor+product scope, keyed `HC_VEND_NBR`+`HC_PROD_NBR` = `OMGPR-I-VENDOR`+
   `OMGPR-I-VND-PRODUCT`) — **every one of the four tables carries its own independent
   effective/expiration date window**, all following the same inclusive-both-ends,
   null-tolerant convention already documented in `docs/rules/date-selection.md`'s `R-DATE-002`
   (`<= :pricing-date AND (expire IS NULL OR expire >= :pricing-date)`), applied four times in one
   query (header, account-or-customer, detail, product) — ALL four windows must be simultaneously
   open for a row to qualify.
5. Priority: account scope (9945) tried first; CID scope (9950) tried only
   `IF NOT WS-F-HC-COST-REC-FND` after the account attempt (line 26054-26058) — the same
   account-then-CID fallback shape already seen repeatedly elsewhere in this codebase (freight
   R-FREIGHT-002, delivery R-DELIVERY-002, overhead in `fees-and-adjustments.md`'s cross-refs).
6. Calculation: no cost arithmetic in this rule — pure eligibility/flag determination. **Override
   flag precedence (CONFIRMED, identical logic at both account and CID scope, lines 26127-26133/
   26222-26228):** `IF HC-PROD-OVRD-FLAG = 'Y' OR 'N'` (i.e. the product-level flag is populated
   with a real value, not null/other) `THEN` use `HC-PROD-OVRD-FLAG` as the effective
   "cost overridden" indicator; `ELSE` (product-level flag absent/unpopulated) fall back to
   `HC-GROUP-OVRD-FLAG` (the group-detail-level default). **Product-level override flag takes
   precedence over the group-level default when both exist.**
7. Output: WS-F-HC-COST-REC-FND-SW (eligibility found at all), WS-F-COST-OVERRIDEN-SW (per item
   6's precedence), OMGPR-HC-GROUP-ID (the matched healthcare group's ID)
8. Dates: `HC-PROD-CST-EXPIRE-DATE`, and a defensive sentinel: `IF HC-PROD-CST-EXP-DATE-NI < 0`
   (i.e. the product-cost expiration is null) `THEN MOVE WS-HIGH-DATE TO
   HC-PROD-CST-EXPIRE-DATE` (line 26134-26136) — a null product-cost expiration is explicitly
   normalized to a high/sentinel date value in working storage, distinct from the general
   null-indicator convention in `date-selection.md` (which normally just skips a null date
   entirely); here the null is actively replaced with a concrete "never expires" value, because
   this date is about to be used as a range-bound in R-HC-002's own SELECT (`WHERE ... <=
   :HC-PROD-CST-EXPIRE-DATE`), where a null value could not be used as a comparison bound
   directly.
9. Exclusions/fallbacks: SQLCODE=100 at account scope falls to the CID attempt; SQLCODE=100 at
   CID scope (after account also failed) leaves `WS-F-HC-COST-REC-FND-SW` blank — no healthcare
   override applies at all, and 7190's caller falls straight through to the acquisition/dealer
   cost fallback (R-ACQ-002) without ever running R-HC-002.
10. Errors: fatal DB error #945 (account-scope WHEN OTHER, table `HC_OVRD_GROUP_ACCOUNT`); a
    parallel fatal error exists for the CID-scope WHEN OTHER (table `HC_OVRD_GROUP_CUSTOMER`,
    exact error number not re-transcribed in this pass but confirmed to follow the same
    fatal-abend pattern by direct structural comparison with the account-scope branch)
11. Confidence: CONFIRMED

### R-HC-002 Healthcare price-list selection: highest acquisition cost wins
1. ID/Name: R-HC-002 Healthcare-eligible price-list row selection (highest-acq-cost tie-break)
2. Program/paragraph: 9970-SELECT-HC-COST-VNG03 (26441-26505), calling 9975-CONVERT-VNG03-FIELDS
   (26507-26538) on success
3. Preconditions: `WS-F-HC-COST-REC-FND` (R-HC-001 found an eligible healthcare-override record)
   AND `WS-F-COST-OVERRIDEN-NO` (the effective override flag from R-HC-001 says "not overridden" —
   i.e. this specific product/group combination is eligible to actually USE the healthcare
   price-list mechanism, as opposed to being explicitly excluded from it)
4. Data deps: `SELECT TOP 1 ... FROM VNG03 WHERE I_VENDOR = :OMGPR-I-VENDOR AND I_VND_PRODUCT =
   :OMGPR-I-VND-PRODUCT AND D_VND_PRC_ACTIVE <= :WS-TEMP-D-PRICING AND ((the row was already
   active before the healthcare product-cost window started, and either never expires or is still
   active at that window's start) OR (the row became active during the healthcare product-cost
   window itself, before that window's own expiration)) ORDER BY A_VND_PRC_ACQ_COST DESC,
   D_VND_PRC_ACTIVE ASC` — a genuinely more complex date-eligibility test than the simple
   inclusive-window pattern elsewhere in this codebase: a VNG03 price-list row qualifies if its
   own active/expire window overlaps the healthcare product-cost window in either of two ways
   (already-active-and-still-open-at-window-start, OR became-active-during-the-window).
5. Priority: **CONFIRMED, genuinely counter-intuitive and worth flagging prominently** — among
   all VNG03 rows matching the WHERE clause, the query explicitly orders by
   `A_VND_PRC_ACQ_COST DESC` first (HIGHEST acquisition cost wins, not lowest), with
   `D_VND_PRC_ACTIVE ASC` (earliest active date) only as a tie-breaker when multiple rows share
   the identical highest acquisition cost, and `SELECT TOP 1` takes only that single winning row.
   **This is the opposite selection direction from every other "duplicate row" resolution rule
   documented in this repository** — R-COST-002 (duplicate individual contracts) explicitly
   selects the LOWEST cost; this healthcare mechanism explicitly selects the HIGHEST acquisition
   cost. Do not assume "lowest cost wins" is a universal convention in this codebase — it is not.
6. Calculation: on success, `9975-CONVERT-VNG03-FIELDS` applies the identical UOM-conversion
   formula already documented for `R-ACQ-001` (`OMGPR-A-VND-PRC-ACQ-COST`/`-DEALER`/`-BEST-QTY`/
   `-LST-HOSP`/`-LST-DOC` each `ROUNDED = (VNG03-value * CONVERT-UP-UMF) / CONVERT-DOWN-UMF`) —
   **this OVERWRITES the same `OMGPR-A-VND-PRC-DEALER` field that `7065-PRO-DEAL-COST-010`
   (R-ACQ-001) already populated earlier in the same pricing pass**, with the healthcare-eligible
   row's values instead of the plain price-list lookup's values. The two paragraphs are
   byte-for-byte identical in their conversion formula (independently re-verified by direct
   comparison), differing only in which VNG03 row was SELECTed beforehand.
7. Output: OMGPR-A-VND-PRC-ACQ-COST, OMGPR-A-VND-PRC-DEALER, OMGPR-A-VND-PRC-BEST-QTY,
   OMGPR-A-VND-PRC-LST-HOSP, OMGPR-A-VND-PRC-LST-DOC, OMGPR-C-VND-PRC-UM, OMGPR-C-VND-PRC-LEVEL,
   OMGPR-D-VND-PRC-LIST-EFF, OMGPR-C-CUS-SELL-PRC-UOM (all overwritten in place — this is the
   value that 7190's subsequent `MOVE OMGPR-A-VND-PRC-DEALER TO OMGPR-A-CNT-LN-UNIT-COST`
   fallback step then uses as the final line cost, per R-ACQ-002)
8. Dates: OMGPR-D-HC-COST-EFF is separately set (by 7190 itself, immediately after this
   paragraph returns) from `VNG03-D-VND-PRC-LIST-EFF` — the healthcare-selected row's own
   effective date, distinct from the general closest-expiration-date mechanism in
   `date-selection.md`
9. Exclusions/fallbacks: SQLCODE=100 (no matching VNG03 row within the healthcare window) and
   SQLCODE=-811 (the same "duplicate row exists, tolerate and continue" reuse pattern already
   seen elsewhere in this codebase, e.g. `fees-and-adjustments.md` cross-references) are both
   treated as non-fatal `CONTINUE` — in the -811 case specifically, `SELECT TOP 1` combined with
   `ORDER BY` should make true duplicates impossible to begin with, so this WHEN branch existing
   at all suggests defensive coding against a scenario the author considered possible but did not
   expect in practice (INFERRED, not stated in any comment)
10. Errors: fatal DB error #949 (WHEN OTHER, table VNG03)
11. Confidence: CONFIRMED for the SQL ordering/selection logic and the field-overwrite mechanism
    (both independently re-verified by direct source comparison). INFERRED that the -811
    tolerance is defensive/unreachable given the `TOP 1` clause, not independently proven.

---

## 5. Acquisition/dealer cost and price-list fallback

# Acquisition/dealer cost and price-list fallback — extracted rules (source notes)

Program/paragraph: A6U01.CBL `7065-PRO-DEAL-COST-010` (9128-9173, UOM conversion of whatever
VNG03 row is already staged) -> `7110-CVT-PRC-LST-UM-010` (called first by 7065, UOM-conversion-
factor lookup, not independently re-traced this pass) and, separately, `7105-SEL-PRC-LST-010`
(10807-10846, the price-list row SELECTION itself — this is the paragraph left BLOCKED in
`fees-and-adjustments.md`'s `R-REBATE-000`; resolved in this pass) -> on miss,
`7575-SEL-MAX-PRCLST-010` (15930-16072, three-step MAX-date/MAX-level resolution). Fallback
decision itself lives in `7190-PRO-COST-AMTS-010` (12309-12355, already the entry point for
R-COST-000/R-HC-001).

### R-ACQ-001 Price-list row selection: direct match, else latest-effective-date/highest-level
1. ID/Name: R-ACQ-001 Vendor price-list (VNG03) row selection
2. Program/paragraph: 7105-SEL-PRC-LST-010 (10807-10846), falling back to
   7575-SEL-MAX-PRCLST-010 (15930-16072) on miss
3. Preconditions: called wherever a specific vendor price-list row is needed — CONFIRMED call
   sites include `fees-and-adjustments.md`'s `R-REBATE-000` (rebate base-cost derivation, when
   `OMGPR-F-VEND-BST-CST-RBT='Y'` and the contract's own price level isn't already `'01'`) and,
   separately, the general acquisition/dealer cost path documented here (R-ACQ-002)
4. Data deps: 7105's own direct lookup (`9200-SQL-SELECT-010`, not re-transcribed to WHERE-clause
   depth this pass) is tried first; on `SQLCODE=100` it falls to 7575's three-chained-SELECT
   resolution (`9300`->`9305`->`9310-SQL-SELECT-010`, each feeding the next)
5. Priority (CONFIRMED, header comment on both 7105 and 7575, matching the code): **a direct
   match is tried first; if none, resolve by (a) the MAXIMUM (most recent) effective date not
   exceeding the pricing date — "USED MAX ON THE EFFECTIVE DATE BECAUSE THE DATES COULD [OVER]LAP.
   THE EFFECTIVE DATE MUST BE UNIQUE SO WILL GET ONLY ONE WITH THE HIGHEST EFFECTIVE DATE FOR THE
   CURRENT DATE," then (b) the MAXIMUM price level among rows sharing that same max effective
   date, used as a tie-break/default ("USED MAX ON THE PRICE LEVEL TO GET THE HIGHEST NUMBER TO
   BE USED AS THE DEFAULT"), then (c) fetch the actual row keyed by both resolved values
   together.** **This is a THIRD distinct duplicate/candidate-resolution convention in this
   codebase**, alongside R-COST-002's "lowest cost wins" (individual contract duplicates) and
   R-HC-002's "highest acquisition cost wins" (healthcare price-list rows) — here it is neither
   cost-based: it is date-recency-based (latest effective date), with price-level-recency as the
   secondary tie-break. **Do not assume any one of these three conventions generalizes to the
   others** — each is independently coded for its own specific duplicate scenario.
6. Calculation: no cost arithmetic in this rule itself — pure row selection, chained across up to
   three sequential SELECTs when the direct match misses
7. Output: the VNG03 host-variable structure (C_VND_PRC_UM, D_VND_PRC_LIST_EFF, D_VND_PRC_ACTIVE,
   D_VND_PRC_EXPIRE, A_VND_PRC_DEALER, A_VND_PRC_ACQ_COST, A_VND_PRC_BEST_QTY, A_VND_PRC_LST_HOSP,
   A_VND_PRC_LST_DOC, C_VND_PRC_LEVEL — the same field set R-HC-002's healthcare-specific lookup
   also populates, consumed next by R-ACQ-002's UOM conversion)
8. Dates: D_VND_PRC_LIST_EFF/D_VND_PRC_ACTIVE/D_VND_PRC_EXPIRE — not independently re-verified for
   inclusive-boundary convention at every one of the three chained SELECTs in 7575 (deferred to
   the T005 SQL CSV), though 7105's primary direct-match attempt and R-HC-002's parallel VNG03
   lookup both follow the standard convention already documented in `date-selection.md`
9. Exclusions/fallbacks: **CONFIRMED, a genuine severity divergence worth flagging** — if 7575's
   final row-fetch (step (c), `9310-SQL-SELECT-010`) returns `SQLCODE=100` DESPITE steps (a) and
   (b) having already successfully resolved a max-date and max-level, this is treated as a
   **hard fatal error** (`#128 "MAX PRICE LIST NOT FOUND"`, `GO TO 0020-EXIT-PRICER`) rather than
   a soft "no price list available" fallthrough — once the aggregate values are known to exist,
   the code assumes the corresponding detail row MUST also exist, and aborts the whole pricer if
   that assumption is violated. Similarly, `SQLCODE=100` on step (a) itself (`#130`) is also
   fatal. **A price-list lookup failure in this fallback path is NOT tolerated the way a missing
   cost contract is** — this reflects that a price list is treated as a required baseline for
   every vendor+product combination, never optional.
10. Errors: fatal DB error #45 (7105's own direct-match WHEN OTHER); fatal #128/#130 (7575's
    "aggregate found but detail row missing" cases, item 9); fatal #129 (7575's own WHEN OTHER on
    the max-level SELECT)
11. Confidence: CONFIRMED for the chained resolution structure and its fatal-error posture
    (independently read). INFERRED/deferred for the exact WHERE-clause text of
    9200/9300/9305/9310 (not individually re-transcribed, consistent with this document's
    economy of depth applied to the lowest-value transcription work).

### R-ACQ-002 Acquisition/dealer cost as final fallback when no contract is found
1. ID/Name: R-ACQ-002 No-cost-contract fallback to price-list dealer cost
2. Program/paragraph: 7190-PRO-COST-AMTS-010, lines 12309-12355 (the same paragraph documenting
   R-COST-000's dispatch and R-HC-001's healthcare-override gate)
3. Preconditions: `WS-COST-CONT-NOT-FND` — reached only after the ENTIRE contract-selection
   cascade (individual R-COST-001/002, group/account/customer/parent R-COST-003/004/005) and the
   healthcare-override mechanism (R-HC-001/002) have all been attempted, in that order, and none
   produced a usable cost
4. Data deps: `OMGPR-A-VND-PRC-DEALER` — populated either by the plain acquisition-cost UOM
   conversion (`7065-PRO-DEAL-COST-010`, called unconditionally at the very start of this same
   paragraph, before any contract search even begins, lines 12314-12315) or, if the healthcare
   path ran and matched, OVERWRITTEN by R-HC-002's `9975-CONVERT-VNG03-FIELDS` instead — **this
   fallback step does not know or care which of the two populated it**; it simply uses whatever
   `OMGPR-A-VND-PRC-DEALER` currently holds at this point in the paragraph.
5. Priority: this is the terminal fallback — CONFIRMED, there is no further fallback beyond this
   within `A6U01.CBL`'s cost-determination logic; if `7065`'s own underlying `7105`/`7575`
   price-list lookup (R-ACQ-001) also failed, that failure would already have aborted the pricer
   fatally before execution ever reaches this point (per R-ACQ-001 item 9), so by the time this
   fallback runs, a price-list-derived cost is guaranteed to exist.
6. Calculation: no new arithmetic — direct MOVEs: `OMGPR-A-CNT-LN-UNIT-COST =
   OMGPR-A-VND-PRC-DEALER`, `OMGPR-C-CNT-LN-UM = OMGPR-C-VND-PRC-UM` (the price list's own UOM
   becomes the contract-line UOM for downstream processing, since there is no contract line to
   supply one), `OMGPR-F-JIT-EXEMPT = 'N'`, `OMGPR-F-FRT-EXEMPT = 'N'` (both reset to
   not-exempt — CONFIRMED by in-line comment: "SET THE JIT AND FRT EXEMPT FLAGS TO 'N' BECAUSE
   THERE IS NO COST CONTRACT" — these two exemption flags are normally populated FROM a cost
   contract's own line-level flags, so when there is no contract to supply them, the code
   explicitly defaults to "not exempt" rather than leaving them at whatever stale value they
   might otherwise hold).
7. Output: OMGPR-A-CNT-LN-UNIT-COST, OMGPR-C-CNT-LN-UM, OMGPR-F-JIT-EXEMPT, OMGPR-F-FRT-EXEMPT —
   from this point forward, the rest of the pricer (freight, JIT, surcharge, etc., all of
   `fees-and-adjustments.md`) treats this line exactly as if `OMGPR-A-CNT-LN-UNIT-COST` had come
   from a real contract, with no further special-casing for the fact that it is actually the
   price-list acquisition/dealer cost.
8. Dates: none new in this fallback step itself (whichever of R-ACQ-001/R-HC-002 populated the
   underlying price-list row already contributed its own dates per those rules)
9. Exclusions/fallbacks: this rule IS the final fallback — there is no rule below it
10. Errors: none in this fragment (all error paths belong to the upstream lookups this fallback
    depends on)
11. Confidence: CONFIRMED

---

## What was scoped out of this pass

- **`0340-PRO-CUG13-010`** and the vendor-override candidate-row paragraphs (R-COST-004) — their
  existence and role (per-candidate-row check inside a cursor loop) is confirmed from the calling
  code, internal WHERE-clause/matching detail is not.
- **`7315-SEL-GRP-CNT-010`'s own SELECT/formula detail** (R-COST-005) — confirmed as the paragraph
  that actually populates the group-cost-contract fields at both child and ancestor BG levels,
  not re-transcribed to formula depth.
- **The customer-side equivalents of `0310`/`0315`/`0320`** (i.e. `0330-SEL-CUST-PRD-CAT-010` and
  its siblings) — confirmed structurally parallel to the account side at the `0275`/`0280` level
  (independently re-read and verified identical in control flow), not re-verified line-for-line
  at the deeper `0315`-equivalent level.
- **`9200`/`9300`/`9305`/`9310-SQL-SELECT-010`'s exact WHERE-clause text** (R-ACQ-001) — the
  chained MAX-date/MAX-level resolution's control flow and fatal-error posture are confirmed;
  the literal SQL predicates are deferred to the T005 SQL inventory CSV.
- **`7110-CVT-PRC-LST-UM-010`** (the UOM-conversion-factor lookup feeding both `7065` and the
  rebate/healthcare conversion formulas) — its existence and role are confirmed by every caller
  using its output (`WS-CA-CONVERT-UP-UMF`/`WS-CA-CONVERT-DOWN-UMF`) identically; its own internal
  logic for deriving those factors was not read.

## Assumptions

1. `CCG27-F-CNT-PRIORITY`'s priority-mode vs. non-priority-mode duplicate-contract comparison
   (R-COST-002 item 5) is assumed to always produce the same final answer as the non-priority
   path — not proven for every possible input, only observed to use an identical comparison
   formula staged differently.
2. The customer-side paragraphs mirroring `0275`/`0310`/`0315`/`0320` are assumed to be exact
   structural parallels of the account-side ones at every level, based on confirmed parity at the
   `0275`/`0280` level (R-COST-004) — not independently re-verified at the deeper `0315`-equivalent
   level for the customer side.

## Blockers

1. `0340-PRO-CUG13-010` and the vendor-override candidate-row paragraphs — needed to know the
   exact matching criteria within a multi-row override cursor loop.
2. `7315-SEL-GRP-CNT-010`'s own formula — needed to confirm exactly which contract-line fields a
   group cost contract populates, versus what this document inferred from the callers' MOVE
   targets.
3. Ten-plus `CCGnn`/`CUGnn`/`BGGnn`/`VNGnn` DCLGENs are not supplied in `upload/` (full list in
   this document's header) — every field sourced from them is individually marked BLOCKED or
   INFERRED at point of use rather than assumed.
4. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set;
   nothing here was validated by actually running the pricer.


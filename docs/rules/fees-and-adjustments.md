# A6U01 — Fees and Adjustments Decision Tables (T009)

**Scope:** Ordered decision-table extraction, per `CLAUDE.md`'s required 11-element rule format,
for all 12 fee/adjustment areas named in T009's acceptance criteria: rebates, freight, JIT,
PANDAC, SurgiTrak, low-UOM, break-bulk, label/application, delivery, surcharge, distribution, and
markup. Evidence shows label/application has no independent computation (fully subsumed by JIT)
and distribution/markup are one combined mechanism, not two — both findings are documented in
their respective sections below rather than silently merged or split to force a 1:1 match with the
task's wording.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL` (26,657 lines) and `OMGPR.CPY`.
Line citations refer to `A6U01.CBL` unless a different file is named. No DCLGEN copybooks for
`VNG01`, `VNG03`, `VNG16`, `VNG17`, `VNG19`, `VNG20`, `VNG31`, `VNG32`, `VNG33`, `VNG34`, `VNG35`,
`VNG37`, `VNG40`, `BGG25`, `ING01`, `CUS120`, `CUP120` are supplied in `upload/` — every field
sourced from these is marked BLOCKED or INFERRED at the point it is used, never assumed.

**Updated — second upload batch (2026-07-20 pass):** `VNG02.CPY`, `VNG05.CPY` (now full DCLGEN,
previously usage-only), and — not part of this document's original header list but now supplying
detail used throughout the JIT/LUOM/Freight sections below — `CUR120.CPY`, `CUG53.CPY`,
`CUG17.CPY`, `BGG23.CPY`, `BGG24.CPY` are newly supplied and read in full
(`docs/cobol-analysis/program-inventory.md` §4.2). See R-JIT-001/002/003/006, R-LUOM-001/002/003,
`R-FREIGHT-006`, and `R-PANDAC-000` below for the resulting field-level upgrades. `CUP120`/`CUS120`
(the programs that populate `CUR120`) remain unsupplied — this batch confirms `CUR120`'s data
*shape*, not the *computation* behind its JIT/CMF fee values.

**Depends on (per `tasks.md`):** T001 (`docs/cobol-analysis/program-inventory.md`, complete) and
T005 (`docs/cobol-analysis/sql-query-inventory.csv` + `db2-table-inventory.md`, complete). Table
names cited as "T005-confirmed" were cross-checked against the SQL inventory CSV built for T005
rather than re-derived from scratch.

**Provenance:** Two of the ten sections below (JIT, Low-UOM/Break-bulk) carry forward prior
decision-table work built against this same `A6U01.CBL` (MD5-verified identical file) in an
earlier, differently-scoped pass; they were re-verified rather than re-derived, and one prior
blocker (`7707-CHECK-PANDAC-ACCT-FLAG`) was resolved during this pass's PANDAC extraction. The
remaining eight sections (Rebates, Freight, PANDAC, SurgiTrak, Label/Application,
Delivery, Surcharge, Distribution/Markup) are new extractions built for this task.

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Cross-cutting findings

Several patterns recur across multiple fee areas, independently confirmed each time. Recorded
once here rather than repeated verbatim in every section; each section still cites the specific
lines where it applies.

1. **The custom -> group-sanctioned -> division-01-non-sanctioned -> individual -> non-contract
   classification cascade appears at least three times**, independently coded, in: freight
   exemption (`R-FREIGHT-006`), the Gross-Margin markup classification (`R-DISTMKP-001`), and
   structurally (a different axis — account/CID/division/corp rather than
   custom/group/individual) in the surcharge lookup (`R-SURCHARGE-001`). Same five-way shape,
   same field names (`CCG09-I-RPT-GRP`, `OMGPR-F-GRP-CNT-FEES`, `BGG01-I-DIVISION`,
   `WS-INDIV-CNT`), never factored into a shared paragraph. A reimplementation could
   legitimately unify these, but must preserve that one variant (freight) defaults an unmatched
   case to "not exempt" while another (markup) treats an unmatched case as a fatal error (#602) —
   they are not interchangeable despite the shared shape.
2. **"Last write wins" sequential-overwrite hazard**, not gated by mutual exclusivity, appears in
   `R-REBATE-001/002/003` (contract rebate options 1-3) and is flagged as a risk in
   `R-DISTMKP-001` (Stock/Usage classification potentially re-running after the Cost-Contract
   classification). Any reimplementation must replicate "whichever condition's block executes
   last in source order wins," not "first match wins" or "most specific match wins."
3. **Billing-frequency fold-in is not a single consistent pattern** — SurgiTrak (`R-SURGITRAK-002`)
   and Surcharge (`R-SURCHARGE-002`) both *add* a separately-computed fee into the sell total
   unless the billing frequency is monthly; Distribution/Markup (`R-DISTMKP-002`) does the
   opposite — the margin is already embedded in the sell price, and a monthly billing frequency
   *subtracts it back out*. Same `OMGPR-BILLING-FRQ-xx`/monthly-auto/monthly-manual field shape,
   opposite direction of adjustment.
4. **Unmatched/unrecognized code values are usually silent no-ops, not diagnostics** — confirmed
   for JIT service-fee type (`R-JIT-001`), LUOM/break-bulk account-vs-group switches
   (`R-LUOM-003`), SurgiTrak fee type (`R-SURGITRAK-001`), and the LUOM UOM-designator resolution
   (`R-LUOM-002`) — but this is not universal: the markup classification's unmatched
   cost-contract case (`R-DISTMKP-001` item 5) is a hard fatal error, and several freight/PANDAC/
   delivery SQL paths abend the whole pricer (`GO TO 0020-EXIT-PRICER`) on an unexpected
   `SQLCODE`. Do not assume "unmatched code" behaves consistently across this codebase without
   checking the specific rule.
5. **`WHEN OTHER` (unexpected `SQLCODE`) severity is inconsistent across fee areas.** Freight,
   PANDAC, delivery, SurgiTrak, and the JIT/LUOM chain all abend the entire pricer
   (`OMGPR-F-PRICER-ERROR='Y'`, `GO TO 0020-EXIT-PRICER`) on an unexpected SQLCODE. Surcharge
   (`R-SURCHARGE-001`/`002`) is the sole confirmed exception — its `WHEN OTHER` branches set a
   local failure flag and return normally, silently skipping just the surcharge for that line
   while the rest of the pricer run continues. Flagged for product/business confirmation before
   assuming this asymmetry is intentional.
6. **In-source comments can describe removed or superseded behavior.** `R-PANDAC-000` documents a
   case where the live code's control flow was changed (an early-exit `GO TO` was removed) but
   the surrounding block comments were not updated to match — the disabled prior implementation
   is still visible immediately above the live code, letting this be confirmed rather than merely
   suspected. Do not treat comments as authoritative over directly-read control flow anywhere in
   this codebase.
7. **Rounding is not applied uniformly.** Nearly every fee formula in this document uses
   `COMPUTE ... ROUNDED`; Surcharge's amount and fold-in computations (`R-SURCHARGE-002`) are the
   one confirmed exception, using plain truncating `COMPUTE` with no `ROUNDED` keyword. Preserve
   this exactly — do not "fix" it to `ROUNDED` by assumption of consistency with the other nine
   fee areas.
8. **2022-era fee-component system ("CMF"/`CUTFEE`/`CUTPRICE_COMPONENT`) coexists with, and in
   places silently supersedes, legacy 1990s-era percent/rate paragraphs**, confirmed for JIT
   break-bulk/apply-label (`R-JIT-004`), the LUOM/break-bulk 2022 path (`R-LUOM-005`), and PANDAC's
   specific-ship-to lookup order (`R-PANDAC-001`/`003`). This is a live, multi-decade layering, not
   a completed migration — both systems must be modeled for full fidelity, not just the newer one.

---

## 1. Rebates

# Rebates — extracted rules (source notes for fees-and-adjustments.md)

Program/paragraph: A6U01.CBL, `7070-PRO-REBT-AMTS-010` (lines 9176-9370), called from
`0030-PROCESS-COST` (line 2544) which is reached only for pricing modes DEFAULT/PRICE/COSTONLY
(not SELLONLY, not JIT-ON-COST — see EVALUATE TRUE at 2476-2498).

Precondition (gate): `WS-COST-CONT-FND` = 'Y' (cost contract found) wraps the entire paragraph
body (line 9183 `IF WS-COST-CONT-FND ... END-IF` at 9366). If no cost contract was found, no
rebate fields are touched (they retain their prior/initialized values).

Skip condition (CONFIRMED, NK0611 change): caller skips the PERFORM entirely when
`OMGPR-C-CNT-ENTRY-METHOD` = '06' (`OMGPR-CNT-ENTRY-FIXED-REB`, "FIX REBATE AMOUNT") or '08'
("SUGGESTED SELL WITH FIXED REBATE") — these entry methods carry an already-fixed rebate value
from the contract itself (per comment block ~10125/10133), so no derived-rebate calculation runs.

### R-REBATE-000 Rebate base cost (WS-REBATE-DEAL-ACQ)
1. ID/Name: R-REBATE-000 Rebate base cost selection
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9183-9214
3. Preconditions: WS-COST-CONT-FND = 'Y'
4. Data deps: OMGPR-F-VEND-BST-CST-RBT (X1), OMGPR-C-VND-PRC-LEVEL (X2), OMGPR-A-VND-PRC-DEALER
   (S9(7)V9(8)), VNG03-A-VND-PRC-DEALER (via 7105-SEL-PRC-LST-010 — VNG03 DCLGEN not supplied,
   BLOCKED), WS-CA-CONVERT-UP-UMF/WS-CA-CONVERT-DOWN-UMF (via 7110-CVT-PRC-LST-UM-010)
5. Priority: runs first, feeds all four rebate options below
6. Calculation: IF VEND-BST-CST-RBT='Y': IF VND-PRC-LEVEL='01' use OMGPR-A-VND-PRC-DEALER as-is;
   ELSE set VNG03-C-VND-PRC-LEVEL='01', PERFORM 7105-SEL-PRC-LST-010 (looks up level-01 vendor
   price — SQL/paragraph body not yet read, BLOCKED on internals) THEN 7110-CVT-PRC-LST-UM-010
   (UOM conversion) THEN COMPUTE WS-REBATE-DEAL-ACQ ROUNDED = (VNG03-A-VND-PRC-DEALER *
   CONVERT-UP-UMF) / CONVERT-DOWN-UMF.
   IF VEND-BST-CST-RBT='N': WS-REBATE-DEAL-ACQ = OMGPR-A-VND-PRC-DEALER directly (division level,
   no level-01 lookup/conversion).
7. Output: WS-REBATE-DEAL-ACQ (working storage only) is also saved to OMGPR-A-REBATE-COST
   (S9(5)V9(4)) for traceability/output.
8. Dates: none directly (upstream vendor-price selection may be date-scoped, not visible here)
9. Exclusions/fallbacks: none — always computed once WS-COST-CONT-FND is true
10. Errors: none in this paragraph; any SQL failure would surface inside 7105-SEL-PRC-LST-010
    (not yet analyzed for this pass)
11. Confidence: CONFIRMED for the branching/formula; INFERRED/BLOCKED for VNG03 host-structure
    internals (DCLGEN not in upload/)

### R-REBATE-001 Standard Contract Rebate (Option #1)
1. ID/Name: R-REBATE-001 Standard contract rebate
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9215-9238
3. Preconditions: WS-COST-CONT-FND='Y' AND (OMGPR-P-CNT-LN-BROKER=0 OR OMGPR-A-CNT-LN-SUGG-SELL=0
   OR OMGPR-P-CNT-LN-COST-PLUS=0) — algebraically simplified from the source's redundant
   `(broker=0 OR sugg-sell=0) OR (cost-plus=0 OR sugg-sell=0)`
4. Data deps: WS-REBATE-DEAL-ACQ (R-REBATE-000), OMGPR-A-CNT-LN-UNIT-COST (S9(5)V9(8)),
   VNG01-F-VEND-NET-CST-RBT and VNG01-P-VEND-NET-CST-RBT and VNG01-F-NEGATIVE-REBATE — VNG01
   DCLGEN NOT supplied in upload/, BLOCKED on exact PIC/type, but usage confirms
   VNG01-F-VEND-NET-CST-RBT is a Y/N flag and VNG01-P-VEND-NET-CST-RBT is a percentage-shaped
   multiplier field
5. Priority: **CRITICAL FINDING (CONFIRMED)** — this is NOT mutually exclusive with R-REBATE-002
   or R-REBATE-003. All three are independent sequential `IF` blocks (no ELSE chaining, no GO TO
   between them) that all execute in source order 001 -> 002 -> 003 whenever their own condition
   is true, each unconditionally overwriting OMGPR-A-CUR-CONT-REBATE via COMPUTE. Net effect:
   **R-REBATE-003 (Cost Plus) silently wins over R-REBATE-002 (Broker) which wins over
   R-REBATE-001 (Standard)** whenever more than one condition is simultaneously true, because
   whichever option's block executes LAST is the one whose COMPUTE result survives. The source's
   own numbering ("OPTION #1/#2/#3") does NOT reflect actual precedence — precedence is the
   reverse of source-comment numbering, driven purely by physical code order. This must be
   preserved exactly in any reimplementation (last-applicable-wins, not first-match-wins).
6. Calculation: COMPUTE OMGPR-A-CUR-CONT-REBATE ROUNDED = WS-REBATE-DEAL-ACQ -
   OMGPR-A-CNT-LN-UNIT-COST. IF VNG01-F-VEND-NET-CST-RBT='Y': CONT-REBATE = CONT-REBATE *
   (1 - VNG01-P-VEND-NET-CST-RBT) [BLOCKED: VNG01 scale/decimal placement unverified]. Then floor:
   IF CONT-REBATE < 0: IF VNG01-F-NEGATIVE-REBATE='Y' THEN leave negative (CONTINUE) ELSE zero it.
7. Output: OMGPR-A-CUR-CONT-REBATE (S9(5)V9(4))
8. Dates: none
9. Exclusions/fallbacks: negative-rebate floor is itself conditionally waived by
   VNG01-F-NEGATIVE-REBATE='Y' (vendor-level opt-in to allow negative rebates)
10. Errors: none in-paragraph
11. Confidence: CONFIRMED for control flow/formula/precedence; BLOCKED for VNG01 field
    scale/type (DCLGEN not supplied)

### R-REBATE-002 Negotiated Sell Rebate with Broker (Option #2)
1. ID/Name: R-REBATE-002 Broker-negotiated sell rebate
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9239-9264
3. Preconditions: WS-COST-CONT-FND='Y' AND OMGPR-P-CNT-LN-BROKER > 0 AND
   OMGPR-A-CNT-LN-SUGG-SELL > 0
4. Data deps: OMGPR-A-CNT-LN-SUGG-SELL (S9(5)V9(8)), OMGPR-A-CNT-LN-UNIT-COST, WS-REBATE-DEAL-ACQ,
   VNG01-F-NEGATIVE-REBATE (BLOCKED, see R-REBATE-001)
5. Priority: see R-REBATE-001 point 5 — overwrites R-REBATE-001's result when both fire; is
   itself overwritten by R-REBATE-003 if that also fires (source order 001, 002, 003)
6. Calculation: OMGPR-A-CUR-VND-DIST-REB ROUNDED = OMGPR-A-CNT-LN-SUGG-SELL -
   OMGPR-A-CNT-LN-UNIT-COST. OMGPR-A-CUR-SELL-CST-DIF ROUNDED = WS-REBATE-DEAL-ACQ -
   OMGPR-A-CNT-LN-SUGG-SELL. OMGPR-A-CUR-CONT-REBATE ROUNDED = VND-DIST-REB + SELL-CST-DIF.
   Same negative-rebate floor/override as R-REBATE-001 (VNG01-F-NEGATIVE-REBATE gated).
7. Output: OMGPR-A-CUR-VND-DIST-REB, OMGPR-A-CUR-SELL-CST-DIF, OMGPR-A-CUR-CONT-REBATE (all
   S9(5)V9(4))
8. Dates: none
9. Exclusions/fallbacks: negative floor waivable per VNG01-F-NEGATIVE-REBATE (BLOCKED detail)
10. Errors: none in-paragraph
11. Confidence: CONFIRMED control flow/formula; BLOCKED VNG01 field detail

### R-REBATE-003 Negotiated Sell Rebate with Cost Plus (Option #3)
1. ID/Name: R-REBATE-003 Cost-plus-negotiated sell rebate
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9265-9290
3. Preconditions: WS-COST-CONT-FND='Y' AND OMGPR-P-CNT-LN-COST-PLUS > 0 AND
   OMGPR-A-CNT-LN-SUGG-SELL > 0
4. Data deps: same shape as R-REBATE-002 (SUGG-SELL, UNIT-COST, REBATE-DEAL-ACQ,
   VNG01-F-NEGATIVE-REBATE BLOCKED)
5. Priority: **highest effective priority of the three options** — executes last in source order,
   so its COMPUTE result is the one that survives OMGPR-A-CUR-CONT-REBATE if this condition and
   either/both of R-REBATE-001/002's conditions are simultaneously true. See R-REBATE-001 point 5.
6. Calculation: identical formula shape to R-REBATE-002 (VND-DIST-REB = SUGG-SELL - UNIT-COST;
   SELL-CST-DIF = REBATE-DEAL-ACQ - SUGG-SELL; CONT-REBATE = VND-DIST-REB + SELL-CST-DIF), gated
   on COST-PLUS>0 instead of BROKER>0. Same negative-rebate floor/override.
7. Output: OMGPR-A-CUR-VND-DIST-REB, OMGPR-A-CUR-SELL-CST-DIF, OMGPR-A-CUR-CONT-REBATE
8. Dates: none
9. Exclusions/fallbacks: negative floor waivable per VNG01-F-NEGATIVE-REBATE (BLOCKED detail)
10. Errors: none in-paragraph
11. Confidence: CONFIRMED control flow/formula; BLOCKED VNG01 field detail

### R-REBATE-004 Price Protection (Option #4)
1. ID/Name: R-REBATE-004 Price protection amount
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9291-9355
3. Preconditions: WS-COST-CONT-FND='Y' AND OMGPR-A-CNT-LN-PROT-ACQ <> 0 AND pricing date
   (OMGPR-D-PRICING) reformatted-compare >= OMGPR-D-CNT-PROT-START AND
   WS-REBATE-DEAL-ACQ <> OMGPR-A-CNT-LN-PROT-ACQ AND reformatted pricing date <=
   OMGPR-D-CNT-PROT-END (this last check is a NESTED IF, i.e. the <= END-date test only runs if
   the >= START-date-AND-neq test already passed — so a pricing date before START, or one on/after
   START but with REBATE-DEAL-ACQ already equal to PROT-ACQ, short-circuits before the END check
   is ever evaluated)
4. Data deps: OMGPR-D-PRICING (X10), OMGPR-D-CNT-PROT-START (X10), OMGPR-D-CNT-PROT-END (X10),
   OMGPR-A-VND-PRC-ACQ-COST (S9(7)V9(8)), OMGPR-A-CNT-LN-PROT-ACQ (S9(5)V9(8))
5. Priority: independent of R-REBATE-001/002/003 — writes a separate output field, does not
   compete for OMGPR-A-CUR-CONT-REBATE
6. Calculation: dates are reformatted from a 10-char field via substring reassembly (positions
   7-10 -> compare(1:4) [year], 1-2 -> compare(5:2) [month], 4-2 -> compare(7:2) [day], i.e.
   source format is interpreted as MM?DD?YYYY-shaped with 1-char separators at positions 3 and 6,
   converted to a YYYYMMDD-comparable string) before the range compare. If in range and changed:
   COMPUTE OMGPR-A-CUR-PRICE-PROT ROUNDED = OMGPR-A-VND-PRC-ACQ-COST - OMGPR-A-CNT-LN-PROT-ACQ.
   Comment explicitly states this **may be negative — no floor is applied** (unlike options 1-3).
7. Output: OMGPR-A-CUR-PRICE-PROT (S9(5)V9(4)). **NOTE (CONFIRMED, notable): this field is NOT
   folded into OMGPR-A-CUR-TOTAL-REBATE** — the total-rebate computation immediately following
   (R-REBATE-005) only sums OMGPR-A-CUR-CONT-REBATE. Price protection is reported as a distinct,
   separate output despite being computed under the "REBATE AMOUNTS" paragraph umbrella.
8. Dates: gated by OMGPR-D-CNT-PROT-START/END window; contract must already be effective (this
   paragraph does not itself check general contract effective/expiration dates, only the
   protection-specific window)
9. Exclusions/fallbacks: skipped entirely if PROT-ACQ=0 (CONTINUE, no-op — comment explains this
   field is loaded "the night before the contract becomes effective" so a zero means not yet
   populated / not applicable)
10. Errors: none in-paragraph
11. Confidence: CONFIRMED (date-reformatting logic and formula both directly read); the exact
    source date layout assumption (MM?DD?YYYY with separators) is INFERRED from the substring
    offsets since no copybook comment states the literal external format

### R-REBATE-005 Total Rebate Assembly
1. ID/Name: R-REBATE-005 Total rebate amount
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9356-9358
3. Preconditions: WS-COST-CONT-FND='Y' (unconditional once inside the outer IF)
4. Data deps: OMGPR-A-CUR-CONT-REBATE (result of whichever of R-REBATE-001/002/003 last executed)
5. Priority: runs after all of R-REBATE-001..004
6. Calculation: COMPUTE OMGPR-A-CUR-TOTAL-REBATE ROUNDED = OMGPR-A-CUR-CONT-REBATE (a straight
   copy/round, not a sum of multiple rebate types despite the "TOTAL" name)
7. Output: OMGPR-A-CUR-TOTAL-REBATE (S9(5)V9(4))
8. Dates: none
9. Exclusions/fallbacks: none
10. Errors: none
11. Confidence: CONFIRMED

### R-REBATE-006 Max Rebate Cap
1. ID/Name: R-REBATE-006 Contract max-rebate cap
2. Program/paragraph: A6U01.CBL 7070-PRO-REBT-AMTS-010, lines 9359-9365
3. Preconditions: WS-COST-CONT-FND='Y' AND OMGPR-A-CNT-LN-MAX-REBT > 0 AND OMGPR-A-CNT-LN-MAX-REBT
   < OMGPR-A-CUR-TOTAL-REBATE
4. Data deps: OMGPR-A-CNT-LN-MAX-REBT (S9(5)V9(8))
5. Priority: runs last, after R-REBATE-005
6. Calculation: IF MAX-REBT > 0 AND MAX-REBT < TOTAL-REBATE THEN MOVE MAX-REBT TO TOTAL-REBATE
7. Output: OMGPR-A-CUR-TOTAL-REBATE (overwritten with the cap when triggered)
8. Dates: none
9. Exclusions/fallbacks: **MAX-REBT = 0 means "no cap configured", not "cap at zero"** — the
   `> 0` guard means a contract with no max-rebate value set never has its rebate suppressed to
   zero by this rule (a naive re-implementation using `>=0` or omitting the zero-guard would
   silently zero every uncapped contract's rebate — flagged explicitly as a footgun)
10. Errors: none
11. Confidence: CONFIRMED

---

## 2. Freight

# Freight — extracted rules (source notes for fees-and-adjustments.md)

Programs/paragraphs (A6U01.CBL unless noted): entry `7200-PRO-ADJ-COST-010` (exemption gate,
lines 12644-12723) -> `0205-PRO-FREIGHT-COST-010` (waterfall dispatcher, 4594-4737) ->
`9650-PRO-ACT-VEND-FREIGHT-CHG` (23811-23878), `9652-PRO-CID-VEND-FREIGHT-CHG` (23880-23947),
`7265-PRO-NEW-FRT-010` (13927-13953), `7270-PRO-ACCT-FREIGHT-010` (13955-14005),
`0285-PRO-BG-FREIGHT-010`/`0286-PRO-BG-FREIGHT-01-010` (6833-6902),
`7280-SEL-IN-FRT-BG-VEN-010` (14060-14165), `7820-PRO-CORP-FRT-010` (18715-18732),
`7895-PRO-DVN-VEND-FRT-010` (19293-19302), `7915-PRO-DEF-BG-FRT-010` (19645-19741),
`7226-CHK-FRT-FEE-EXEMPT` (13348-13439, called separately, after cost totals are assembled).
CID freight table confirmed by T005 SQL evidence as `VN_CID_VN_FREIGHT` (VNG40 host struct).

### R-FREIGHT-000 Freight applicability gate (cost side)
1. ID/Name: R-FREIGHT-000 Freight applicability gate
2. Program/paragraph: 7200-PRO-ADJ-COST-010, lines 12649-12682
3. Preconditions: none upstream beyond normal cost processing
4. Data deps: OMGPR-F-FRT-EXEMPT (X1, cost-contract-line-level exempt flag), OMGPR-C-VEND-VHA-PLUS
   (X1, 'Y'/'C'), OMGPR-F-AA-VHA-IND (X1), OMGPR-F-AA-FRT-IN (X1, account-level apply-freight
   flag: 'N'=skip, else a mode letter forwarded into WS-APPLY-FRT-CST-SELL-SW)
5. Priority: runs before the freight waterfall; either bypasses it entirely or feeds the mode
6. Calculation (no arithmetic, pure gating): IF OMGPR-F-FRT-EXEMPT='Y' OR ((VEND-VHA-PLUS='Y' OR
   ='C') AND AA-VHA-IND='Y') THEN skip freight entirely (CONTINUE, no PERFORM). ELSE IF
   OMGPR-F-AA-FRT-IN='N' THEN also skip. ELSE: WS-APPLY-FRT-CST-SELL-SW = OMGPR-F-AA-FRT-IN, then
   PERFORM 0205-PRO-FREIGHT-COST-010. After the waterfall returns, if a product-level freight was
   found (OMGPR-C-INFRT-TYPE = WS-FRT-PRODUCT-IND) and the order line's UOM differs from the
   vendor's base UOM, OMGPR-A-INFRT is rescaled by OMGPR-ALT-ORD-CONV-FACTOR so the freight amount
   matches the ordered-line UOM rather than the base UOM.
7. Output: gates whether OMGPR-A-INFRT is computed at all this cost pass; also
   OMGPR-A-INFRT UOM-rescale for product-level freight
8. Dates: none
9. Exclusions/fallbacks: two independent full-bypass conditions (line-level FRT-EXEMPT flag;
   VHA-plus + VHA-indicator combination) and one conditional-mode bypass (account flag = 'N')
10. Errors: none in this paragraph
11. Confidence: CONFIRMED

### R-FREIGHT-001 Account-vendor freight (highest priority)
1. ID/Name: R-FREIGHT-001 Account-vendor freight (VNG31)
2. Program/paragraph: 9650-PRO-ACT-VEND-FREIGHT-CHG, lines 23811-23878; dispatched
   unconditionally first from 0205-PRO-FREIGHT-COST-010 (lines 4607-4608, comment: "ADDED LOGIC
   TO PROCESS ACT BASED FREIGHT FIRST BEFORE ANY OTHER WAY OF COMPUTING FREIGHT CHARGES")
3. Preconditions: R-FREIGHT-000 gate passed
4. Data deps (T005-confirmed SQL): `SELECT P_AC_VN_FREIGHT, D_AC_VN_FREIGHT_EX,
   P_AC_VN_SELL_FREIGHT FROM VNG31 WHERE I_VENDOR=:OMGPR-I-VENDOR AND
   I_ACCOUNT=:OMGPR-I-ACCOUNT AND D_AC_VN_FREIGHT_EF <= :WS-CURRENT-DB2-DATE AND
   (D_AC_VN_FREIGHT_EX >= :WS-CURRENT-DB2-DATE OR D_AC_VN_FREIGHT_EX IS NULL)` — i.e. an
   effective-dated, single-row-expected lookup keyed on vendor+account
5. Priority: checked before every other freight source; if found (SQLCODE 0), no other freight
   source paragraph in the waterfall runs (R-FREIGHT-000's caller GO TO 0205-EXIT immediately
   after, see step in R-FREIGHT-002)
6. Calculation: WHEN SQLCODE=0: OMGPR-C-INFRT-TYPE = WS-FRT-ACT-IND (account-level type code);
   IF VNG31-P-AC-VN-FREIGHT > 0 AND WS-APPLY-FRT-CST-SELL-SW='C' (cost-applied mode): OMGPR-A-INFRT
   ROUNDED = OMGPR-A-VND-PRC-DEALER * VNG31-P-AC-VN-FREIGHT (percent-of-dealer-cost). ELSE IF
   WS-APPLY-FRT-CST-SELL-SW='S' (sell-applied mode): OMGPR-P-INFRT-BG is instead loaded with
   VNG31-P-AC-VN-SELL-FREIGHT (a *rate*, not a computed amount — no COMPUTE against dealer cost in
   this branch). Expiration date (if not null) is added to the closest-expiration tracking array
   via 7695-ADD-EXP-DATE-ARRA-010. WHEN SQLCODE=100 (not found): CONTINUE (fall through to
   R-FREIGHT-002). WHEN OTHER: **fatal error** — WS-DB-ERROR-NBR=36, OMGPR-F-PRICER-ERROR='Y',
   GO TO 0020-EXIT-PRICER (abend, no partial pricing result returned).
7. Output: OMGPR-A-INFRT or OMGPR-P-INFRT-BG (S9(3)V9(4)-shaped percent field depending on branch),
   OMGPR-C-INFRT-TYPE, WS-IN-FRT-FOUND-SW='Y'
8. Dates: D_AC_VN_FREIGHT_EF/EX (effective/expiration window on the DB2 row); closest-expiration
   contributed via 7695-ADD-EXP-DATE-ARRA-010
9. Exclusions/fallbacks: falls through to R-FREIGHT-002 only on SQLCODE=100 (no matching row)
10. Errors: DB error #36, fatal, WHEN OTHER SQLCODE branch
11. Confidence: CONFIRMED. **Structural caveat (INFERRED, needs compiler/listing verification
    before reimplementation):** the source's nested-IF/ELSE construct at lines 23841-23852
    (`IF freight>0 AND switch='C' ... ELSE IF switch='S' ... END-IF`) has only one `END-IF`
    covering what reads as two nested IF levels. By standard COBOL innermost-first END-IF
    matching, that `END-IF` closes the inner `IF switch='S'` and leaves the outer
    `IF freight>0 AND switch='C'` open, which would then also wrap the subsequent
    "closest expiration date" block (lines 23853-23858) inside its (unclosed) ELSE branch —
    meaning the expiration-date capture would only run when the cost-applied branch does NOT
    fire. This reads as an unintended side effect of the NK0611 patch (which added the AND
    condition and the ELSE/IF-'S' branch to what was originally a single-condition IF), not a
    deliberate rule. Flagged rather than silently corrected: verify against a real compile
    listing/cross-reference before an implementer relies on either interpretation.

### R-FREIGHT-002 CID (customer-ID)-vendor freight (fallback #1)
1. ID/Name: R-FREIGHT-002 CID-vendor freight (VN_CID_VN_FREIGHT)
2. Program/paragraph: 9652-PRO-CID-VEND-FREIGHT-CHG, lines 23880-23947; dispatched from
   0205-PRO-FREIGHT-COST-010 lines 4610-4616 only `IF SQLCODE = +100` after R-FREIGHT-001
   (i.e., only when the account-vendor lookup found no row)
3. Preconditions: R-FREIGHT-001 returned SQLCODE=100
4. Data deps (T005-confirmed SQL, table name confirmed distinct from copybook's self-description):
   `SELECT P_CID_VN_FREIGHT, D_CID_VN_FREIGHT_EX, P_CID_VN_SELL_FREIGHT FROM VN_CID_VN_FREIGHT
   VNG40 WHERE I_VENDOR=:OMGPR-I-VENDOR AND CUSTOMER_NBR=:OMGPR-CUSTOMER-NBR AND
   D_CID_VN_FREIGHT_EF <= :WS-CURRENT-DB2-DATE AND (D_CID_VN_FREIGHT_EX >=
   :WS-CURRENT-DB2-DATE OR IS NULL)`
5. Priority: second in the waterfall, after account-vendor, before the BG/product/division/corp
   cascade
6. Calculation: identical shape to R-FREIGHT-001 (cost-applied percent-of-dealer-cost vs.
   sell-applied rate branch, same structural END-IF caveat applies here too at lines
   23910-23921), keyed by customer instead of account. OMGPR-C-INFRT-TYPE = WS-FRT-CID-IND.
7. Output: same fields as R-FREIGHT-001
8. Dates: D_CID_VN_FREIGHT_EF/EX window; contributes to closest-expiration array
9. Exclusions/fallbacks: after this paragraph returns (found or not), the caller checks
   `IF OMGPR-C-INFRT-TYPE = WS-FRT-ACT-IND OR WS-FRT-CID-IND` (4623) — if EITHER R-FREIGHT-001 or
   R-FREIGHT-002 found a match, freight processing stops here (GO TO 0205-EXIT), with one more
   twist: if WS-APPLY-FRT-CST-SELL-SW='V' (variable-freight mode), it's downgraded to 'S' at this
   point before exiting (line 4624-4626)
10. Errors: same fatal-error shape as R-FREIGHT-001 (DB error #212, WN_CID_VN_FREIGHT, fatal abend
    on WHEN OTHER)
11. Confidence: CONFIRMED

### R-FREIGHT-003 Buy-group (child-then-parent) vendor freight
1. ID/Name: R-FREIGHT-003 Buy-group vendor freight cascade
2. Program/paragraph: 0285-PRO-BG-FREIGHT-010 / 0286-PRO-BG-FREIGHT-01-010 (6833-6902) calling
   7280-SEL-IN-FRT-BG-VEN-010 (14060-14165); reached only when R-FREIGHT-001/002 both missed and
   the contract is individual/no-cost-contract-found or group-cost-contract WITH a resolved BG
   priority (see R-FREIGHT-000's caller-level dispatch at 0205 lines 4638-4681/4685-4733), and the
   BGG20 flag-to-test = 'Y' (routes through 7265-PRO-NEW-FRT-010, which perform-chains into 0285)
3. Preconditions: WS-BG-MEMBER-FND (child BG) tried first; if not found or no freight there, walks
   up parent BG chain via 7585-SEL-PARENT-SELL-010 in a PERFORM UNTIL loop, retrying
   7280-SEL-IN-FRT-BG-VEN-010 at each parent level until either freight is found or no more
   parents exist (WS-FIND-PARENT-SW='N')
4. Data deps (T005-confirmed SQL, from 7280): `SELECT ... FROM BGG10 WHERE ...` (buy-group vendor
   freight table), keyed by buy-group (+vendor when OMGPR-F-AA-FRT-IN='V' triggers the
   as-of-date-aware 9680-SQL-SELECT variant instead of the plain 9250-SQL-SELECT-010)
5. Priority: child BG level checked before any parent level; first BG level (child or ancestor)
   with a matching row wins and stops the climb
6. Calculation: WHEN SQLCODE=0 or +001: OMGPR-A-INFRT ROUNDED = OMGPR-A-VND-PRC-DEALER *
   BGG10-P-INFRT-BG (percent-of-dealer-cost, same shape as R-FREIGHT-001/002); WS-FRT-BUY-GROUP-IND
   type code; WS-APPLY-FRT-CST-SELL-SW is taken from OMGPR-F-AA-FRT-IN when that ='V', else from
   BGG10-F-FRT-BG-FLAG (the buy-group row's own cost/sell/variable flag — this means the mode can
   differ per BG row, not fixed at the account level, when not in 'V' mode at the account). Closest
   expiration collected from BGG10/BGG02/CUG10/CUG06 exp fields (multiple candidate dates, all fed
   into the shared 7695-ADD-EXP-DATE-ARRA-010 accumulator — final "closest" resolution happens
   elsewhere, outside this paragraph's scope). WHEN SQLCODE=100 (no row at this BG level):
   PERFORM 7915-PRO-DEF-BG-FRT-010 (R-FREIGHT-004, default-vendor '0000' fallback) before giving up
   on this BG level. WHEN OTHER: fatal error (DB error #36, table BGG10, abend).
7. Output: OMGPR-A-INFRT, OMGPR-P-INFRT-BG, OMGPR-I-BUY-GROUP-FRT, OMGPR-C-INFRT-TYPE
8. Dates: BGG10-D-INFRT-BG-EXP, BGG02-D-BGM-END-MEM, CUG10/CUG06-D-*-BG-PRI-EXP all contribute to
   the closest-expiration tracking
9. Exclusions/fallbacks: 7915 default-vendor fallback tried at each BG level before moving up to
   the next parent; loop terminates on first success at any level or exhaustion of parent chain
10. Errors: fatal on unexpected SQLCODE (DB error #36)
11. Confidence: CONFIRMED for control flow and formula; the exact vendor-as-of-date variant
    queries (9680-SQL-SELECT, 9685-SQL-SELECT) were not independently re-read in this pass —
    their existence and trigger condition (OMGPR-F-AA-FRT-IN='V' AND a buy-group-vendor-flag
    keyed date present) is CONFIRMED from the calling code, but their internal WHERE-clause detail
    is deferred to the T005 SQL inventory CSV rather than re-transcribed here

### R-FREIGHT-004 Default buy-group freight (vendor-agnostic fallback)
1. ID/Name: R-FREIGHT-004 Default (vendor '0000') buy-group freight
2. Program/paragraph: 7915-PRO-DEF-BG-FRT-010, lines 19645-19741
3. Preconditions: called only from within R-FREIGHT-003 when the vendor-specific BGG10 lookup at
   the current BG level returned SQLCODE=100
4. Data deps: same BGG10 table, but BGG10-I-VENDOR is forced to '0000' (wildcard/default vendor
   row) before the SELECT
5. Priority: tried once per BG level, between the vendor-specific miss and moving to the next
   parent BG
6. Calculation: identical formula and expiration-tracking shape to R-FREIGHT-003's WHEN SQLCODE=0
   branch. WHEN SQLCODE=100: CONTINUE (truly nothing at this BG level, climb to parent). WHEN
   OTHER: fatal (same DB error #36 pattern).
7. Output: same fields as R-FREIGHT-003
8. Dates: same closest-expiration contribution
9. Exclusions/fallbacks: this is itself the last fallback within a single BG level before the
   parent-climb loop in 0286 tries the next ancestor
10. Errors: fatal on unexpected SQLCODE
11. Confidence: CONFIRMED

### R-FREIGHT-005 Product/division/corporate freight (no-BG-priority fallback)
1. ID/Name: R-FREIGHT-005 Product, division-vendor, and corporate freight cascade
2. Program/paragraph: 7270-PRO-ACCT-FREIGHT-010, lines 13955-14005, calling
   7895-PRO-DVN-VEND-FRT-010 (19293-19302) and 7820-PRO-CORP-FRT-010 (18715-18732, which itself
   re-enters 0285-PRO-BG-FREIGHT-010's child/parent cascade — R-FREIGHT-003 — but scoped to the
   corporate buy-group context)
3. Preconditions: reached when no BG priority was found for the account/customer (0205's ELSE
   branch at lines 4674-4678), or reached from within R-FREIGHT-003's 7265 dispatcher as a
   fallback after BG-vendor freight failed at every level (line 13949 call site)
4. Data deps: ING01-A-DIV-INV-FREIGHT (product/inventory-level freight, in-memory field — source
   SELECT not re-examined this pass), VNG20 (division-vendor freight, keyed
   OMGPR-I-VENDOR + OMGPR-I-DIVISION, via 9600-SQL-SELECT-010 — replaced older VNG07-based logic
   per a dated comment, "SATHYA 11/13/00")
5. Priority (CONFIRMED, explicit in-line priority comment at 13970): (1) product/inventory freight
   (ING01-A-DIV-INV-FREIGHT > 0) checked first and wins outright if present — GO TO 7270-EXIT
   immediately, no division or corp freight even attempted; (2) division-vendor freight
   (7895, only tried if OMGPR-F-AA-FRT-IN <> 'V') — if that already set OMGPR-C-INFRT-TYPE to
   the division-vendor indicator, exit; (3) corporate freight (7820) as the final fallback
6. Calculation: product freight is a direct MOVE (ING01-A-DIV-INV-FREIGHT -> OMGPR-A-INFRT,
   type=WS-FRT-PRODUCT-IND), not a percent-of-cost formula — it is already a resolved amount at
   this level, unlike the account/CID/BG percent-of-dealer-cost patterns above. Division-vendor
   and corporate freight paragraphs were not read to formula-level detail in this pass (deferred).
7. Output: OMGPR-A-INFRT, OMGPR-C-INFRT-TYPE
8. Dates: not examined for ING01/VNG20 in this pass (BLOCKED/deferred — DCLGENs for ING01 and
   VNG20 are not in upload/)
9. Exclusions/fallbacks: **notable dead-code finding (CONFIRMED via in-line comment)** — a prior
   version of this paragraph used the buy-group freight amount as a top-end ceiling/limit on the
   account freight amount ("YOU CAN ONLY USE THE ACCOUNT FREIGHT AMOUNT UP TO THE GROUP FREIGHT
   AMOUNT"); that comparison logic has been commented out entirely, with a maintainer note reading
   "SATHYA COMMENTED THE FOLLOWING; I DO NOT KNOW WHY THIS EXISTED IN THE FIRST PLACE" (lines
   13994-14001). The group-freight-as-ceiling behavior described in the surrounding comments is
   **no longer active** — do not reimplement it as current behavior, but its prior existence
   explains the comment block if that context is ever needed.
10. Errors: not examined in this pass for 7895/7820/9600-SQL-SELECT-010 internals (deferred to
    T005 SQL CSV)
11. Confidence: CONFIRMED for the three-way priority and the dead-code finding; INFERRED/deferred
    for ING01/VNG20 field-level detail (DCLGENs not supplied)

### R-FREIGHT-006 Freight exemption and type-code recoding (post-calculation)
1. ID/Name: R-FREIGHT-006 Freight fee exemption determination and type recoding
2. Program/paragraph: 7226-CHK-FRT-FEE-EXEMPT, lines 13348-13439; called from
   7200-PRO-ADJ-COST-010 at lines 12718-12723, **after** the freight waterfall and the total
   adjusted-cost COMPUTE, i.e. as a final post-processing step, not part of freight *selection*
3. Preconditions: none beyond having already run the freight waterfall
4. Data deps: VNG02-C-CUSTOM-IND, OMGPR-F-EXEMPT-CUSTOM-FLAG, OMGPR-F-EXEMPT-SANC-FLAG,
   OMGPR-F-EXEMPT-NON-SANC-FLAG, OMGPR-F-EXEMPT-IND-FLAG, OMGPR-F-EXEMPT-NON-CONT-FLAG,
   CCG09-I-RPT-GRP, OMGPR-F-GRP-CNT-FEES, BGG01-I-DIVISION, WS-INDIV-CNT, WS-COST-CONT-FND.
   **Update (second upload batch): the five `OMGPR-F-EXEMPT-*` flags' upstream source table,
   `CUG53` (DCLGEN TABLE `CU_AC_FREIGHT_FLAG`), is now supplied and read in full — exact match
   confirmed:** `CUG53-F-FRT-FLAG` (general freight flag), `CUG53-F-GRP-SANC`
   (-> `OMGPR-F-EXEMPT-SANC-FLAG`), `CUG53-F-GRP-NON-SANC` (-> `-NON-SANC-FLAG`), `CUG53-F-IND`
   (-> `-IND-FLAG`), `CUG53-F-NON-CONT` (-> `-NON-CONT-FLAG`), `CUG53-F-CUSTOM`
   (-> `-CUSTOM-FLAG`), all `X(1)`, keyed by `CUG53-I-ACCOUNT` (S9(9) COMP) with
   `CUG53-D-EFF-DATE`/`-D-EXP-DATE` (X(10), EXP nullable) and a `CUG53-I-BUY-GROUP` (S9(8) COMP)
   column also present but not cited by name in this paragraph. `VNG02-C-CUSTOM-IND` (X(1)) is
   likewise now confirmed via the supplied `VNG02.CPY`.
5. Priority (CONFIRMED, mutually-exclusive cascade of exactly one exemption category applies):
   (a) IF VNG02-C-CUSTOM-IND='Y': custom-account exemption flag governs, full stop (does not fall
   through to any of b-e). (b) ELSE IF WS-COST-CONT-FND (cost contract exists): sanctioned-group
   exemption (CCG09-I-RPT-GRP>0 OR OMGPR-F-GRP-CNT-FEES='Y', gated by EXEMPT-SANC-FLAG) is tried
   first; (c) else non-sanctioned-group exemption (division='01' AND GRP-CNT-FEES='N', gated by
   EXEMPT-NON-SANC-FLAG); (d) else individual-contract exemption (WS-INDIV-CNT OR division<>'01',
   gated by EXEMPT-IND-FLAG). (e) ELSE (no cost contract at all): non-contract exemption
   (EXEMPT-NON-CONT-FLAG). Exactly one of (a)/(b)/(c)/(d)/(e) is evaluated per invocation
   (if/elseif chain), and within each, the specific exemption flag must be 'N' (not 'Y') for
   WS-EXEMPT-FRT-CHARGES to be set to 'Y' — i.e. the flag semantics are inverted: the customer/
   contract-scope condition identifies WHICH exemption flag applies, and that flag being 'N'
   (not opted out of the fee) is what triggers WS-EXEMPT-FRT-CHARGES='Y' (exempt FROM the freight
   charge). A flag value of 'Y' means the opposite — NOT exempt, freight charge stands.
6. Calculation: sets WS-EXEMPT-FRT-CHARGES to 'Y' or leaves 'N', per the cascade above. Then,
   regardless of which path, recodes OMGPR-C-INFRT-TYPE via an EVALUATE table: if exempt, letters
   C/B/D/P/V/A/W recode to E/J/K/L/M/N/Y respectively; if not exempt AND
   WS-APPLY-FRT-CST-SELL-SW='S', a different recode table applies (C/B/D/P/V/A/W ->
   S/E/F/G/H/I/X); if not exempt AND WS-APPLY-FRT-CST-SELL-SW='V', yet another table applies
   (B/D/P/V/A/W -> Q/R/S/T/U/Z). These are output/reporting classification codes layered onto
   whichever freight source (R-FREIGHT-001..005) already determined OMGPR-C-INFRT-TYPE.
7. Output: WS-EXEMPT-FRT-CHARGES (working-storage flag, consumed immediately by the caller);
   OMGPR-C-INFRT-TYPE (recoded)
8. Dates: none
9. Exclusions/fallbacks: **the actual cost impact is applied by the caller, not this paragraph**
   — 7200-PRO-ADJ-COST-010 lines 12721-12723: `IF WS-EXEMPT-FRT-CHARGES = 'Y' THEN MOVE ZEROES TO
   OMGPR-A-INFRT` — exemption zeroes the freight charge outright; the elaborate type-recoding
   table exists purely to preserve an audit/reporting trail of what kind of freight it *would have
   been*, not to alter the (already-zeroed) amount.
10. Errors: none in this paragraph
11. Confidence: CONFIRMED

---

## 3. JIT (Just-In-Time) fee

# JIT (Just-In-Time) fee — extracted rules (source notes for fees-and-adjustments.md)

**Provenance note:** this section is carried over from `omni-claude`'s prior
`docs/cobol-analysis/decision-tables/jit-adjustment.md` (built against the identical
`A6U01.CBL` — MD5-verified match between the two repos' `upload/` copies), re-verified for T009
rather than re-derived from scratch. Content and line citations below are unchanged from that
source; only the framing/cross-references are adapted to fit alongside the other 9 T009 areas.
R-JIT-007 (freight-fee exemption cascade), originally documented adjacent to JIT because it lives
in the same source region, is **not** repeated here — it is superseded by this document's own
`R-FREIGHT-006`, which additionally traces the caller-side effect (exemption zeroes
`OMGPR-A-INFRT`) that the original JIT-side write-up left as BLOCKED/unexamined.

Program/paragraph: A6U01.CBL, `7225-PRO-JIT-ADJ-010` (lines 13020-13341), called from
`7195-PRO-ADJ-SELL-010` only when `OMGPR-F-JIT-EXEMPT NOT = 'Y'` and
`OMGPR-F-ST-JIT-CUSTOMER = 'Y'` and `OMGPR-JIT-SERVICE-FEE` is one of R/P/C/A.

### R-JIT-001 Service-fee type gate
1. ID/Name: R-JIT-001 Service-fee-type master gate
2. Program/paragraph: 7225-PRO-JIT-ADJ-010, lines 13020-13341 (gate 13044-13047, cost/sell split
   13320-13334)
3. Preconditions: caller re-checks OMGPR-F-JIT-EXEMPT<>'Y' AND OMGPR-F-ST-JIT-CUSTOMER='Y' AND
   service-fee code in {R,P,C,A} before even calling this paragraph
4. Data deps: OMGPR-JIT-SERVICE-FEE and all OMGPR-JIT-* inputs, populated upstream via CUP100's
   A400-GET-JIT-ADJ from CUR120 (ultimately CUP120/CUS120 — BLOCKED, not supplied). **Update
   (second upload batch): `CUR120.CPY` is now supplied and read in full, confirming the exact
   field shapes CUP100 moves into OMGPR (though not the computation that populates `CUR120`
   itself, which remains CUP120/CUS120's still-BLOCKED job):** `CUR120-F-ST-JIT-CUSTOMER` (X(1)),
   `CUR120-JIT-SERVICE-FEE` (X(1), the A/C/R/P code this rule gates on),
   `CUR120-JIT-FEE-BREAK-OUT-SW` (X(1)), `CUR120-JIT-SERVICE-FEE-PCT` (S9(1)V9(4) COMP-3),
   `CUR120-JIT-LABEL-CHRG-TYPE`/`-AMT` (X(1) / S9(2)V9(4) COMP-3),
   `CUR120-JIT-APPLY-CHRG-TYPE`/`-AMT` (X(1) / S9(2)V9(4) COMP-3),
   `CUR120-JIT-BREAK-CHRG-TYPE`/`-AMT` (X(1) / S9(2)V9(4) COMP-3),
   `CUR120-JIT-LUOM-CHRG-AMT` (S9(2)V9(4) COMP-3, `HR0415` addition),
   `CUR120-JIT-LUM-FEE-PCT`/`-EXTRA-DELIV-FEE-PCT`/`-NON-OM-SLCT-FEE-PCT` (all S9(1)V9(4) COMP-3 —
   these three back R-JIT-002 item 5's "four sub-amounts" service-fee split). The scale/decimal
   placement question previously open for these percentage/amount fields is now CONFIRMED, not
   INFERRED.
5. Priority: every other JIT rule below is conditional on this gate. Code meanings (CONFIRMED,
   header comment + branching at 13320-13340):
   | Code | Meaning | Calc base | Embedded in line item? |
   |---|---|---|---|
   | A | Bill at adjusted cost | Cost | No (order-total only) |
   | C | Add to cost | Cost | Yes |
   | R | Bill at price | Sell | No (order-total only) |
   | P | Add to price | Sell | Yes |
6. Calculation: N/A at this level (see R-JIT-002)
7. Output: N/A directly (see R-JIT-002/R-JIT-006)
8. Dates: none
9. Exclusions/fallbacks: any code other than A/C/R/P (including spaces) is a silent no-op — the
   whole paragraph is one `IF ... = 'A' OR 'C' OR 'R' OR 'P' ... END-IF` with no ELSE and no
   error. This is the third place in A6U01 (alongside contract cost-entry method and sell-price
   method) where an unrecognized code is treated as "do nothing" rather than raising a diagnostic
   — a recurring idiom in this codebase, not an isolated oversight.
10. Errors: none raised by 7225 itself
11. Confidence: CONFIRMED

### R-JIT-002 Five fee categories (break-bulk, print-label, apply-label, service-fee family)
1. ID/Name: R-JIT-002 Per-category fee computation
2. Program/paragraph: 7225-PRO-JIT-ADJ-010 — "CALC JIT BREAK BULK FEE #1" (13084-13131, mostly
   dead code, see R-JIT-003), "CALC JIT PRINT LABEL FEE #2" (13132-13192), "CALC JIT APPLY LABEL
   FEE #3" (13193-13262), "CALC JIT SERVICE FEE #4" (13263-13317), summed at "CALCULATE TOTAL JIT
   FEE FOR COST/SELL #5" (13318-13334)
3. Preconditions: break-bulk/label/apply-label each gated by an item-level 88-level flag
   (OMGPR-JIT-ITEM-BREAK-BULK / -LABEL / -APPLY-LAB) set upstream per order line. Service fee (#4)
   has no item-level gate — always attempts to compute if the vendor has a nonzero
   WS-VND-JIT-SF-FEE or WS-VND-JIT-PF-FEE.
4. Data deps: WS-VND-JIT-*-FEE working fields (set from OMGPR-JIT-* inputs at paragraph top; see
   R-JIT-004 for the 2022 fee-component interaction on break-bulk/apply-label)
5. Priority: each category sub-dispatches on its own charge-type field (percent vs. rate vs. flat)
   via independent IFs (not EVALUATE), mutually exclusive in practice since the underlying
   88-levels test one field with distinct literal values:
   - Percent-based (#2/#3, mirrored): `IF (SERVICE-FEE='A' OR 'C') AND PRIVATE-LBL NOT='O' ->
     fee = TOTAL-COST * vendor-pct; ELSE -> fee = SELL-BEFORE-ADJ * vendor-pct`. This re-tests the
     cost/sell split independently per category rather than reusing R-JIT-001's result.
   - Rate-based (#2/#3, EVALUATE TRUE at 13158/13225): 3-way UOM-dependent — label-UM and
     alt-order-UM both found and equal to order UOM -> `(rate * ALT-ORD-CONV-FACTOR * qty) /
     CUST-LABEL-CONV-FACTOR`; label-UM found alone -> `(rate * qty) / CUST-LABEL-CONV-FACTOR`;
     otherwise -> `rate * qty` (no conversion).
   - Per-item flat rate (LABEL-RATE-PER-ITEM/APPLY-RATE-PER-ITEM): direct MOVE of vendor's flat
     fee, no quantity multiplication.
   - Break-bulk (#1): the percent/rate computation code is entirely commented out
     (lines 13089-13117) — see R-JIT-003 for what replaced it.
   - Service fee (#4): no percent/rate/flat split — always `base * vendor-pct` for up to four
     sub-amounts simultaneously (service, LUM, extra-delivery, non-OM-select), gated by the same
     cost/sell test. Non-OM-select has an additional gate (WS-NON-OM-SELECT-ITEM) and is ADDED
     into OMGPR-A-JIT-SERVICE-FEE rather than kept separate.
6. Calculation: all COMPUTEs use ROUNDED (round-half-up)
7. Output: OMGPR-A-JIT-BREAK-FEE, -LABEL-FEE, -APPLY-FEE, -SERVICE-FEE, -LUM-FEE,
   -EXTRA-DELIV-FEE, -NON-OM-SLCT-FEE (folded into -SERVICE-FEE)
8. Dates: none in this paragraph
9. Exclusions/fallbacks: item-level gate is itself the exclusion — an unflagged item never
   computes that category's fee
10. Errors: none
11. Confidence: CONFIRMED for structure/formulas. `7227-CHK-FOR-OVERRIDES` (called before each
    category) can alter WS-VND-JIT-*-FEE before these formulas run; its internals are BLOCKED
    (6 paragraphs, tables not supplied) — the exact vendor-fee value consumed may reflect an
    account/product-category/vendor-level override not traced in this pass.

### R-JIT-003 Break-bulk fee redirect to low-UOM/break-bulk chain
1. ID/Name: R-JIT-003 Break-bulk fee unconditional redirect
2. Program/paragraph: 7225-PRO-JIT-ADJ-010, lines 13118-13131 (redirect only; target chain
   extracted separately, see Low-UOM/Break-bulk section of this document)
3. Preconditions (CONFIRMED, lines 13123-13128): OMGPR-A-JIT-BREAK-FEE=0 AND
   OMGPR-F-LUOM-ELIG-SW='Y' AND OMGPR-ORDER-TYPE-STOCK (order type 'S' or space)
4. Data deps: 0245-PRO-BG-LOW-UOM-010 (see Low-UOM/Break-bulk section)
5. Priority: because the break-bulk percent/rate computation is entirely commented out
   (R-JIT-002), OMGPR-A-JIT-BREAK-FEE is always 0 at this check — **this redirect is not really
   conditional in practice; it fires whenever LUOM-ELIG and ORDER-TYPE-STOCK hold, every time.**
   0245's chain is the only live path to a break-bulk/LUOM JIT charge in this codebase.
6. Calculation: see Low-UOM/Break-bulk section
7. Output: same OMGPR-A-JIT-BREAK-FEE field the dead inline formula would have written
8. Dates: none directly (see Low-UOM/Break-bulk section)
9. Exclusions/fallbacks: OMGPR-F-LUOM-ELIG-SW is the exclusion gate — its own copybook comment
   states "LUOM CHARGES WILL BE COMPUTED ONLY IF ACCOUNT IS ELIGIBLE FOR THIS CHARGE. THIS SWITCH
   IS SET IN CUS120 AND PASSED THROUGH CUP100/CUP110" — origin is the still-BLOCKED CUS120 member;
   this task cannot state which accounts qualify, only that A6U01 trusts whatever CUS120 decided.
   **Update: `CUR120-F-LUOM-ELIG-SW` (X(1), `SA0403` addition, incident #461097 per its own comment)
   is CONFIRMED as the exact intermediate carrier of this flag between CUS120 and OMGPR — the
   field shape is now known, though CUS120's eligibility rule that sets it remains BLOCKED.**
10. Errors: none in this fragment
11. Confidence: CONFIRMED for the control-flow finding; BLOCKED for CUS120's actual eligibility
    rule

### R-JIT-004 2022 fee-component system supersedes legacy break-bulk/apply-label percent calc
1. ID/Name: R-JIT-004 2022 fee-component override of legacy JIT percent calc
2. Program/paragraph: 7225-PRO-JIT-ADJ-010, lines 13049-13060 (break-bulk source selection),
   13194-13261 (apply-label, early branch at 13195-13200)
3. Preconditions:
   - Break-bulk: IF OMGPR-FEE-SHRT-CODE-BB > SPACES (a 2022 fee-component row exists) AND
     (OMGPR-FEE-PCT-SELL-BB OR -COST-BB) -> use OMGPR-JIT-BREAK-CHRG-AMT as before (no change).
     ELSE (row exists but type is flat/per-line/per-qty/per-order) -> WS-VND-JIT-BB-FEE forced to
     ZERO, suppressing the legacy percent entirely. No fee-component row at all -> legacy amount
     used unmodified. Since R-JIT-003 already makes the legacy break-bulk formula dead code today,
     this interaction is currently moot but would matter immediately if that formula were ever
     un-commented.
   - Apply-label: IF OMGPR-FEE-SHRT-CODE-AL > SPACES AND (per-line OR per-order OR per-qty type)
     -> OMGPR-A-JIT-APPLY-FEE set DIRECTLY from OMGPR-A-APPLY-LAB-AL (the 2022 fee-component's own
     pre-computed amount) — the entire 7227-override/percent/rate cascade is skipped entirely.
     Only when this condition is false does the legacy cascade run.
4. Data deps: OMGPR-FEE-SHRT-CODE-BB/-AL, OMGPR-FEE-PCT-SELL-BB/-COST-BB, OMGPR-FEE-PER-LINE-AL/
   -ORDER-AL/-QTY-AL, OMGPR-A-BREAKBULK-BB, OMGPR-A-APPLY-LAB-AL — all populated by CUP100's 2022
   fee-component paragraphs
5. Priority: the 2022 fee-component system's presence and type directly gates whether the legacy
   percent/rate machinery even runs for these two categories — a genuine precedence rule between
   two subsystems built 20+ years apart, undocumented except in this branching logic
6. Calculation: apply-label fast path is a direct MOVE (already rounded upstream); break-bulk
   suppression just zeroes a working field feeding a currently-dead formula
7. Output: WS-VND-JIT-BB-FEE (break-bulk), OMGPR-A-JIT-APPLY-FEE (apply-label fast path)
8. Dates: none in this fragment
9. Exclusions/fallbacks: absence of a 2022 fee-component row is itself the fallback to legacy
   behavior
10. Errors: none
11. Confidence: CONFIRMED for break-bulk and apply-label. Print-label (#2) and service fee (#4)
    show no equivalent 2022 fast path — confirmed by absence in the paragraph.

### R-JIT-005 Private-label 'O' products always billed on sell, never cost
1. ID/Name: R-JIT-005 Private-label 'O' cost/sell override
2. Program/paragraph: 7225-PRO-JIT-ADJ-010, recurring clause `AND OMGPR-C-PRIVATE-LBL NOT = 'O'`
   guarding every cost-vs-sell split — lines 13146, 13213, 13276, 13322 (four occurrences: label,
   apply-label, service-fee, final total split)
3. Preconditions: OMGPR-JIT-SERVICE-FEE = 'A' or 'C' (cost-based codes) AND
   OMGPR-C-PRIVATE-LBL = 'O'
4. Data deps: OMGPR-C-PRIVATE-LBL (populated upstream, not traced)
5. Priority: overrides R-JIT-001/002's stated cost basis for codes A/C specifically for
   private-label-'O' products — applies consistently across all four sites, a deliberate carve-out.
   No comment explains why; flagged as a business-rule question, not inferable from code alone.
6. Calculation: no new formula — changes which formula (cost- vs sell-based) applies
7. Output: same fields as R-JIT-002/006, routed through the sell-based formula
8. Dates: none
9. Exclusions/fallbacks: N/A — this rule is itself the exclusion
10. Errors: none
11. Confidence: CONFIRMED for the mechanism. INFERRED that 'O' means "Owens-sourced" (by analogy
    to A6O016U's 'O'=Owens-kit/'S'=supplier-kit product-type convention) — not confirmed from any
    comment in this specific paragraph. **Update: `OMGPR-C-PRIVATE-LBL`'s upstream field,
    `VNG02-C-PRIVATE-LBL` (X(1)), is now CONFIRMED to exist and match this shape (`VNG02.CPY`
    supplied) — but `VNG02.CPY` carries no comment giving 'O''s letter-value meaning either, so the
    "Owens-sourced" reading is still INFERRED, not upgraded to CONFIRMED.**

### R-JIT-006 Total JIT fee routes to cost or sell bucket; line-item visibility suppression
1. ID/Name: R-JIT-006 JIT total routing and line-item suppression flag
2. Program/paragraph: 7225-PRO-JIT-ADJ-010, lines 13318-13340
3. Preconditions: runs unconditionally once the four category calcs (R-JIT-002) complete, within
   the outer A/C/R/P gate (R-JIT-001)
4. Data deps: none new — arithmetic on already-computed OMGPR-A-JIT-*-FEE fields
5. Priority: same (A or C) AND PRIVATE-LBL NOT='O' test as R-JIT-002/005, applied once more at the
   total level — OMGPR-A-JIT-COST gets the sum in that case, OMGPR-A-JIT-SELL otherwise. This is
   the field that downstream cost/sell-adjustment paragraphs (7195/7200) actually consume.
6. Calculation: `COMPUTE ... ROUNDED = service + label + apply + break`, one sum, no intermediate
   rounding beyond what each category already applied
7. Output: OMGPR-A-JIT-COST (mutually exclusive with next field), OMGPR-A-JIT-SELL,
   OMGPR-F-JIT-REMOVED
8. Dates: none
9. Exclusions/fallbacks: OMGPR-F-JIT-REMOVED set to 'Y' (lines 13336-13340) when
   `(SERVICE-FEE='A' AND JIT-COST>0) OR SERVICE-FEE='R'` — precisely the two "order-total only"
   codes from R-JIT-001, and only for 'A' when the cost-based total is actually positive (a zero
   JIT-cost 'A'-type line is not flagged as removed). 'R' is flagged unconditionally with no
   equivalent >0 guard — a minor asymmetry, not necessarily consequential since a zero sell-based
   total flagged "removed" has no visible effect either way.
10. Errors: none
11. Confidence: CONFIRMED

---

## 4. PANDAC fee

# PANDAC fee — extracted rules (source notes for fees-and-adjustments.md)

Program/paragraph: A6U01.CBL `0165-PRO-PANDAC-010` (2855-2874) -> `0170-SEL-PANDAC-010`
(2877-2928) -> `7706-SEL-PANDAC-SPECIF-NEW` (17246-17319, calls `7707-CHECK-PANDAC-ACCT-FLAG`
17321-17361) and/or `7705-SEL-PANDAC-SPECIF-010` (17198-17244) -> `0172-PROCESS-FOR-DEFUALT-SHIP`
(2931-2971, default-ship-to fallback, not read to formula depth this pass). Live call site:
`7195-PRO-ADJ-SELL-010`, lines 12383-12387.

### R-PANDAC-000 PANDAC eligibility gate and **stale-comment finding**
1. ID/Name: R-PANDAC-000 PANDAC eligibility gate
2. Program/paragraph: 7195-PRO-ADJ-SELL-010 lines 12383-12387 (live caller);
   0165-PRO-PANDAC-010 lines 2865-2870 (inner gate)
3. Preconditions: VNG01-F-VEND-PANDAC = 'Y' (vendor participates in PANDAC) AND
   VNG02-F-PANDAC-ITEM = 'Y' (item/product is PANDAC-eligible) — both re-checked, once at the
   caller (7195) and again inside 0165 itself (redundant but harmless double-gate)
4. Data deps: VNG01-F-VEND-PANDAC, VNG02-F-PANDAC-ITEM — VNG01 DCLGEN still not supplied, BLOCKED
   on exact PIC/type (usage confirms Y/N flag). **Update (second upload batch): `VNG02-F-PANDAC-
   ITEM` is RESOLVED — `VNG02.CPY` now supplied, field confirmed `PIC X(1)`, `RP0511` addition.**
5. Priority: **CONFIRMED significant finding — the source comments describing this rule's
   behavior are STALE and describe removed logic.** Both the block comment immediately above the
   live call ("PANDAC - IMPLIED SELL ARRANGEMENT ... THE PANDAC FEE BECOMES AN IMPLIED SELL
   ARRANGEMENT. SO YOU WANT TO SKIP THE SELL ARRANGEMENT PROCESSING AND SELL ADJUSTMENT
   PROCESSING", lines 12370-12374) and 0165's own header comment ("THE PANDAC PERCENT WILL NOW BE
   USED AS A TYPE OF SELL ARRANGEMENT. THE PRICER WILL NOT DO ANY OTHER SELL ARRANGEMENT OR SELL
   ADJUSTMENT PROCESSING IF PANDAC IS APPLIED", lines 2859-2862) describe a short-circuit
   behavior that is **directly visible, but commented out, immediately above the live code**:
   lines 12376-12382 (`DP1022*`-tagged, disabled) show the original implementation — compute
   OMGPR-A-TOTAL-SELL from WS-A-SELL-BEFORE-ADJ + OMGPR-A-TOTAL-ADJ-SELL and `GO TO 7195-EXIT`
   immediately, skipping everything else in the paragraph. The **live** replacement (12383-12387)
   removes the `GO TO` — it only calls `0165-PRO-PANDAC-010` (which computes OMGPR-A-PANDAC) and
   then falls through into the normal downstream sell-arrangement/inventory-class-adjustment
   processing that follows in the same paragraph (line 12391 onward). **PANDAC no longer replaces
   sell-arrangement processing; it now runs alongside it.** Any reimplementation must follow the
   live control flow (fall-through), not the header comments' description of the old behavior —
   flagged explicitly since the comments would mislead a reader who trusts them over the code.
6. Calculation: gate only, no arithmetic (see R-PANDAC-001/002)
7. Output: gates whether 0165 (and its downstream ship-to selection) runs at all
8. Dates: none
9. Exclusions/fallbacks: neither flag='Y' means PANDAC is skipped entirely for this line
10. Errors: none in this fragment
11. Confidence: CONFIRMED (both the live code and the disabled prior version were directly read)

### R-PANDAC-001 Ship-to selection order (specific-new, specific-legacy, default)
1. ID/Name: R-PANDAC-001 Ship-to-suffix PANDAC row selection cascade
2. Program/paragraph: 0170-SEL-PANDAC-010, lines 2877-2928
3. Preconditions: R-PANDAC-000 gate passed
4. Data deps: OMGPR-C-SHIP-TO-SUFFIX (drives which branch runs)
5. Priority (CONFIRMED, sequential, first-match-wins, in-source comment at 2881-2894 documents
   the intended order and matches the code): (1) if OMGPR-C-SHIP-TO-SUFFIX is non-blank/non-low,
   try the "new" CMF-based specific-ship-to lookup first (`7706-SEL-PANDAC-SPECIF-NEW`) — if that
   finds a fee (WS-CMF-PANDAC-FEE-FND), stop immediately (GO TO 0170-EXIT), skipping the legacy
   path entirely; (2) else (or if the new lookup found nothing), and ship-to-suffix is still
   non-blank, try the legacy specific-ship-to lookup (`7705-SEL-PANDAC-SPECIF-010`, CUG29 table);
   (3) if still not found (WS-PANDAC-FND-SW not 'Y'), fall back to the default ship-to
   (`0172-PROCESS-FOR-DEFUALT-SHIP`, "000" default row) — this fallback also runs directly (no
   specific-ship-to attempt) when OMGPR-C-SHIP-TO-SUFFIX was blank/low-values from the start.
   **The "new" (CMF/CUTFEE-based) system takes precedence over the legacy (CUG29-based) system
   whenever a specific ship-to suffix is present**, but the legacy table can still supply the fee
   if the new system has no matching row — this is a live, two-generation coexistence, not a full
   cutover.
6. Calculation: see R-PANDAC-002 (legacy/CUG29) and R-PANDAC-003 (new/CMF)
7. Output: WS-PANDAC-FND-SW, ultimately OMGPR-A-PANDAC / OMGPR-P-PANDAC-ADJ
8. Dates: none directly in this dispatcher (see R-PANDAC-002/003)
9. Exclusions/fallbacks: default-ship-to path (0172) not read to formula depth this pass —
   BLOCKED/deferred
10. Errors: none in this dispatcher itself
11. Confidence: CONFIRMED for the cascade order; BLOCKED/deferred for 0172's internal formula

### R-PANDAC-002 Legacy PANDAC calculation (CUG29)
1. ID/Name: R-PANDAC-002 Legacy specific-ship-to PANDAC fee (CUG29)
2. Program/paragraph: 7705-SEL-PANDAC-SPECIF-010, lines 17198-17244
3. Preconditions: reached via R-PANDAC-001 step 2 (new system found nothing, or ship-to-suffix
   path skipped the new lookup — actually always attempted when ship-to-suffix is non-blank,
   regardless of new-system outcome, per the source's separate un-linked IF at 2911-2917)
4. Data deps (T005-confirmed SQL via 9435-SQL-SELECT-010): SELECT against CUG29, keyed by
   ship-to/account context (exact WHERE clause not re-transcribed here, deferred to T005 CSV row
   for paragraph 9435-SQL-SELECT-010)
5. Priority: see R-PANDAC-001
6. Calculation: WHEN SQLCODE=0: OMGPR-A-PANDAC ROUNDED = OMGPR-A-TOTAL-COST * CUG29-P-PANDAC-ADJ
   (percent-of-cost, always cost-based — no cost/sell split unlike JIT/label/apply-label percent
   calcs). OMGPR-P-PANDAC-ADJ = CUG29-P-PANDAC-ADJ (rate echoed to output). Expiration date (if
   not null) contributed via 7695-ADD-EXP-DATE-ARRA-010. WHEN SQLCODE=100: CONTINUE (no row,
   fall through to R-PANDAC-001's next fallback). WHEN OTHER: fatal error (DB error #89, table
   CUG29, OMGPR-F-PRICER-ERROR='Y', GO TO 0020-EXIT-PRICER).
7. Output: OMGPR-A-PANDAC, OMGPR-P-PANDAC-ADJ, WS-PANDAC-FND-SW='Y'
8. Dates: CUG29-D-PANDAC-ADJ-EXP contributes to closest-expiration tracking
9. Exclusions/fallbacks: SQLCODE=100 falls to R-PANDAC-001's default-ship-to step
10. Errors: fatal DB error #89 on unexpected SQLCODE
11. Confidence: CONFIRMED for control flow/formula; the exact 9435-SQL-SELECT-010 WHERE clause
    was not re-transcribed in this pass (deferred to T005 CSV)

### R-PANDAC-003 New PANDAC calculation (CMF/CUTFEE fee-component system)
1. ID/Name: R-PANDAC-003 New specific-ship-to PANDAC fee (CUTFEE/CMF)
2. Program/paragraph: 7706-SEL-PANDAC-SPECIF-NEW (17246-17319), calling
   7707-CHECK-PANDAC-ACCT-FLAG (17321-17361, an account/ship-to-level PANDAC eligibility
   re-check against table CUTADR)
3. Preconditions: reached via R-PANDAC-001 step 1, only when a CMF/CUTFEE row exists for this
   ship-to (SQLCODE=0 on 9436-SQL-SELECT-010) AND 7707's own account-flag check confirms
   WS-PANDAC-CUST is true
4. Data deps (T005-confirmed SQL): 9436-SQL-SELECT-010 against table CUTFEE (error-branch DB name
   literal confirms this); 7707's own inline SQL: `SELECT PANDAC_CUST_SW FROM CUTADR WHERE
   COMPANY_ID='OM' AND CUST_ID=:WS-CUST-ID AND ADDR_SUF=:WS-C-SHIP-TO-SUFFIX AND
   (PANDAC_START_DATE <= :WS-CURRENT-DB2-DATE OR PANDAC_START_DATE IS NULL) AND
   (PANDAC_END_DATE >= :WS-CURRENT-DB2-DATE OR PANDAC_END_DATE IS NULL)` — an explicit
   date-effective window on the customer/ship-to's PANDAC participation itself, independent of
   the fee-row's own dates
5. Priority: this whole rule only fires if 7707's account-flag check passes; if the CMF row
   exists but the account/ship-to isn't currently PANDAC-flagged (WS-PANDAC-CUST false), the fee
   type/short-code/SKU/billing-frequency output fields are explicitly cleared to spaces (a
   "row exists but doesn't apply" outcome, distinct from "row doesn't exist")
6. Calculation: two shapes depending on CUFP-FEE-TYPE's own classification:
   (a) Percent-based (FEE-PCT-SALES or FEE-PCT-COST): OMGPR-P-PANDAC-ADJ = CUFP-FEE-PCT / 100
   (source stores the percent as a whole number needing /100 scaling, unlike CUG29's
   already-scaled decimal rate); IF FEE-PCT-COST: OMGPR-A-PANDAC ROUNDED = TOTAL-COST *
   PANDAC-ADJ; IF FEE-PCT-SALES: OMGPR-A-PANDAC ROUNDED = WS-A-SELL-BEFORE-ADJ * PANDAC-ADJ (this
   is the one branch of the whole PANDAC subsystem that supports a sell-based percent, unlike the
   legacy CUG29 path which is cost-only). A **second, distinct** field pair,
   OMGPR-P-PANDAC-PN/OMGPR-A-PANDAC-PN, is also populated here (rate copied, amount forced to 0)
   — this "-PN" pair appears to be a parallel/audit output slot, not consumed further in this
   pass (BLOCKED on its downstream use).
   (b) Flat-fee types (CUFP-FEE-TYPE = 'O' or 'L' or 'Q'): OMGPR-A-PANDAC and OMGPR-A-PANDAC-PN
   both set directly from CUFP-FEE-AMT (already-resolved flat amount, no formula); PANDAC-ADJ and
   PANDAC-PN rate fields zeroed.
   Fee-type/short-code/SKU/billing-frequency descriptor fields (OMGPR-FEE-TYPE-PN etc.) are
   populated from the CUFP row regardless of which shape applied.
7. Output: OMGPR-A-PANDAC, OMGPR-P-PANDAC-ADJ, OMGPR-A-PANDAC-PN, OMGPR-P-PANDAC-PN,
   OMGPR-FEE-TYPE-PN, OMGPR-FEE-SHRT-CODE-PN, OMGPR-SKU-CODE-PN, OMGPR-BILLING-FRQ-PN,
   WS-CMF-PANDAC-FEE-FND-SW='Y', WS-PANDAC-FND-SW='Y' (only when WS-PANDAC-CUST true)
8. Dates: CUFP-D-ADJ-EXP-NI/CUFP-DATE-EXPIRE contributes to closest-expiration tracking (via
   7695-ADD-EXP-DATE-ARRA-010); separately, 7707's CUTADR lookup enforces its own
   PANDAC_START_DATE/PANDAC_END_DATE window on the account's PANDAC participation, independent of
   the fee row's expiration
9. Exclusions/fallbacks: SQLCODE=100 (no CMF row) clears the descriptor fields to spaces and
   falls through to R-PANDAC-001's legacy/default steps; WS-PANDAC-CUST=false (row exists but
   account not currently flagged) also clears descriptors but does NOT set WS-PANDAC-FND-SW,
   allowing the legacy/default fallback to still run
10. Errors: fatal DB error #93 (table CUTFEE, from 7706's own WHEN OTHER) and fatal DB error #707
    (table CUTADR, from 7707's WHEN OTHER)
11. Confidence: CONFIRMED for control flow and both formula shapes; BLOCKED for the -PN field
    pair's downstream consumption (not traced in this pass)

---

## 5. SurgiTrak fee

# SurgiTrak fee — extracted rules (source notes for fees-and-adjustments.md)

Program/paragraph: A6U01.CBL `7758-CALCULATE-SURGITRAK-FEE` (17989-18006), called from
`7195-PRO-ADJ-SELL-010` at lines 12577-12580. Fee-code structure (OMGPR-A/P-SURGITRAK-ST,
OMGPR-FEE-SHRT-CODE-ST, OMGPR-BILLING-FRQ-ST, OMGPR-FEE-TYPE-ST) defined OMGPR.CPY lines 327-344.
Upstream population: CUP100's `2200-GET-SURGITRAK-FEE` (per program-inventory.md §1.5).

### R-SURGITRAK-001 SurgiTrak fee eligibility and calculation
1. ID/Name: R-SURGITRAK-001 SurgiTrak fee eligibility and per-type calculation
2. Program/paragraph: A6U01.CBL 7758-CALCULATE-SURGITRAK-FEE (17989-18006), gated at
   7195-PRO-ADJ-SELL-010 lines 12577-12580
3. Preconditions: OMGPR-C-PRODUCT-TYPE = 'O' (Owens-sourced product, per the same convention
   documented for R-JIT-005/A6O016U) AND OMGPR-FEE-SHRT-CODE-ST > SPACES (a SurgiTrak
   fee-component row was found upstream by CUP100)
4. Data deps: OMGPR-A-SURGITRAK-ST (S9(7)V9(8), flat amount), OMGPR-P-SURGITRAK-ST (S9(3)V9(4),
   percent rate), OMGPR-FEE-TYPE-ST 88-levels (OMGPR-FEE-PER-LINE-ST/-QTY-ST/-ORDER-ST/
   -PCT-SELL-ST/-PCT-COST-ST)
5. Priority: single EVALUATE TRUE, mutually exclusive by fee-type classification (this is the
   simplest of the 10 T009 areas — no cascade, no priority contention, one lookup then one
   calculation)
6. Calculation: WHEN fee-type is per-line, per-qty, OR per-order (three type-codes share one
   WHEN via fall-through EVALUATE branches with no intervening statement): OMGPR-A-SURGITRAK-FEE
   = OMGPR-A-SURGITRAK-ST directly (a resolved flat amount — no quantity multiplication is applied
   here despite "per-qty"/"per-order" naming, meaning any per-unit scaling must have already
   happened upstream in CUP100 when OMGPR-A-SURGITRAK-ST was populated; this paragraph does not
   itself multiply by quantity or line count). WHEN percent-of-cost:
   OMGPR-A-SURGITRAK-FEE ROUNDED = OMGPR-A-TOTAL-COST * OMGPR-P-SURGITRAK-ST. WHEN
   percent-of-sell: OMGPR-A-SURGITRAK-FEE ROUNDED = WS-A-SELL-BEFORE-ADJ * OMGPR-P-SURGITRAK-ST.
7. Output: OMGPR-A-SURGITRAK-FEE
8. Dates: none in this paragraph (not examined for CUP100's 2200-GET-SURGITRAK-FEE upstream date
   filtering — deferred)
9. Exclusions/fallbacks: none within this paragraph; an EVALUATE with no WHEN OTHER means a
   fee-type code outside the five documented values is a silent no-op, consistent with this
   codebase's recurring "unmatched code = do nothing" idiom (see R-JIT-001 item 9)
10. Errors: none in this paragraph
11. Confidence: CONFIRMED

### R-SURGITRAK-002 Line-item vs. separate-billing fold-in
1. ID/Name: R-SURGITRAK-002 SurgiTrak fee fold-in gated by billing frequency
2. Program/paragraph: 7195-PRO-ADJ-SELL-010, lines 12582-12589 (immediately follows R-SURGITRAK-001's
   call site)
3. Preconditions: runs unconditionally after R-SURGITRAK-001 (whether or not it actually computed
   a nonzero fee — the billing-frequency check does not re-test the eligibility gate)
4. Data deps: OMGPR-BILLING-FRQ-ST 88-levels (OMGPR-MONTHLY-AUTO-BILL-ST /
   -MONTHLY-MANUAL-BILL-ST)
5. Priority: this is the same "billed separately vs. embedded in line price" pattern also seen in
   the Distribution/Markup bucket (7095-POPULATE-MARKUP-BKT) — a recurring convention across the
   OMGPR-BILLING-FRQ-xx family of fee sub-structures, not unique to SurgiTrak
6. Calculation: IF OMGPR-BILLING-FRQ-ST > SPACES AND (MONTHLY-AUTO-BILL-ST OR
   MONTHLY-MANUAL-BILL-ST): CONTINUE (do NOT fold into the sell total — this fee is billed
   separately on a monthly cycle, outside the order's line pricing). ELSE (billing frequency is
   blank, or is a non-monthly code such as daily 'DL'/'DS'): COMPUTE OMGPR-A-TOTAL-ADJ-SELL
   ROUNDED = OMGPR-A-TOTAL-ADJ-SELL + OMGPR-A-SURGITRAK-FEE (folded directly into the line's sell
   price adjustment total).
7. Output: OMGPR-A-TOTAL-ADJ-SELL (conditionally incremented)
8. Dates: none
9. Exclusions/fallbacks: monthly-auto/manual billing frequency is the exclusion from line-price
   fold-in; note the fold-in still happens even if R-SURGITRAK-001's gate never fired and
   OMGPR-A-SURGITRAK-FEE is whatever it was previously (presumably zero/uninitialized-safe, not
   independently verified)
10. Errors: none
11. Confidence: CONFIRMED

---

## 6. Low-UOM and break-bulk fee

# Low-UOM / Break-bulk fee — extracted rules (source notes for fees-and-adjustments.md)

**Provenance note:** carried over from `omni-claude`'s prior
`docs/cobol-analysis/decision-tables/low-uom-break-bulk.md` (same MD5-verified `A6U01.CBL`),
re-verified for T009. One prior blocker is now resolved: `7707-CHECK-PANDAC-ACCT-FLAG` (cited
below as BLOCKED in R-LUOM-005) was fully read during this session's PANDAC extraction — see
R-PANDAC-003 in this document. It performs `SELECT PANDAC_CUST_SW FROM CUTADR WHERE
COMPANY_ID='OM' AND CUST_ID=... AND ADDR_SUF=... AND` a PANDAC-date-effective window; `WS-PANDAC-CUST`
is set from the returned flag (blank on SQLCODE=100). This confirms the R-LUOM-005 PANDAC
suppression check is a genuine account+ship-to-level, date-scoped eligibility test, not a static
flag.

Control-flow (CONFIRMED):
```
7225-PRO-JIT-ADJ-010 (precondition: OMGPR-A-JIT-BREAK-FEE=0, LUOM-eligible, stock order)
  -> 0245-PRO-BG-LOW-UOM-010
       [OMGPR-F-LUOM-VEND-EXCL='Y'] -> 7885-FIND-VEND-EXCL-010 (BGG25 vendor-exclusion)
            [excluded AND (LUOM-group OR break-bulk-group)] -> GO TO 0245-EXIT (no charge)
       (always) resolve WS-ACTUAL-C-ORD-UOM / WS-ACTUAL-Q-ORD-LIN-ORDERED
       [OMGPR-Q-ORD-LIN-ORDERED > 1] -> 7872-LOAD-ALTER-UOM -> 7874-FIND-CONV-FACT
       [else]                        -> 7875-FIND-ALT-UOM-DESIGNATOR
       -> 7880-FIND-LUOM-BB-CHG
            [WS-UOM-DESIGNATOR = 'L' or 'B']
                 [OMGPR-FEE-SHRT-CODE-BB > SPACES] -> 7898-CALC-LUOM-OR-BB-FEE-NEW (2022)
                 [else]                            -> 7896-CALC-LUOM-OR-BB-FEE (legacy)
```

### R-LUOM-001 Buy-group vendor exclusion (full suppression gate)
1. ID/Name: R-LUOM-001 Buy-group vendor exclusion
2. Program/paragraph: 0245-PRO-BG-LOW-UOM-010 (6025-6038) + 7885-FIND-VEND-EXCL-010
   (19252-19290)
3. Preconditions: OMGPR-F-LUOM-VEND-EXCL='Y' (set by CUP100's A830-SEL-LOW-UOM from
   BGG23-F-LUOM-VEND-EXCL). **Update (second upload batch): `BGG23.CPY` (DCLGEN TABLE
   `P1.BG_LOW_UOM`) now supplied and read in full — confirms `BGG23-F-LUOM-VEND-EXCL` (X(1)) plus
   sibling fields on the same row not previously documented anywhere in this project:
   `BGG23-P-BG-LOW-UOM`/`BGG23-P-BG-BRK-BULK` (both S9(1)V9(4) COMP-3, the LUOM/break-bulk
   percentage this section's downstream formula (`7896`/`7898`) actually applies — the percentage
   *value*'s upstream source was previously unconfirmed) and `BGG23-F-BG-LOW-UOM-EXCL` (X(1), a
   second, LUOM-specific exclusion flag distinct from `F-LUOM-VEND-EXCL`, not currently cited by
   name anywhere in this document — worth a follow-up trace if `0245`'s SELECT is re-examined).
   `BGG25` (the vendor-exclusion table actually queried by `7885-FIND-VEND-EXCL-010`) remains
   BLOCKED — `BGG23` and `BGG25` are two different tables; do not conflate them.**
4. Data deps: SELECT from BGG25 keyed by OMGPR-I-BUY-GROUP-LUOM, OMGPR-D-BG-LOW-UOM-EFF,
   OMGPR-I-VENDOR. SQLCODE=0 or -811 (duplicate-row-exists reuse pattern) both mean "exclusion
   exists"; 100 or other means "no exclusion" (WHEN OTHER sets WS-BG-LOW-VEXC-EXISTS-SW='N' but
   then still falls through to the same fatal-error path as every other WHEN OTHER in this
   codebase — the 'N' assignment is dead code in that branch, not a real behavioral difference)
5. Priority: first gate in the chain — if it fires, every rule below is skipped via
   GO TO 0245-EXIT. Requires (OMGPR-F-LUOM-GROUP OR OMGPR-F-BREAK-BULK-GROUP) — an account-level
   LUOM/BB source is NOT subject to this vendor-exclusion short-circuit, only group-level
6. Calculation: N/A — gate only
7. Output: none beyond the gate; skips all output fields below
8. Dates: OMGPR-D-BG-LOW-UOM-EFF (query-scoping only)
9. Exclusions/fallbacks: this rule is itself an exclusion mechanism
10. Errors: WHEN OTHER on BGG25 select -> error 301, fatal, GO TO 0020-EXIT-PRICER
11. Confidence: CONFIRMED

**New table surfaced by the second upload batch, not yet tied to any confirmed call site —
flagged per CLAUDE.md rather than silently assumed:** `BGG24.CPY` (DCLGEN TABLE
`P1.BG_LOW_UOM_EXCL`) is a **customer-level LUOM exclusion table**, one level more specific than
`BGG23`'s buy-group-level `F-LUOM-VEND-EXCL`/`F-BG-LOW-UOM-EXCL` flags: `BGG24-I-BUY-GROUP`
(S9(8) COMP), `BGG24-D-BG-LOW-UOM-EFF` (X(10)), `BGG24-I-CUSTOMER` (S9(8) COMP),
`BGG24-D-BG-LUOM-EXCL-EFF`/`-EXP` (X(10), EXP nullable) — i.e. a specific customer, within an
otherwise-LUOM-eligible buy-group, can be individually excluded. No paragraph reading this table
was found among the programs read for this project (`A6U01`, `CUP100`, and the DAO-tier programs);
CUP100's inventory (`program-inventory.md` §4.2/§6.3) does not list a `BGG24` SELECT among its
numbered DB-error paragraphs either. **This is reported as a newly-discovered table whose consumer
is unconfirmed — it must not be assumed to feed R-LUOM-001's vendor-exclusion gate or any other
rule in this document until a consuming paragraph is actually located; it may belong to a program
not in this upload set.**

### R-LUOM-002 Ordered-quantity-dependent UOM-designator resolution
1. ID/Name: R-LUOM-002 UOM designator resolution ('L'/'B'/neither)
2. Program/paragraph: 0245 lines 6074-6082, 7872-LOAD-ALTER-UOM (19050-19156),
   7874-FIND-CONV-FACT (19158-19192), 7875-FIND-ALT-UOM-DESIGNATOR (19194-19233)
3. Preconditions: runs after R-LUOM-001 passes
4. Data deps: OMGPR-Q-ORD-LIN-ORDERED>1 branch loads all VNG05 alt-UOM rows for
   vendor+product into a working array (each factor pre-multiplied by
   WS-BASE-ORD-CONV-FACTOR, itself parsed from VNG02-T-VND-PROD-UM-DESC's numeric prefix,
   lines 6047-6059); if VNG05 has no rows, a synthetic entry uses WS-BASE-ORD-CONV-FACTOR +
   VNG02-F-UOM-DESIGNATOR (product's own base designator). OMGPR-Q-ORD-LIN-ORDERED<=1 branch does
   a single direct VNG05 lookup keyed by the actual order UOM; SQLCODE=100 falls back to the same
   VNG02-F-UOM-DESIGNATOR base value. **Update (second upload batch): RESOLVED, no longer
   INFERRED/BLOCKED — `VNG02.CPY` and `VNG05.CPY` are now supplied with full DCLGEN.**
   `VNG02-T-VND-PROD-UM-DESC` (X(5)) and `VNG02-F-UOM-DESIGNATOR` (X(1), `KS0115` addition)
   confirmed. `VNG05-C-VD-PRD-ALT-UM` (X(2)), `VNG05-A-VD-PRD-ALT-UMF` (S9(6)V9(8) COMP-3, the
   alt-UOM conversion factor this rule's array is built from), `VNG05-F-ALT-UOM-DESIGNATOR` (X(1)),
   plus `VNG05-F-ALT-UNIT-OF-SALE-SW`/`-F-ALT-UNIT-OF-USE-SW` (X(1) each, `NR0625` additions, not
   currently cited by name in this rule) also confirmed.
5. Priority: the two resolution methods are mutually exclusive (single IF/ELSE on quantity), not
   a try-one-then-other cascade — a genuinely different algorithm depending on quantity: qty>1
   searches for a quantity-divides-evenly match across all alternates; qty<=1 does a direct
   lookup on the UOM itself.
6. Calculation: 7874's DIVIDE...REMAINDER is exact-division test (zero remainder), not rounded
   approximation — first array entry whose factor evenly divides WS-ACTUAL-Q-ORD-LIN-ORDERED wins.
   If none divide evenly, WS-UOM-DESIGNATOR stays blank (initialized value) — no charge follows.
7. Output: WS-UOM-DESIGNATOR ('L', 'B', or blank), consumed by R-LUOM-003+
8. Dates: none
9. Exclusions/fallbacks: both VNG05-not-found sub-cases fall back to VNG02-F-UOM-DESIGNATOR
   rather than erroring (same fallback value in both quantity branches)
10. Errors: 7872 cursor open/fetch failure -> errors 701/702 (fatal); array overflow -> 703
    (fatal, WS-MAX-ALTER-UOM-ENTRIES limit not itself read); 7875 select failure -> error 86
    (fatal)
11. Confidence: CONFIRMED for control flow/formulas. INFERRED that VNG02-T-VND-PROD-UM-DESC's
    numeric-prefix parsing always yields a sensible factor (VNG02 DCLGEN not supplied)

### R-LUOM-003 Account-level vs. group-level charge source (legacy path only)
1. ID/Name: R-LUOM-003 Account-vs-group charge source (legacy)
2. Program/paragraph: 7896-CALC-LUOM-OR-BB-FEE, lines 19356-19380 (this split is absent from the
   2022 path, 7898 — see R-LUOM-005)
3. Preconditions: WS-UOM-DESIGNATOR='L' or 'B' (R-LUOM-002); reached only when
   OMGPR-FEE-SHRT-CODE-BB=SPACES (no 2022 fee-component row)
4. Data deps: OMGPR-F-LUOM-ACCOUNT/-GROUP (88-levels on OMGPR-F-LUOM-SW) and
   OMGPR-F-BREAK-BULK-ACCOUNT/-GROUP (88-levels on OMGPR-F-BREAK-BULK-SW) — both set in CUP100's
   A400-GET-JIT-ADJ from CUR120-F-LUOM-ACT-GRP-FEE / CUR120-F-BRK-BULK-ACT-GRP-FEE, ultimately
   from the still-BLOCKED CUP120/CUS120 chain. **Update: `CUR120-F-LUOM-ACT-GRP-FEE` and
   `CUR120-F-BRK-BULK-ACT-GRP-FEE` (both X(1), `HR0415` additions) are now CONFIRMED field shapes
   via the supplied `CUR120.CPY` — the intermediate carrier is known, CUS120's own account-vs-group
   determination logic remains BLOCKED as before.**
5. Priority: for 'L': IF LUOM-ACCOUNT -> use OMGPR-JIT-LUOM-CHRG-AMT (account-specific); ELSE IF
   LUOM-GROUP -> use OMGPR-P-LOW-UOM (buy-group percentage from CUP100's buy-group-priority
   walk); ELSE -> WS-VND-JIT-BB-FEE left unset. For 'B': identical structure with
   OMGPR-JIT-BREAK-CHRG-AMT (account) vs. OMGPR-P-BREAK-BULK (group). **Account takes priority
   over group when both switches are set** (IF/ELSE IF structure) — whether both can be
   simultaneously true is BLOCKED (depends on CUS120's own logic)
6. Calculation: straight MOVE, no computation — feeds R-LUOM-004's formula
7. Output: OMGPR-F-BREAK-BULK-OR-LUOM ('L' or 'B' record), WS-VND-JIT-BB-FEE
8. Dates: none in this fragment
9. Exclusions/fallbacks: "neither switch set" is a silent no-op (same idiom as R-JIT-001/003)
10. Errors: none in this fragment
11. Confidence: CONFIRMED for branching structure; BLOCKED for whether both switches can be
    simultaneously true

### R-LUOM-004 Legacy fee computation (cost-vs-sell from JIT service-fee code)
1. ID/Name: R-LUOM-004 Legacy LUOM/break-bulk fee formula
2. Program/paragraph: 7896-CALC-LUOM-OR-BB-FEE, lines 19390-19407
3. Preconditions: WS-VND-JIT-BB-FEE > 0 after R-LUOM-003, and after 7227-CHK-FOR-OVERRIDES runs
   again here (a second, independent call to the same override cascade documented as BLOCKED in
   the JIT section)
4. Data deps: none new
5. Priority: `IF (OMGPR-JIT-SERVICE-FEE='A' OR 'C') AND OMGPR-C-PRIVATE-LBL NOT='O' -> cost-based
   (TOTAL-COST * WS-VND-JIT-BB-FEE); ELSE -> sell-based (SELL-BEFORE-ADJ * WS-VND-JIT-BB-FEE)` —
   identical test/formula shape to every other JIT category (R-JIT-002/005). Confirms this legacy
   LUOM/break-bulk fee is conceptually a fifth JIT-fee category living in a separate paragraph.
6. Calculation: COMPUTE...ROUNDED, standard round-half-up
7. Output: OMGPR-P-ACTUAL-BB-OR-LUOM-PCT, OMGPR-A-JIT-BREAK-FEE — the SAME output field 7225's
   dead inline break-bulk code would have written (confirms true relocation, not a new bucket)
8. Dates: none
9. Exclusions/fallbacks: WS-VND-JIT-BB-FEE=0 skips the whole block, OMGPR-A-JIT-BREAK-FEE stays
   at its prior (typically zero) value
10. Errors: none in this fragment
11. Confidence: CONFIRMED

### R-LUOM-005 2022 fee-component path (PANDAC suppression, flat bypass, divergent cost/sell test)
1. ID/Name: R-LUOM-005 2022 fee-component LUOM/break-bulk fee (PANDAC-aware)
2. Program/paragraph: 7898-CALC-LUOM-OR-BB-FEE-NEW, lines 19412-19513
3. Preconditions: OMGPR-FEE-SHRT-CODE-BB > SPACES (a 2022 CUTPRICE_COMPONENT row exists),
   reached via 7880's gate
4. Data deps: no new SQL in 7898 itself; calls 7707-CHECK-PANDAC-ACCT-FLAG (now CONFIRMED, see
   provenance note above and R-PANDAC-003) and 7227-CHK-FOR-OVERRIDES (still out of scope)
5. Priority, in the order the code checks them:
   1. **PANDAC suppression** (lines 19414-19426): IF VNG01-F-VEND-PANDAC='Y' (VNG01 DCLGEN still
      BLOCKED) AND VNG02-F-PANDAC-ITEM='Y' (**now CONFIRMED**, `X(1)`, `RP0511` addition per the
      supplied `VNG02.CPY` — matches `program-inventory.md`'s `NK0810`/`RP0511` field-history
      reading exactly) AND WS-PANDAC-CUST (now confirmed via CUTADR/7707, date-scoped) ->
      every 2022 break-bulk/LUOM fee-component field is blanked/zeroed and the paragraph exits
      immediately — **no break-bulk/LUOM charge of any kind for a PANDAC-eligible
      vendor+item+customer combination, full stop.** PANDAC pricing and break-bulk/LUOM fees do
      not coexist on the same line via the 2022 fee-component path.
   2. **Flat-fee bypass** (lines 19436-19447, same pattern as R-JIT-004): IF
      OMGPR-FEE-PER-LINE-BB OR -PER-ORDER-BB OR -PER-QTY-BB -> for 'L':
      OMGPR-A-JIT-BREAK-FEE = OMGPR-A-LUM-FEE-AMT-BB directly; for 'B': = OMGPR-A-BREAKBULK-BB
      directly — no percent calc, no 7227 override check for this fee (exits via GO TO 7898-EXIT
      before the 7227 call at line 19477, confirmed by control flow)
   3. **Percent-based** (lines 19449-19493, reached only if neither above applied): source is
      'L' -> OMGPR-P-LUM-FEE-PCT-BB if (PCT-SELL-BB OR PCT-COST-BB), else zero; 'B' ->
      OMGPR-P-BREAKBULK-BB under the same test, else zero — no account-vs-group split (unlike
      R-LUOM-003's legacy path). 7227-CHK-FOR-OVERRIDES runs (19477-19478), then: IF
      OMGPR-FEE-PCT-COST-BB AND PRIVATE-LBL NOT='O' -> cost-based; ELSE -> sell-based. **This
      cost-vs-sell test uses OMGPR-FEE-PCT-COST-BB (the 2022 row's own type flag), NOT
      OMGPR-JIT-SERVICE-FEE** — a genuine, confirmed divergence from the legacy path and every
      other JIT-area cost/sell decision: an account's JIT service-fee type can say "bill at
      cost" while its 2022 break-bulk component is configured percent-of-sell, so the LUOM/BB
      portion computes from sell even though the rest of the line computes from cost (or vice
      versa). **Single most important behavioral subtlety in this section** for anyone
      validating a migrated implementation — do not assume "cost-based JIT" is an all-or-nothing
      per-line property.
6. Calculation: same COMPUTE...ROUNDED shape as R-LUOM-004 for the percent sub-case; direct MOVE
   (no rounding) for the flat-fee bypass
7. Output: same as R-LUOM-004, plus the PANDAC-suppression case's cleared fee-component fields
8. Dates: none directly (7707's own CUTADR date window covered in R-PANDAC-003)
9. Exclusions/fallbacks: neither-percent-type-nor-flat-type forces WS-VND-JIT-BB-FEE=0 (same
   silent-zero idiom)
10. Errors: none raised directly in 7898
11. Confidence: CONFIRMED for the three-tier priority and the cost/sell divergence. PANDAC gate
    is now CONFIRMED end-to-end (7707/CUTADR read this session, see R-PANDAC-003) rather than
    BLOCKED as in the prior omni-claude version of this document.

---

## 7. Label / application fee

# Label / application fee — extracted rules (source notes for fees-and-adjustments.md)

**Finding (CONFIRMED by exhaustive search):** "Label" and "Application" are not independent fee
areas in A6U01.CBL — there is no standalone label/application computation paragraph, and
`OMGPR-BILLING-FRQ-AL` (the Apply-Label fee-code structure's billing-frequency field, OMGPR.CPY
lines 367-384) is never referenced anywhere in A6U01.CBL, unlike the equivalent SurgiTrak/
Distribution-Markup/BG-freight structures which all have a billing-frequency-gated fold-in step.
Both "print label" and "apply label" are fully subsumed as two of the four fee sub-categories
inside the JIT paragraph (`7225-PRO-JIT-ADJ-010`), already extracted in this document's JIT
section:

- **Print label** ("CALC JIT PRINT LABEL FEE #2", lines 13132-13192) — percent/rate/flat-per-item
  calculation, see R-JIT-002 item 5, second bullet ("Rate-based (#2/#3, mirrored...)") and third
  bullet ("Per-item flat rate"). Output: `OMGPR-A-JIT-LABEL-FEE`. No 2022 fee-component fast path
  exists for this category (confirmed by absence, R-JIT-004 item 11) — it always uses the legacy
  `OMGPR-JIT-LABEL-CHRG-AMT`/`WS-VND-JIT-LB-FEE` source.
- **Apply label** ("CALC JIT APPLY LABEL FEE #3", lines 13193-13262) — same percent/rate/flat
  structure as print label, PLUS a 2022 fee-component fast path (`OMGPR-FEE-SHRT-CODE-AL`/
  `OMGPR-A-APPLY-LAB-AL`) that bypasses the override/percent/rate cascade entirely when a
  per-line/per-order/per-qty fee-component row exists — see R-JIT-004 item 3 ("Apply-label").
  Output: `OMGPR-A-JIT-APPLY-FEE`.

Both fee amounts fold into the same `OMGPR-A-JIT-COST`/`OMGPR-A-JIT-SELL` total via R-JIT-006 —
there is no separate line-item-vs-monthly-billing fold-in decision for label/application the way
there is for SurgiTrak (R-SURGITRAK-002) or Distribution/Markup, consistent with
`OMGPR-BILLING-FRQ-AL` never being read.

**No new rules are extracted in this section** — R-JIT-002 and R-JIT-004 (JIT section, above) are
the authoritative source for label/application fee computation. This section exists to record the
negative finding (no independent paragraph) so T009's "label/application" scope item is not
mistaken for an unexamined gap.

---

## 8. Delivery fee

# Delivery fee — extracted rules (source notes for fees-and-adjustments.md)

Program/paragraph: A6U01.CBL `0200-SEL-DELIVERY-010` (4497-4547, default ship-to, cascades to
`7800-SEL-DELIVERY-010` on miss), `0202-SEL-DELIVERY-CID-010` (4549-4591, CID fallback),
`7800-SEL-DELIVERY-010` (18515-18559, specific ship-to). Called from `7195-PRO-ADJ-SELL-010`
(lines 12481-12488). All three compute an identical formula against table CUG25 (account/ship-to
level) or CUG71/`CID_DELIV_ADJ` (customer-ID level, T005-confirmed real table name).

### R-DELIVERY-001 Account/ship-to delivery adjustment (default-then-specific cascade)
1. ID/Name: R-DELIVERY-001 Account-level delivery adjustment, default-ship-to-first
2. Program/paragraph: 0200-SEL-DELIVERY-010 (4497-4547) cascading to 7800-SEL-DELIVERY-010
   (18515-18559) on miss
3. Preconditions: none beyond normal sell-adjustment processing reaching this point
4. Data deps (T005-confirmed SQL, table CUG25): 0200's own SELECT (via 9035-SQL-SELECT-010) is
   documented by its header comment as checking the default ("000") ship-to row first; on
   SQLCODE=100 (no default row), 7800 performs a second SELECT against the same CUG25 table (via
   9515-SQL-SELECT-010) — CONFIRMED by control flow that the fallback exists and targets the same
   table; the exact WHERE-clause distinction between the two (default-ship-to-suffix vs.
   specific-ship-to-suffix keying) is INFERRED from the header comment
   ("1. CHECK FOR SHIP TO (000)... 2. CHECK FOR THE SPECIFIED SHIP TO (200 TO 999)") rather than
   independently re-verified against the two SELECT statements' literal WHERE clauses in this
   pass — deferred to the T005 SQL inventory CSV rows for 9035-SQL-SELECT-010/9515-SQL-SELECT-010
5. Priority: default ship-to (000) checked first; specific ship-to only attempted if the default
   row is absent (SQLCODE=100) — this is the OPPOSITE cascade order from PANDAC (R-PANDAC-001),
   which tries specific-ship-to first and falls back to default. Worth noting as a
   codebase-internal inconsistency between two structurally similar ship-to-scoped fee lookups,
   not a documentation error on this task's part.
6. Calculation: OMGPR-A-DELIVERY-ADJ ROUNDED = WS-A-SELL-BEFORE-ADJ * CUG25-P-DELIVERY-ADJ — a
   sell-based percentage in both the default and specific-ship-to cases (identical formula in
   0200 and 7800). **No cost-based variant exists for delivery** — unlike freight/JIT/PANDAC/
   LUOM, there is no service-fee-code-driven cost-vs-sell branch here; delivery is always
   sell-based.
7. Output: OMGPR-A-DELIVERY-ADJ, OMGPR-P-DELIVERY-ADJ
8. Dates: CUG25-D-DELIVERY-ADJ-EXP contributes to closest-expiration tracking (both sites)
9. Exclusions/fallbacks: SQLCODE=100 at the default level triggers the specific-ship-to fallback
   (7800); SQLCODE=100 at the specific level (within 7800) is a final CONTINUE — no delivery
   adjustment at all, falls through to R-DELIVERY-002 at the caller
10. Errors: fatal DB error #92 (0200, table CUG25) / #54 (7800, table CUG25) on unexpected
    SQLCODE — note two different error-number literals for what is structurally the same table,
    reflecting two independently-coded SELECT sites rather than a shared error path
11. Confidence: CONFIRMED for control flow and formula; INFERRED for the exact default-vs-specific
    WHERE-clause distinction (not independently re-verified against SQL text this pass)

### R-DELIVERY-002 CID-level delivery adjustment (fallback)
1. ID/Name: R-DELIVERY-002 Customer-ID-level delivery adjustment
2. Program/paragraph: 0202-SEL-DELIVERY-CID-010, lines 4549-4591; dispatched from the caller at
   7195-PRO-ADJ-SELL-010 lines 12484-12488 only `IF SQLCODE = +100` after R-DELIVERY-001's entire
   cascade (both default and specific ship-to) returned not-found
3. Preconditions: R-DELIVERY-001 exhausted with no match at either ship-to level
4. Data deps (T005-confirmed table name): SELECT via 9036-SQL-SELECT-010 against
   `CID_DELIV_ADJ` (aliased CUG71 in host variables) — T005 SQL inventory confirms this is the
   real table name (distinct from the CUG71 copybook-style alias used in working-storage)
5. Priority: last resort in the delivery-adjustment cascade — R-DELIVERY-001 (account/ship-to,
   both sub-levels) always takes precedence
6. Calculation: identical formula shape to R-DELIVERY-001 — OMGPR-A-DELIVERY-ADJ ROUNDED =
   WS-A-SELL-BEFORE-ADJ * CUG71-P-DELIVERY-ADJ (sell-based only, same as account/ship-to level)
7. Output: OMGPR-A-DELIVERY-ADJ, OMGPR-P-DELIVERY-ADJ (overwrites whatever R-DELIVERY-001 left,
   though R-DELIVERY-001 would only have left default/uninitialized values here since this path
   is only reached on a full miss)
8. Dates: CUG71-D-DELIVERY-ADJ-EXP contributes to closest-expiration tracking
9. Exclusions/fallbacks: SQLCODE=100 here is a final CONTINUE — no delivery adjustment for this
   line at all; no further fallback exists beyond this
10. Errors: fatal DB error #203, table CID_DELIV_ADJ, on unexpected SQLCODE
11. Confidence: CONFIRMED

---

## 9. Surcharge fee

# Surcharge fee — extracted rules (source notes for fees-and-adjustments.md)

Program/paragraph: A6U01.CBL `7210-PRO-SURCHARGE-010` (12868-12915, entry/output) ->
`0215-SURCHARGE-DTLS-010` (4767-5243, the primary account/CID/division/corp cascade) ->
`0250-PRO-BG-SURCHG-010` (6090-6154, secondary buy-group cascade, reached only if 0215's cascade
fully misses) -> `0288`/`0290`/`0292`/`0295` (BG sub-levels, 6905-7154) and `7288`/`7293`
(product-category BG variants, 14283-14415). Fee-code structure: "Standard Terms and Surcharges"
(TS), OMGPR.CPY lines 392-398.

**Methodology note:** 0215 alone is 476 lines implementing a deep, highly repetitive cascade. Its
full priority ladder was extracted **mechanically** by enumerating every `SET A9G36-<status>` /
`SET A9G36-<status> TO TRUE` statement in source order (a legitimate, verifiable technique — each
such statement marks exactly one terminal outcome of the cascade) rather than by transcribing all
476 lines by hand. The formula and error-handling shape of each of the 16 branches was spot-
verified on the first four branches (lines 4827-4924, reproduced below) and found identical in
structure (SELECT against a `VNGnn` table, `WHEN +0` sets the status and exits immediately,
`WHEN +100` falls through to the next branch, `WHEN OTHER` is a fatal DB error) — this pattern is
assumed, not individually re-verified, for the remaining 12 branches. Flagged as INFERRED-by-
pattern for those 12, not independently confirmed line-by-line.

### R-SURCHARGE-001 Surcharge lookup priority cascade
1. ID/Name: R-SURCHARGE-001 Sixteen-level (+ BG fallback) surcharge source cascade
2. Program/paragraph: 0215-SURCHARGE-DTLS-010 (4767-5243), falling back to 0250-PRO-BG-SURCHG-010
   (6090-6154) and its sub-paragraphs on a full miss
3. Preconditions: A9G36-I-DIVISION and A9G36-I-VENDOR must both be populated (blank division or
   vendor is an immediate hard failure, not a fallback — SET A9G36-SURCHARGE-FAILURE, GO TO
   0215-EXIT); account context must resolve via A9G36-I-ACCOUNT, or A9G36-S-ACCOUNT, or
   A9G36-CUSTOMER-NBR (first non-blank/non-zero of the three) else also a hard failure
4. Data deps (tables identified by their GET-paragraph naming convention, T005-cross-checkable):
   VNG35 (account-level product-category surcharge, via 9700-GET-VNG35-ROW), VNG16 (account-level
   general surcharge, via 7835-GET-VNG16-ROW-010), VNG37 (CID-level product-category, via
   9710-GET-VNG37-ROW), a CID-level general-surcharge table (unnamed in this pass), VNG33
   (division-level product-category, via 9720-GET-VNG33-ROW), a division-level general table,
   VNG32 (corp-level product-category, via 9730-GET-VNG32-ROW), a corp-level general table
5. Priority (CONFIRMED by mechanical SET-statement enumeration, first-match-wins, sequential):
   **Tier A — Account/CID/Division/Corp cascade (0215), four scope levels, each trying
   product-category-specific before general, and vendor-specific before default-vendor, in this
   exact order:**
   1. Account + product-category + specific-vendor (PC-SURCHG-ACCT-VEND)
   2. Account + product-category + default-vendor (PC-SURCHG-ACCT-DEF)
   3. Account + general + specific-vendor (SURCHARGE-ACCT-VEND)
   4. Account + general + default-vendor (SURCHARGE-ACCT-DEF)
   5. CID + product-category + specific-vendor (PC-SURCHG-CID-VEND)
   6. CID + product-category + default-vendor (PC-SURCHG-CID-DEF)
   7. CID + general + specific-vendor (SURCHARGE-CID-VEND)
   8. CID + general + default-vendor (SURCHARGE-CID-DEF)
   9. Division + product-category + specific-vendor (PC-SURCHG-DIV-VEND)
   10. Division + product-category + default-vendor (PC-SURCHG-DIV-DEF)
   11. Division + general + specific-vendor (SURCHARGE-DIV-VEND)
   12. Division + general + default-vendor (SURCHARGE-DIV-DEF)
   13. Corp + product-category + specific-vendor (PC-SURCHG-CORP-VEND)
   14. Corp + product-category + default-vendor (PC-SURCHG-CORP-DEF)
   15. Corp + general + specific-vendor (SURCHARGE-CORP-VEND)
   16. Corp + general + default-vendor (SURCHARGE-CORP-DEF)
   The product-category sub-branch at each level is only attempted when OMGPR-S-PROD-CATEGORY > 0
   (lines 4829, and the equivalent guards at each of the other three levels); a failure
   (SQLCODE OTHER) at ANY step is an immediate hard-stop (GO TO 0215-EXIT with
   A9G36-SURCHARGE-FAILURE set) — it does NOT fall through to the next tier the way a plain
   not-found (SQLCODE 100) does.
   **Tier B — Buy-group cascade (0250), reached only if all 16 of Tier A miss cleanly (no
   failure, just SQLCODE=100 throughout):** per 0250's own header comment (CONFIRMED accurate
   against the code, lines 6094-6112): search the account's effective buy-groups starting from
   lowest priority number; if a BG belongs to a non-01 division, try BG+vendor then BG+default-
   vendor, then repeat for parent BG, then grandparent BG, then repeat the whole climb at the next
   priority level, as long as division stays non-01; if a BG belongs to division 01 and no hit is
   found through that climb, fall through to the customer/CID-buy-group search instead (structured
   the same way, with its own product-category-first sub-check per BG level via 7288/7293). This
   is the SECOND independently-coded priority cascade in this rule area (Tier A's four-scope-level
   cascade being the first) — a genuinely two-tier system: named-scope lookup (account/CID/
   division/corp) first, buy-group-membership lookup second, only if the first tier exhausts
   completely clean.
6. Calculation: see R-SURCHARGE-002 (the formula itself lives in the caller, 7210, using
   whichever A9G36-P-SURCHARGE percentage the winning branch populated)
7. Output: A9G36-C-SURCHARGE-STATUS ('01'=found by default/initialization — note the field is
   pre-set to '01' at the top of 0215 before any branch runs, meaning **status defaults to
   "success" and no branch actually needs to explicitly confirm success**, only failure paths
   change it), A9G36-P-SURCHARGE (the winning percentage), A9G36-C-SURCHARGE-SOURCE
8. Dates: A9G36-D-PRICING passed into every lookup as the effective-date filter (exact per-branch
   WHERE-clause date columns not individually re-verified across all 16, consistent with this
   rule's INFERRED-by-pattern caveat)
9. Exclusions/fallbacks: a hard failure (blank division/vendor/account, or any SQLCODE OTHER at
   any tier) aborts the whole surcharge determination rather than falling back further — this is
   stricter than the freight/PANDAC/delivery cascades, which treat an individual step's failure as
   fatal-to-the-whole-pricer (GO TO 0020-EXIT-PRICER) rather than fatal-to-just-this-fee; here a
   surcharge-specific failure is scoped to A9G36-SURCHARGE-FAILURE and does NOT abort the whole
   pricer run by itself (see R-SURCHARGE-002 item 5 for how the caller reacts)
10. Errors: A9G36-SURCHARGE-FAILURE is a soft/local failure indicator, not necessarily a full
    pricer abend — see R-SURCHARGE-002
11. Confidence: CONFIRMED for the priority order (mechanically enumerated from source) and for the
    first four branches' internal formula shape (directly read). INFERRED-by-pattern (not
    individually re-verified) for branches 5-16's internal SQL/formula detail, and for Tier B's
    exact internal formula (0250's own sub-paragraphs 0288/0290/0292/0295/7288/7293 were only
    read at the dispatcher level, not line-by-line for their SQL).

### R-SURCHARGE-002 Surcharge fold-in and price-lock bypass
1. ID/Name: R-SURCHARGE-002 Surcharge amount calculation, fold-in, and price-lock exclusion
2. Program/paragraph: 7210-PRO-SURCHARGE-010, lines 12868-12915
3. Preconditions: **NOT OMGPR-PRICE-LOCKED** — a price-locked line skips surcharge determination
   entirely (GO TO 7210-EXIT before even populating the A9G36 lookup keys). This is the first
   confirmed interaction in this document set between the "price lock" concept (named in T009's
   own acceptance criteria as belonging to sell-selection rules) and a specific fee area: locked
   prices do not have surcharges applied/recomputed.
4. Data deps: OMGPR-A-TOTAL-COST, A9G36-P-SURCHARGE (from R-SURCHARGE-001's winning branch),
   OMGPR-BILLING-FRQ-TS (billing-frequency field of the Terms-and-Surcharges fee-code structure)
5. Priority: runs once, after R-SURCHARGE-001's full cascade returns
6. Calculation: only IF A9G36-C-SURCHARGE-STATUS = '01' (recall this is the pre-set default,
   meaning it evaluates true unless a hard failure changed it): OMGPR-P-SURCHARGE =
   A9G36-P-SURCHARGE (rate copied to output); OMGPR-C-SURCHARGE = A9G36-C-SURCHARGE-SOURCE
   (records which of the 16+ tiers won); OMGPR-A-SURCHARGE = OMGPR-A-TOTAL-COST *
   OMGPR-P-SURCHARGE (always cost-based — no cost/sell option, similar to PANDAC and unlike
   freight/JIT which support both). **Note: the surcharge amount COMPUTE (line 12897) has no
   ROUNDED keyword** — unlike virtually every other fee formula in this codebase (rebates,
   freight, JIT, PANDAC, SurgiTrak, delivery all explicitly say ROUNDED), this one truncates
   instead of rounding. Flagged as a genuine formula-level inconsistency worth preserving exactly
   in any reimplementation, not normalizing to ROUNDED by assumption.
7. Output: OMGPR-P-SURCHARGE, OMGPR-C-SURCHARGE, OMGPR-A-SURCHARGE, and conditionally
   OMGPR-A-TOTAL-SELL (see item 9)
8. Dates: none directly in this paragraph (dates belong to R-SURCHARGE-001's lookups)
9. Exclusions/fallbacks: same billing-frequency fold-in gate seen for SurgiTrak (R-SURGITRAK-002)
   and the Distribution/Markup bucket — IF OMGPR-BILLING-FRQ-TS > SPACES AND
   (MONTHLY-AUTO-BILL-TS OR MONTHLY-MANUAL-BILL-TS): CONTINUE (billed separately, not folded into
   the line price). ELSE: OMGPR-A-TOTAL-SELL = OMGPR-A-TOTAL-SELL + OMGPR-A-SURCHARGE (folded in,
   note: NOT rounded here either). A9G36-T-ERROR-MSG is copied to OMGPR-ERROR-MESSAGE
   unconditionally at the end (line 12911) regardless of success/failure — even a successful
   surcharge determination overwrites OMGPR-ERROR-MESSAGE with A9G36's (blank, on success) message
   field, meaning **a prior, unrelated error message in OMGPR-ERROR-MESSAGE would be silently
   wiped by a successful surcharge lookup** — flagged as a potential cross-feature side effect
   worth verifying against the field's actual lifecycle before assuming error messages are
   additive/preserved across paragraphs.
10. Errors: **CONFIRMED, and a genuine outlier in this codebase's error-handling conventions:**
    unlike every other fee area in this document (freight, PANDAC, delivery, SurgiTrak, JIT),
    where a `WHEN OTHER` SQLCODE branch calls `GO TO 0020-EXIT-PRICER` (a full pricer abend),
    0215's `WHEN OTHER` branches (e.g. lines 4839-4845) only `MOVE SQLCODE`/error text into
    A9G36 fields, `SET A9G36-SURCHARGE-FAILURE TO TRUE`, and `GO TO 0215-EXIT` — control returns
    normally to 7210, which never calls `GO TO 0020-EXIT-PRICER` either (it just checks
    `A9G36-C-SURCHARGE-STATUS = '01'`, which the failure left unset). **A DB2 error during
    surcharge lookup does not abend the pricer** — it silently degrades to "no surcharge applied
    this line," with only the error text (briefly) available in OMGPR-ERROR-MESSAGE (and then
    only until the next paragraph overwrites it, since nothing else reads it or halts on it in
    the code read this pass). Flagged as a deliberate-looking but undocumented severity difference
    from the rest of the pricer's fatal-by-default DB2-error convention — worth confirming with
    the business whether a silently-skipped surcharge is acceptable behavior or an existing latent
    bug, since it means a transient DB2 issue could silently under-charge an order rather than
    failing loudly the way freight/JIT/PANDAC/delivery do under the same failure class.
11. Confidence: CONFIRMED

---

## 10. Distribution and markup fee

# Distribution / Markup fee — extracted rules (source notes for fees-and-adjustments.md)

**Finding:** T009 lists "distribution" and "markup" as two separate scope items, but the evidence
shows a single combined mechanism — the "Standard Dist Markup" fee-code family in OMGPR.CPY
(lines 399-433: five sub-types GS/GN/MI/MN/MC), computed by one paragraph
(`7095-POPULATE-MARKUP-BKT`, lines 10744-10802) fed by a classification cascade inside
`7090-PRO-SELL-AMTS-010`'s "Gross Margin" sell-price method (lines 10284-10417, and a second,
structurally near-identical invocation at 10419-10535 not separately re-transcribed here — both
funnel into the same 7095 paragraph and 5-category classification). This document treats them as
one rule area rather than fabricating an artificial split the source doesn't support.

### R-DISTMKP-001 Five-way markup-category classification
1. ID/Name: R-DISTMKP-001 Group-sanction/non-sanction/individual/non-contract/custom
   classification for the Gross-Margin sell method
2. Program/paragraph: 7090-PRO-SELL-AMTS-010, "GROSS MARGIN PRICING PERCENTAGE - SELL METHOD #1"
   section, lines 10284-10417 (new-cost-contract-columns sub-case at 10301-10344 is the
   authoritative 5-way cascade; STOCK and USAGE sub-sections at 10350-10379 use a coarser 2-way
   version of the same classification, see item 5)
3. Preconditions: WS-SELL-ARR-FND AND WS-C-SELL-PRC-METHOD = WS-GROSS-MARGIN-IND (this account's
   sell arrangement uses the Gross Margin pricing method); further gated by
   OMGPR-C-ACCT-PRCE-METHOD (Cost-Contract / Stock / Usage sub-method)
4. Data deps: VNG02-C-CUSTOM-IND, WS-COST-CONT-FND, CCG09-I-RPT-GRP, OMGPR-F-GRP-CNT-FEES,
   BGG01-I-DIVISION, WS-INDIV-CNT, ING01-F-STOCK-ITEM, WS-F-SPECIFIED-USAGE (via
   7170-SEL-SPEC-VER-010)
5. Priority (CONFIRMED, nested IF/ELSE, first-match-wins — **this is the same custom -> group-
   sanctioned -> division-01-non-sanctioned -> individual/non-div-01 -> non-contract cascade
   shape already documented for R-FREIGHT-006's exemption check**, applied here to select a
   gross-margin PERCENTAGE and a markup-bucket CLASSIFICATION rather than an exemption flag — a
   third confirmed reuse of this same customer-classification pattern in this codebase, alongside
   freight exemption and, structurally, surcharge's account/CID/division/corp axis):
   Cost-contract sub-case (the authoritative, full 5-way version):
   1. Custom account (VNG02-C-CUSTOM-IND='Y' AND a valid custom gross-margin % on file) ->
      WS-P-SELL-GRSSMGN-CUS -> classified MC (custom)
   2. Else, if cost contract found: group-sanctioned (CCG09-I-RPT-GRP>0 OR
      OMGPR-F-GRP-CNT-FEES='Y') -> WS-P-SELL-GRSSMGN-SAN -> classified GS
   3. Else division-01 non-sanctioned (BGG01-I-DIVISION='01' AND GRP-CNT-FEES='N') ->
      WS-P-SELL-GRSSMGN-NSC -> classified GN
   4. Else individual/non-div-01 (WS-INDIV-CNT OR division<>'01') -> WS-P-SELL-GRSSMGN-IND ->
      classified MI (individual)
   5. Else (cost contract found but none of 2-4 match): **hard fatal error** — "INVALID
      WS-COST-CONTRACT SCENARIO FOR GRSSMGN", error #602, GO TO 0020-EXIT-PRICER. Unlike the
      analogous freight-exemption cascade (R-FREIGHT-006), which silently falls to "not exempt"
      when no case matches, this classification treats an unmatched case as a genuine data-
      integrity error worth aborting on, not a safe default.
   6. Else (no cost contract at all): WS-P-SELL-GRSSMGN-NON -> classified MN (non-contract)
   Stock/Usage sub-cases (coarser, 2-way only, run independently of and in addition to whichever
   cost-contract branch fired — CONFIRMED by the absence of ELSE/GO TO linking them to the
   cost-contract IF block above): stock item found (ING01-F-STOCK-ITEM='Y') or specified-usage
   found -> WS-P-SELL-GRSSMGN-SAN, classified GS; otherwise -> WS-P-SELL-GRSSMGN-NON, classified
   MN. Because these run unconditionally (gated only by OMGPR-C-ACCT-PRCE-METHOD, a different
   field than the cost-contract branch's own gate), **a line could be classified twice in
   sequence** if OMGPR-C-ACCT-PRCE-METHOD somehow matched more than one of Cost-Contract/Stock/
   Usage — the last classification to run wins (same last-write-wins hazard already documented
   for R-REBATE-001). Whether OMGPR-C-ACCT-PRCE-METHOD's values are mutually exclusive by
   convention was not independently verified (its full domain was not re-derived in this pass).
6. Calculation: OMGPR-PRICING-PERCENTAGE is set to whichever WS-P-SELL-GRSSMGN-* rate applies,
   capped at 0.9999 if it would otherwise be >= 1 (line 10408-10410, guards against a
   divide-by-zero/negative denominator in the sell-price formula below). Sell price itself:
   OMGPR-A-CUS-UOM-SELL-PRC ROUNDED = OMGPR-A-TOTAL-COST / (1 - OMGPR-PRICING-PERCENTAGE) — this
   is a margin-based (not markup-based) formula: the percentage represents target gross margin as
   a fraction of sell price, not a multiplier on cost.
7. Output: OMGPR-PRICING-PERCENTAGE, OMGPR-A-CUS-UOM-SELL-PRC, OMGPR-PRICING-METHOD='GM',
   WS-MARKUP-{CUS-MC,GRP-SAN-GS,GRP-NSC-GN,IND-MI,NON-CON-MN} classification flag
8. Dates: none in this fragment
9. Exclusions/fallbacks: custom-account path requires BOTH the custom flag AND a populated
   custom gross-margin percentage (WS-P-SELL-GRSSMGN-CUS-NI NOT = -1, a null-indicator check) —
   a custom account without a configured custom percentage falls through to the cost-contract
   cascade below it rather than using the custom branch
10. Errors: fatal error #602 on an unmatched cost-contract classification (item 5, case 5)
11. Confidence: CONFIRMED

### R-DISTMKP-002 Markup bucket population and separate-billing exclusion
1. ID/Name: R-DISTMKP-002 Distribution/markup fee amount and billed-separately reversal
2. Program/paragraph: 7095-POPULATE-MARKUP-BKT, lines 10744-10802
3. Preconditions: called immediately after R-DISTMKP-001 classifies the line (both invocation
   sites, 10416 and ~10535)
4. Data deps: WS-MARKUP-* classification (R-DISTMKP-001), OMGPR-A-CUS-UOM-SELL-PRC,
   OMGPR-A-TOTAL-COST, OMGPR-BILLING-FRQ-{GS,GN,MI,MN,MC} (per-sub-type billing-frequency fields,
   OMGPR.CPY lines 399-433)
5. Priority: single EVALUATE TRUE on the classification set by R-DISTMKP-001, mutually exclusive
   by construction (only one WS-MARKUP-* 88-level can be true at a time, since they share one
   underlying field)
6. Calculation: for whichever category is set, e.g. GS: OMGPR-A-MKP-GRP-SAN-GS =
   OMGPR-A-CUS-UOM-SELL-PRC - OMGPR-A-TOTAL-COST (the markup amount, derived by subtracting cost
   from the already-computed margin-based sell price — i.e., this is NOT an independent fee
   calculation, it is a decomposition/labeling of margin already embedded in
   OMGPR-A-CUS-UOM-SELL-PRC by R-DISTMKP-001's formula). Same pattern for all five categories,
   writing to the category-specific OMGPR-A-MKP-* field.
7. Output: OMGPR-A-MKP-{GRP-SAN-GS,GRP-NSC-GN,IND-MI,NON-CON-MN,CUS-MC} (whichever category
   matched), and conditionally OMGPR-A-CUS-UOM-SELL-PRC itself (see item 9)
8. Dates: none in this paragraph
9. Exclusions/fallbacks: **this is the inverse of the SurgiTrak/Surcharge billing-frequency
   pattern (R-SURGITRAK-002).** For SurgiTrak/Surcharge, a fee computed separately from the base
   price is conditionally ADDED to the total UNLESS billed monthly. Here, the margin is already
   BAKED INTO OMGPR-A-CUS-UOM-SELL-PRC by R-DISTMKP-001's cost/(1-pct) formula; if this category's
   billing frequency is monthly-auto or monthly-manual (OMGPR-MONTHLY-AUTO-BILL-GS /
   -MANUAL-BILL-GS, etc.), the just-computed markup amount is SUBTRACTED BACK OUT of
   OMGPR-A-CUS-UOM-SELL-PRC (`OMGPR-A-CUS-UOM-SELL-PRC = OMGPR-A-CUS-UOM-SELL-PRC -
   OMGPR-A-MKP-GRP-SAN-GS`, lines 10753-10755 and equivalent for each category) — i.e., the sell
   price reverts to approximately cost, and the markup is billed separately instead. **Flagged
   explicitly since the mechanics are opposite in direction from the otherwise-similar
   SurgiTrak/Surcharge fold-in pattern** — add-if-not-monthly vs. subtract-back-if-monthly — a
   detail easy to get backwards in a reimplementation that assumes all "billing frequency" gates
   behave the same way across fee areas.
10. Errors: none in this paragraph
11. Confidence: CONFIRMED

---

## What was scoped out of this pass

- **`7227-CHK-FOR-OVERRIDES`'s internals** (six paragraphs: `7325`/`7328`/`7330`/`7332`/`7335`/
  `7337`, a medichoice-private-label -> product-category -> vendor-level override cascade) —
  called from at least six sites across JIT and LUOM/break-bulk; its own logic was never read.
  Underlying tables (likely `CUG55`/`56`/`57`/`73`/`74`/`75`) not supplied.
- **`CUS120`/`CUP120`'s actual derivation logic** for every eligibility/classification switch this
  document traces back to them (LUOM eligibility, LUOM/break-bulk account-vs-group source,
  various JIT charge amounts) — confirmed as the ultimate source repeatedly, never itself
  examined. This is the single most-referenced blocker across the entire fees-and-adjustments
  area.
- **0215-SURCHARGE-DTLS-010's branches 5 through 16** (CID/division/corp levels) and
  **0250-PRO-BG-SURCHG-010's full internals** — priority order and formula shape confirmed by
  mechanical enumeration and first-four-branch sampling respectively, not individually
  transcribed line-by-line (see R-SURCHARGE-001's methodology note).
- **`0172-PROCESS-FOR-DEFUALT-SHIP`** (PANDAC default-ship-to fallback) — dispatch order
  confirmed (R-PANDAC-001), internal formula not read.
- **`ING01`, `VNG20`'s field-level detail** (product/inventory and division-vendor freight,
  R-FREIGHT-005) and **`9680`/`9685`-SQL-SELECT** (vendor-as-of-date freight variants,
  R-FREIGHT-003) — existence and trigger conditions confirmed from calling code; internal
  WHERE-clause detail deferred to the T005 SQL inventory CSV rather than re-transcribed.

## Assumptions

1. `WS-VND-JIT-*-FEE` and similar working-storage fields are assumed correctly re-initialized to
   zero before each fee area's paragraph runs per line item (standard COBOL working-storage
   reuse across a processing loop); not independently re-verified anywhere in this pass.
2. `OMGPR-C-PRIVATE-LBL = 'O'` is read as "Owens-sourced" by analogy to `A6O016U`'s
   `'O'`=Owens-kit/`'S'`=supplier-kit product-type convention (program-inventory.md §1.6) — an
   inference carried over from the prior JIT extraction, not confirmed by any comment local to
   this rule area.
3. The prior JIT/LUOM extraction's assumption that `WS-MAX-ALTER-UOM-ENTRIES`'s configured limit
   was not independently read still holds; only that exceeding it is a fatal error (R-LUOM-002).

## Blockers

1. `7227-CHK-FOR-OVERRIDES` and its six sub-paragraphs — needed to know the actual vendor
   fee/percentage value several formulas in this document (JIT categories, LUOM/break-bulk)
   ultimately consume, in any case where an override exists.
2. `CUS120`/`CUP120` — the standing, most-referenced blocker in this document; every eligibility
   switch this pass traced back to "CUP100 populates it from CUR120, ultimately from CUS120" is a
   confirmed pass-through, not a confirmed business rule.
3. `OMGPR-C-INFRT-TYPE`'s letter-code domain (R-FREIGHT-006 item 7) — no supplied source defines
   what each single-letter freight-type code means.
4. Ten-plus `VNGnn`/`BGGnn`/`ING01`/`CUS120` DCLGENs are not supplied in `upload/` (full list in
   this document's header) — every field sourced from them is individually marked BLOCKED or
   INFERRED at point of use rather than assumed.
5. No DB2/CICS/COBOL execution environment — standing blocker across every characterization/
   decision-table document produced for this repository; nothing in this document was validated
   by actually running the pricer.
6. The COBOL scope-terminator ambiguity flagged in R-FREIGHT-001 item 11 (a single `END-IF`
   apparently covering two nested `IF` levels in `9650-PRO-ACT-VEND-FREIGHT-CHG`) should be
   verified against an actual compile listing/cross-reference before an implementer relies on
   either possible interpretation of which branch the "closest expiration date" capture belongs
   to.


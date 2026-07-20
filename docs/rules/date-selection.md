# A6U01 — Date Selection Rules (T010)

**Scope:** Boundary behavior for effective/expiration date windows, null-date handling, and
closest-expiration-date resolution — the cross-cutting date mechanism referenced by nearly every
rule in `docs/rules/fees-and-adjustments.md` (T009) but not itself examined there.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL`. Central mechanism:
`7695-ADD-EXP-DATE-ARRA-010` (lines 17128-17147, accumulator) and
`7720-PRO-EXPIRE-DATES-010` (lines 17410-17469, resolver), both called from
`0050-PROCESS-EXP-DATE` (lines 2621-2633), which itself runs at the end of every completed pricing
mode except JIT-ON-COST (see R-DATE-000 item 9). Individual fee areas' WHERE-clause date filters
were already cited per-rule in `fees-and-adjustments.md`; this document does not re-list every
site, only the shared mechanism and the shared boundary convention.

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## R-DATE-001 Null-date indicator convention (DB2 `-NI` fields)

1. ID/Name: R-DATE-001 Null-indicator (`-NI`) convention for optional/open-ended dates
2. Program/paragraph: pervasive across `A6U01.CBL` — every fee-area SQL SELECT with an optional
   expiration column pairs it with a `:HOST-VAR-NI` indicator variable (e.g.
   `BGG10-D-INFRT-BG-EXP-NI`, `CUG29-D-PANDAC-ADJ-EXP-NI`, `CCG03-D-CNT-LINE-EXPIRE-NI`,
   `VNG31-D-AC-VN-FREIGHT-EX-NI` — dozens of confirmed occurrences across the fee areas already
   documented in `fees-and-adjustments.md`)
3. Preconditions: applies to every DB2-sourced date field that is nullable in its source table
4. Data deps: standard embedded-SQL DB2 null-indicator host variables — this is a DB2/COBOL
   platform convention, not a business rule specific to this codebase
5. Priority: this is the universal gate checked before any nullable date is used or added to the
   closest-expiration array (R-DATE-003) — CONFIRMED, every site follows the identical shape
   `IF <FIELD>-NI NOT = -1`
6. Calculation: `-1` means the corresponding column was `NULL` in the fetched row; any other value
   (typically `0`) means the date field itself holds a real, usable value. This is the standard
   DB2 embedded-SQL indicator-variable contract, not a value this codebase invented.
7. Output: gates whether the paired date field is trusted/used at all
8. Dates: this rule IS the date-nullability mechanism
9. Exclusions/fallbacks: a `NULL` (indicator `-1`) expiration date is treated as "open-ended, no
   expiration from this source" — CONFIRMED by the consistent SQL WHERE-clause pattern
   `(D_XXX_EX >= :WS-CURRENT-DB2-DATE OR D_XXX_EX IS NULL)` seen across every fee area's row
   lookup in `fees-and-adjustments.md` — a null expiration matches regardless of the current date,
   and (per R-DATE-002) never itself contributes a candidate to the closest-expiration array
10. Errors: none — this is a data-shape check, not an error condition
11. Confidence: CONFIRMED (this is also a standard DB2 embedded-SQL platform behavior, not
    something inferred from ambiguous evidence)

## R-DATE-002 Effective/expiration window boundary behavior (inclusive both ends)

1. ID/Name: R-DATE-002 Date-range row-selection boundary convention
2. Program/paragraph: every date-scoped SELECT across the fee areas documented in
   `fees-and-adjustments.md` (independently confirmed at, among others: R-FREIGHT-001/002 (VNG31/
   VN_CID_VN_FREIGHT), R-PANDAC-003's CUTADR lookup, and the general shape of every `VNGnn`/
   `CUGnn`/`BGGnn` fee-row SELECT read this pass)
3. Preconditions: none — this is the default row-selection shape used throughout, not conditional
   on anything else
4. Data deps: `WS-CURRENT-DB2-DATE` (the pricing date used as "today" for all effective-dating
   comparisons — set once per pricer invocation, see R-DATE-000 item 4 for its own derivation)
5. Priority: N/A — this is a per-query filter, not a competing rule
6. Calculation: the confirmed, repeated shape is
   `WHERE ... D_XXX_EF <= :WS-CURRENT-DB2-DATE AND (D_XXX_EX >= :WS-CURRENT-DB2-DATE OR
   D_XXX_EX IS NULL)` — i.e. **both the effective-date and expiration-date bounds are inclusive**
   (`<=`/`>=`, not `<`/`>`): a row effective exactly today, or expiring exactly today, still
   matches. A null expiration date means the row never expires by this filter (R-DATE-001).
7. Output: determines which single row (or first-matching row, depending on `ORDER BY`/cursor
   semantics not re-examined per-site here) a given lookup returns
8. Dates: `_EF` (effective) and `_EX` (expiration) column-naming convention, consistent across
   every table cited in `fees-and-adjustments.md`
9. Exclusions/fallbacks: a row whose effective date is in the future, or whose (non-null)
   expiration date is in the past, is excluded from the result set entirely — it simply does not
   match the WHERE clause, there is no separate "expired row" handling path
10. Errors: none in this rule itself (SQLCODE handling for the surrounding SELECT is documented
    per fee area in `fees-and-adjustments.md`)
11. Confidence: CONFIRMED for every site independently re-examined in T009; INFERRED (by strong,
    consistent pattern) that this convention holds for the handful of date-scoped lookups in
    `fees-and-adjustments.md` whose exact WHERE clause was deferred to the T005 SQL CSV rather
    than individually re-transcribed (e.g. some of surcharge's 16-branch cascade)

## R-DATE-003 Closest-expiration-date accumulation

1. ID/Name: R-DATE-003 Closest-expiration-date array accumulator
2. Program/paragraph: `7695-ADD-EXP-DATE-ARRA-010`, lines 17128-17147
3. Preconditions: called once per candidate date, from dozens of sites across every fee area
   (freight, rebates, PANDAC, delivery, JIT/LUOM, SurgiTrak, and more — already cited individually
   in `fees-and-adjustments.md`), each site already having passed its own `R-DATE-001` null check
   before calling this paragraph
4. Data deps: `WS-D-CHECK` (`OCCURS 50 TIMES`, `PIC X(10)` per entry, declared lines 1214-1216),
   `WS-Q-CHECK` (`PIC 9(08) COMP`, the current count/next-index, declared line 1209),
   `WS-Q-CHECK-MAX` (`PIC 9(03) COMP-3 VALUE 50`, declared line 1213 — the array is fixed at 50
   slots), `WS-PRICE-EXP-DATE` (the single staging field each call site populates before invoking
   this paragraph)
5. Priority: purely additive — every call appends one more candidate date to the array; there is
   no priority among candidates at this stage (R-DATE-004 resolves priority by taking the
   earliest)
6. Calculation: `ADD 1 TO WS-Q-CHECK`; if the new count exceeds `WS-Q-CHECK-MAX` (50), set a fatal
   error (see item 10) and return immediately without storing the date; otherwise
   `MOVE WS-PRICE-EXP-DATE TO WS-D-CHECK(WS-Q-CHECK)`.
7. Output: `WS-D-CHECK` array grows by one entry (or the array-overflow error fires)
8. Dates: this rule IS the date-collection mechanism; it stores whatever `WS-PRICE-EXP-DATE` held
   at call time, with no format validation of its own (format is whatever the caller supplied —
   see R-DATE-004 for how the resolver interprets it)
9. Exclusions/fallbacks: none — every call unconditionally appends (the null-check gating is done
   entirely by the caller before invoking this paragraph, per R-DATE-001, not inside it)
10. Errors: **CONFIRMED, and notably a soft/local failure, not a pricer abend** — exceeding
    `WS-Q-CHECK-MAX` (50 candidate dates in a single pricing pass) sets `OMGPR-Q-ERROR-NBR = 145`,
    `OMGPR-ERROR-MESSAGE = "ERR#145 NO OF EXPIRATION DATES EXCEEDS LIMIT"`, and
    `OMGPR-F-PRICER-ERROR = WS-YES-IND`, then simply returns via `GO TO 7695-EXIT` — **it does
    NOT `GO TO 0020-EXIT-PRICER`**, unlike almost every other fatal-error path in this codebase
    (which explicitly abend the pricer immediately). This means a 51st candidate date is silently
    dropped and the error flag is left for *some* later check to notice — at least one call site
    (`0205-PRO-FREIGHT-COST-010`, line 4618-4620: `IF OMGPR-PRICER-ERROR GO TO 0020-EXIT-PRICER`)
    does check and escalate, but this task did not verify that every one of the dozens of call
    sites performs an equivalent check immediately afterward. **Flagged as a potential silent-
    failure gap**: if a call site that does NOT immediately check `OMGPR-PRICER-ERROR` triggers
    the 51-candidate overflow, the error flag could persist un-escalated for the rest of the
    pricing pass, with the practical effect being simply "one expiration date candidate is
    missing from the closest-expiration calculation" rather than a hard failure.
11. Confidence: CONFIRMED for the accumulator's own logic; INFERRED/not-exhaustively-verified
    that every calling site properly escalates the overflow error (only one site's escalation
    behavior was directly confirmed)

## R-DATE-004 Closest-expiration-date resolution

1. ID/Name: R-DATE-004 Earliest-date selection from the candidate array
2. Program/paragraph: `7720-PRO-EXPIRE-DATES-010`, lines 17410-17469; called from
   `0050-PROCESS-EXP-DATE` (2621-2633) immediately after rounding (R-ROUND-001)
3. Preconditions: runs once per completed pricing pass (see R-DATE-000 item 9 for which modes
   reach it)
4. Data deps: `WS-D-CHECK`/`WS-Q-CHECK` (populated by R-DATE-003 across the whole pricing pass),
   `WS-ALL-NINES` (`PIC X(10) VALUE '9999999999'`, the sentinel "no date found yet" value),
   `WS-D-CHECK-HOLD`/`WS-D-CHECK-MOVE`/`WS-D-CHECK-COMPARE`/`WS-D-CHECK-HOLD-COMPARE` (working
   fields for the reformat-and-compare loop)
5. Priority: **earliest (soonest) date among all candidates wins** — this is a linear scan, not a
   sort; CONFIRMED, `PERFORM UNTIL` loop over all `WS-Q-CHECK` entries, keeping a running
   `WS-D-CHECK-HOLD` minimum
6. Calculation: `WS-Q-CHECK = 0` (no candidates were ever added — every date was null across the
   whole pricing pass) is checked FIRST and short-circuits the entire resolution
   (`OMGPR-D-EXPIRATION = SPACES`, `GO TO 7720-EXIT`) — see item 9. Otherwise: initialize
   `WS-D-CHECK-HOLD` to the sentinel `WS-ALL-NINES` (a date so far in the future it will lose
   every real comparison), then for each of the `WS-Q-CHECK` entries (bounded additionally by
   `WS-Q-CHECK-MAX` as a defensive second bound): skip blank entries
   (`WS-D-CHECK(WS-SUB1) > SPACES` guard) and skip an entry that happens to already equal the
   current hold value (an optimization/dedup, not a correctness requirement); otherwise
   reformat both the candidate and the current hold value from their stored 10-character form
   into an 8-character `YYYYMMDD`-shaped comparable string via the **same substring-reassembly
   pattern already documented in `fees-and-adjustments.md`'s `R-REBATE-004`**
   (`MOVE-MOVE(7:4)->COMPARE(1:4)` [year], `MOVE(1:2)->COMPARE(5:2)` [month],
   `MOVE(4:2)->COMPARE(7:2)` [day] — i.e. the stored 10-character date is read as
   `MM?DD?YYYY` with 1-character separators at positions 3 and 6, the same INFERRED external
   layout noted in R-REBATE-004), then keep whichever of the two is chronologically earlier as
   the new `WS-D-CHECK-HOLD`. After the loop: if `WS-D-CHECK-HOLD` is still the sentinel
   (`WS-ALL-NINES`, meaning every candidate was blank/skipped), leave `OMGPR-D-EXPIRATION`
   untouched (`CONTINUE`); otherwise `MOVE WS-D-CHECK-HOLD TO OMGPR-D-EXPIRATION`.
7. Output: `OMGPR-D-EXPIRATION` — the single closest-expiration date surfaced to the calling
   system for the entire priced line, drawn from whichever fee/cost/sell source's date candidate
   was earliest among everything that contributed during this pricing pass
8. Dates: this rule IS the date-resolution mechanism; see item 6 for the exact reformat/compare
   logic (identical in shape to R-REBATE-004's price-protection date compare, confirming this is
   a shared codebase-wide idiom for 10-character-date chronological comparison, not a
   one-off)
9. Exclusions/fallbacks: **two distinct "no expiration" outcomes, confirmed as different paths**:
   (a) `WS-Q-CHECK = 0` (nothing was ever added — e.g. every date source that ran for this line
   had a null expiration) explicitly sets `OMGPR-D-EXPIRATION = SPACES`; (b) candidates were
   added but all were blank/skipped (`WS-D-CHECK-HOLD` still the sentinel after the loop) leaves
   `OMGPR-D-EXPIRATION` at whatever it held before this paragraph ran — **these are NOT
   equivalent**: path (a) explicitly blanks the field, path (b) does not touch it at all,
   meaning a stale value from a prior COMMAREA use (if the caller doesn't clear it) could
   theoretically survive path (b). Whether `OMGPR-D-EXPIRATION` is reliably blank on entry to
   each pricing call was not independently verified (BLOCKED — depends on caller/COMMAREA
   lifecycle, outside `A6U01.CBL`).
10. Errors: none in this paragraph itself (the only error condition tied to this mechanism is
    the array-overflow in R-DATE-003)
11. Confidence: CONFIRMED for the resolution algorithm. INFERRED for the exact external meaning
    of the 10-character stored date layout (same caveat as R-REBATE-004, no copybook comment
    states the literal format). BLOCKED for whether `OMGPR-D-EXPIRATION` is guaranteed blank on
    entry (path (b)'s edge case, item 9).

## R-DATE-000 Pricing-date derivation and pricing-mode coverage

1. ID/Name: R-DATE-000 `WS-CURRENT-DB2-DATE` derivation and which pricing modes run date
   resolution
2. Program/paragraph: `0020-PRICE-A-LINE` dispatcher (lines 2457-2498, already documented as the
   pricing-mode `EVALUATE TRUE` in `fees-and-adjustments.md`'s cross-references), specifically
   lines 2465-2469
3. Preconditions: none — runs once per pricer invocation, before any fee/cost/sell processing
4. Data deps: `OMGPR-D-PRICING` (the caller-supplied pricing-as-of date, a COMMAREA input field)
5. Priority: N/A
6. Calculation: `IF OMGPR-JIT-ON-COST: WS-CURRENT-DB2-DATE = OMGPR-D-PRICING` directly. `ELSE:
   PERFORM 7185-PRO-PASSED-DATA-010` (not re-examined this pass — presumably does the same
   assignment plus additional setup, deferred) — CONFIRMED there are two different code paths to
   derive the same working date depending on pricing mode, not independently verified whether
   they produce identical results for the same input (INFERRED they do, by the shared variable
   name and evident purpose).
7. Output: `WS-CURRENT-DB2-DATE`, the date every fee-area SQL lookup in `fees-and-adjustments.md`
   filters against (R-DATE-002)
8. Dates: `OMGPR-D-PRICING` is the ultimate source for the entire pricing pass's "as of" date
9. Exclusions/fallbacks: **CONFIRMED, R-DATE-003/R-DATE-004 (the closest-expiration mechanism)
   and R-ROUND-001 (final rounding) are reached by every pricing mode except JIT-ON-COST** — per
   the top-level dispatcher's `EVALUATE TRUE` (already cited in `fees-and-adjustments.md`'s JIT
   section): `WHEN OMGPR-JIT-ON-COST -> PERFORM 7225-PRO-JIT-ADJ-010` directly, bypassing
   `0025-PROCESS-FOR-PRICE` (and therefore `0050-PROCESS-EXP-DATE`) entirely. A JIT-ON-COST
   pricing call does not populate `OMGPR-D-EXPIRATION` or apply the account-level rounding mode
   documented in `docs/rules/rounding.md` — this is a genuine behavioral difference for that
   pricing mode, not an oversight in this document.
10. Errors: none in this fragment
11. Confidence: CONFIRMED for the mode-coverage finding (directly read from the dispatcher);
    INFERRED that `7185-PRO-PASSED-DATA-010` produces an equivalent `WS-CURRENT-DB2-DATE` to the
    JIT-ON-COST direct-assignment branch (not independently re-verified, deferred as out of
    scope for this pass)

---

## What was scoped out of this pass

- **`7185-PRO-PASSED-DATA-010`** — the non-JIT-ON-COST date-setup paragraph; assumed to produce
  an equivalent `WS-CURRENT-DB2-DATE` by name and evident purpose, not independently read.
- **Per-fee-area WHERE-clause date-column names** beyond what was already cited in
  `fees-and-adjustments.md` — this document states the *shared convention*
  (`R-DATE-002`), not a re-enumeration of every table's specific `_EF`/`_EX` column pair.
- **`OMGPR-D-EXPIRATION`'s downstream consumption** outside `A6U01.CBL` (e.g. how the calling
  CICS transaction or order-entry system displays/uses this date) — out of scope for a COBOL-only
  analysis pass.

## Assumptions

1. The 10-character stored date format's external layout (`MM?DD?YYYY` with single-character
   separators at positions 3 and 6, per the substring-reassembly logic in R-DATE-004) is inferred
   from the reformatting code, not confirmed by any copybook comment — carried over from the same
   caveat already noted in `fees-and-adjustments.md`'s `R-REBATE-004`.
2. `WS-CURRENT-DB2-DATE`'s two derivation paths (JIT-ON-COST direct assignment vs.
   `7185-PRO-PASSED-DATA-010`) are assumed to be behaviorally equivalent for the same
   `OMGPR-D-PRICING` input.

## Blockers

1. `7185-PRO-PASSED-DATA-010`'s internals — needed to fully confirm R-DATE-000 item 6's
   equivalence assumption.
2. Whether every one of the dozens of `7695-ADD-EXP-DATE-ARRA-010` call sites across
   `fees-and-adjustments.md` checks `OMGPR-PRICER-ERROR` immediately afterward (R-DATE-003 item
   10) — only one site's escalation behavior was directly confirmed in this pass.
3. `OMGPR-D-EXPIRATION`'s COMMAREA lifecycle/initial-blank guarantee (R-DATE-004 item 9) — outside
   `A6U01.CBL`'s own source.
4. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set.

# A6O012U — Kit/Pack Explosion Processing (T004)

**Scope:** Ordered decision-table extraction for `A6O012U` (the online kit-explosion
orchestrator, 525 lines, read in full), covering component capacity, quantity, UOM, level,
sub-pack, rollup, errors, and expiration per T004's acceptance criteria.

**What this program is:** per its own header comment, `A6O012U` is "ONLINE COMPONENT TO PROCESS
THE REQUEST FOR EXPLOSION" — it validates an incoming request to explode a kit/pack product into
its components, delegates the actual explosion to `A6O015U` via `EXEC CICS LINK`, then
re-shapes `A6O015U`'s raw result (`OMGPK`) into the caller-facing `OMGEXPL` structure according to
which of three "views" was requested. **`A6O012U` does not itself explode anything** — it is a
thin validate/dispatch/reshape layer around a black-box engine.

**Source of truth:** COBOL as read directly in `upload/A6O012U.CBL` (full program) and
`upload/OMGEXPL.CPY` (full copybook). `OMGPK`'s own copybook and `A6O015U`'s own source are not
supplied — every conclusion about what happens *inside* the actual explosion (cost rollup,
how components are originally identified, database/table access) is BLOCKED, not inferred as
fact, per this repository's standing instruction not to invent missing called-program behavior.

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Control-flow map (CONFIRMED)

```
PROCEDURE DIVISION
  -> 0010-INIT            (capture current date via SQL, initialize output areas)
  -> 0015-VALIDATE         (R-KIT-001: input validation, may set OMGEXPL-ABEND/-ERROR)
  [IF NOT abend/error]
  -> 0020-PROCESS
       -> 1000-MOVE-INP-OMGPK      (copy validated input into the OMGPK parameter list)
       -> 2000-LINK-TO-EXPLODER    (EXEC CICS LINK to A6O015U -- R-KIT-005)
       [IF link succeeded]
       -> 3000-MOVE-TO-OMGEXPL
            -> 3100-MOVE-HDR-DATA          (pack-level attributes, straight pass-through)
            -> PERFORM VARYING SUB ... (loop over OMGPK-COMP-INFO, up to 700 or
                                         OMGPK-NUMBER-OF-ITEMS, whichever is smaller)
                 -> 3050-HANDLE-ASPER-REQUEST (dispatch by request type -- R-KIT-002)
                      -> 3200-PROCESS-STRUCTURE | 3300-PROCESS-LEVEL | 3400-PROCESS-COMPONENT
                           -> 4000-FIND-PARENT-PROD (level-2 only -- R-KIT-003, quantity rollup)
  -> 0030-EOJ              (move OMGEXPL-C to DFHCOMMAREA, EXEC CICS RETURN)
```

---

## R-KIT-001 Input validation
1. ID/Name: R-KIT-001 Explosion-request input validation
2. Program/paragraph: `0015-VALIDATE` (172-245) plus `0018-VALIDATE-DATE` (247-251, a thin
   wrapper around the shared `SYH208`/`SYU208` date-validation utility copybook)
3. Preconditions: runs once per call, after `0010-INIT`
4. Data deps: none beyond the caller-supplied `OMGEXPL` input fields themselves; date validation
   delegates to `SYH208`/`SYU208`, a shared utility not specific to kit processing (not
   independently re-examined this pass — treated as a black-box "is this a valid date" check)
5. Priority: four sequential checks, each immediately short-circuiting to `0015-EXIT` if it sets
   `OMGEXPL-ABEND`/`OMGEXPL-ERROR` (the `IF OMGEXPL-ABEND OR OMGEXPL-ERROR GO TO 0015-EXIT`
   pattern repeated after each check, lines 173, 183, 201, 240) — first failing check wins, later
   checks never run:
   1. `OMGEXPL-PACK-PRODNO = SPACES` -> error 61201 (product# not provided)
   2. `OMGEXPL-INP-EFF-DATE` supplied but fails `SYH208`/`SYU208` validation -> error 61202
      (effective date invalid). If NOT supplied (blank), defaults silently to `WS-CURRENT-DATE`
      (today, captured in `0010-INIT` via `SELECT CURRENT_TIMESTAMP`) — not an error.
   3. `OMGEXPL-ROLLUP-COST-SW` not one of `LOW-VALUES`/`'Y'`/`'N'`/`' '` -> error 61204 (rollup
      fees switch invalid)
   4. `OMGEXPL-REQUEST-TYPE` not one of `'S'`/`'L'`/`'C'` -> if blank, silently defaults to `'C'`
      (component-only, the documented default); any other non-blank value -> error 61203
      (request type invalid)
6. Calculation: none — pure validation
7. Output: `OMGEXPL-ERROR-RESPONSE-FLAG`/`-ERROR-NUMBER`/`-ERROR-MSG` on failure;
   `OMGEXPL-INP-EFF-DATE`/`OMGEXPL-REQUEST-TYPE` defaulted in place on the two silent-default
   paths
8. Dates: `OMGEXPL-INP-EFF-DATE` is the only date validated at this stage; there is no
   closest-expiration-array mechanism anywhere in this program (contrast with `A6U01`'s
   elaborate mechanism in `docs/rules/date-selection.md`) — the OH/TP-fee expiration dates in the
   output are pure pass-throughs from `OMGPK`, never compared against each other or against the
   pricing date by `A6O012U` itself (see R-KIT-006)
9. Exclusions/fallbacks: two of the four checks have silent defaults rather than errors (blank
   effective date -> today; blank request type -> `'C'`) — only genuinely invalid non-blank
   values are treated as errors. This is a different convention from `A6U01`'s validation
   errors (`docs/cobol-analysis/error-catalog.md` §2.1), which treat blank required fields as
   errors outright — here, blank is a valid "use the default" signal for two of the four fields
   (not for `PACK-PRODNO`, which has no default and is always required).
10. Errors: 61201, 61202, 61203, 61204 — all set `OMGEXPL-ERROR-RESPONSE-FLAG` to the
    non-abend `'E'` (`WS-NONABEND-ERROR`), not `'A'` (abend) — see R-KIT-007 for what this
    severity distinction does downstream (as far as this program's own logic shows: nothing
    different — both `OMGEXPL-ABEND` and `OMGEXPL-ERROR` cause `0020-PROCESS` to be skipped
    identically at line 136-142; the distinction may matter only to callers outside this
    program, which is BLOCKED knowledge).
11. Confidence: CONFIRMED

## R-KIT-002 Request-type dispatch: three different views of the same exploded pack
1. ID/Name: R-KIT-002 Structure vs. Level-1-only vs. Components-only response shaping
2. Program/paragraph: `3050-HANDLE-ASPER-REQUEST` (319-334) dispatching to
   `3200-PROCESS-STRUCTURE` (355-407), `3300-PROCESS-LEVEL` (409-446), or
   `3400-PROCESS-COMPONENT` (448-495)
3. Preconditions: `OMGEXPL-REQUEST-TYPE` already validated/defaulted by R-KIT-001; this dispatch
   runs once per `OMGPK-COMP-INFO` row returned by `A6O015U` (i.e. once per raw exploded item),
   not once per call
4. Data deps: `OMGPK-COMP-PROD-TYPE(SUB)`, a three-value domain confirmed by usage:
   `'S'` (sub-pack), `'1'` (component at level 1), `'2'` (component at level 2) — see
   `omgexpl-data-dictionary.md`'s `OMGPK` section for how this differs from `OMGEXPL`'s own
   two-value domain
5. Priority: mutually exclusive by `OMGEXPL-REQUEST-TYPE`'s own value (set once per call, not
   per row), so exactly one of the three paragraphs runs for every row in a given call:

   | Request type | `'S'` row (sub-pack) | `'1'` row (level-1 component) | `'2'` row (level-2 component) |
   |---|---|---|---|
   | **`'S'` Structure** | Tracked in `WS-SUB-PACK-ARRAY` for later level-2 lookups; **not written to `OMGEXPL-COMP-INFO`** | Written as `OMGEXPL-COMP-PROD-TYPE='C'`, `OMGEXPL-LEVEL=1`, `OMGEXPL-SUB-PACK-NUM` = the top-level pack's own product number | Written as `'C'`, `OMGEXPL-LEVEL=2`, quantity rolled up (R-KIT-003), `OMGEXPL-SUB-PACK-NUM` = the resolved PARENT sub-pack's product number (via 4000, R-KIT-003) — only if the parent is found |
   | **`'L'` Level-1-only** | Written as `OMGEXPL-COMP-PROD-TYPE='S'` directly (unlike Structure mode, sub-packs ARE reported here), `OMGEXPL-SUB-PACK-NUM` = top-level pack's product number | Same as Structure mode's `'1'` handling | **Explicitly skipped** ("NOT INTERESTED" — `CONTINUE`, no row written at all) |
   | **`'C'` Components-only (default)** | Tracked in `WS-SUB-PACK-ARRAY` only (same as Structure mode) — **not written** | Same as Structure/Level modes | Written as `'C'`, `OMGEXPL-LEVEL=2`, quantity rolled up (R-KIT-003), but `OMGEXPL-SUB-PACK-NUM` is **explicitly left blank** ("SINCE COMP ONLY, WE ARE NOT INTERESTED IN KNOWING PARENT#", line 477-478) — only if the parent is found |

   **CONFIRMED, worth stating plainly:** the three request types are not "the same data at
   different granularity" — they produce genuinely different row sets and different field
   population for the *same* underlying level-2 component (present with a populated parent
   reference in Structure mode, present but with a BLANKED parent reference in Components-only
   mode, absent entirely in Level-1-only mode).
6. Calculation: see R-KIT-003 for level-2 quantity
7. Output: `OMGEXPL-COMP-INFO` array entries, `OMGEXPL-NUMBER-OF-ITEMS` (final count, via
   `SUB-OMEXPL`)
8. Dates: none in this dispatch
9. Exclusions/fallbacks: `OMGPK-COMP-PROD-TYPE(SUB)` values outside `'S'`/`'1'`/`'2'` are a
   silent no-op in all three paragraphs (`WHEN OTHER -> CONTINUE`, with an in-line comment
   "THIS IS AN ERROR" that is **not actually enforced as an error** — the row is simply skipped,
   no `OMGEXPL-ERROR-*` field is set) — a genuine gap between the comment's stated intent and
   the code's actual behavior, flagged rather than silently corrected.
10. Errors: none actively raised by this dispatch (see item 9)
11. Confidence: CONFIRMED

## R-KIT-003 Level-2 (nested) component quantity rollup via parent sub-pack lookup
1. ID/Name: R-KIT-003 Nested-component quantity multiplication through its parent sub-pack
2. Program/paragraph: `4000-FIND-PARENT-PROD` (497-518), called from within
   `3200-PROCESS-STRUCTURE` and `3400-PROCESS-COMPONENT`'s own `WHEN '2'` branches (not called
   from `3300-PROCESS-LEVEL`, which skips level-2 rows entirely per R-KIT-002)
3. Preconditions: the current row's `OMGPK-COMP-PROD-TYPE(SUB) = '2'` (a level-2/nested
   component), and at least one `'S'` (sub-pack) row must have already been encountered and
   recorded in `WS-SUB-PACK-ARRAY` earlier in the same loop — **CONFIRMED, this depends on
   `A6O015U` returning sub-pack rows before the level-2 components that belong to them** in its
   raw `OMGPK-COMP-INFO` array ordering; this ordering assumption is not independently verified
   (BLOCKED, `A6O015U` not supplied) but the calling code's structure only works correctly if it
   holds.
4. Data deps: `WS-SUB-PACK-ARRAY` (`OCCURS 300 TIMES`, populated when `'S'` rows are encountered:
   `WS-SUB-PACK-PROD-NO`, `WS-SUB-PACK-SL-NO` — a slot/sequence number, NOT a product number —
   and, per the `NK1209` fix, `WS-SUB-PACK-QTY`)
5. Priority: linear search through `WS-SUB-PACK-ARRAY` (bounded at `WS-MAX-SUB-PACKS`=300),
   matching `OMGPK-SUB-PACK-NUM(SUB)` (the level-2 row's own claimed parent-slot reference)
   against each recorded `WS-SUB-PACK-SL-NO(SUB-SEARCH)` — **CONFIRMED, this is a slot-number
   match, not a product-number match**, despite the field being named `SUB-PACK-NUM`. First
   match wins; search also stops early if a blank `WS-SUB-PACK-PROD-NO` slot is encountered
   (`WS-NO-MORE-SEARCH-SW`, treated as "end of populated entries").
6. Calculation: **CONFIRMED, the one place in this program where real arithmetic happens**
   (change-ID `NK1209`, with an explicit incident reference in the header comment: "INCIDENT #
   1667882 - PACK WITH 4 SUBPACKS W/ THE SAME PRODUCT # ONLY BEING COSTING 1 TIME INSTEAD OF 4"):
   `OMGEXPL-COMP-QTY(SUB-OMEXPL) = OMGPK-COMP-QTY(SUB) * WS-PARENT-OF-COMP-QTY` — the level-2
   component's own quantity (its quantity within ONE copy of its parent sub-pack) is multiplied
   by the parent sub-pack's own quantity within the top-level pack. **This is a genuine bug-fix
   history worth preserving exactly**: before this fix, a pack containing the same sub-pack
   product multiple times (e.g. 4 identical sub-packs) would only have its nested components
   costed once instead of 4 times, because the multiplication by `WS-PARENT-OF-COMP-QTY` was
   missing. A reimplementation that naively takes "component quantity" as a flat pass-through
   would reintroduce this exact historical bug.
7. Output: `WS-PARENT-OF-COMP` (parent sub-pack's product number, used for `OMGEXPL-SUB-PACK-NUM`
   in Structure mode only, per R-KIT-002), `WS-PARENT-OF-COMP-QTY` (used in the multiplication
   above)
8. Dates: none
9. Exclusions/fallbacks: **CONFIRMED — if no matching parent slot is found
   (`WS-SUB-PACK-FOUND` stays `'N'`), the level-2 component row is silently dropped entirely**
   (the `ADD 1 TO SUB-OMEXPL` and subsequent `MOVE`s are all inside `IF WS-SUB-PACK-FOUND`, both
   in `3200-PROCESS-STRUCTURE` and `3400-PROCESS-COMPONENT`) — no error is raised, the component
   simply does not appear in the output. This is a second silent-drop path (alongside R-KIT-002
   item 9's unrecognized-type case) where a data-shape anomaly from `A6O015U` produces a
   quietly incomplete result rather than a diagnosable error.
10. Errors: none
11. Confidence: CONFIRMED for the mechanism and the bug-fix history (the change-ID comment
    directly explains the fix's purpose). INFERRED that `A6O015U` always returns sub-pack rows
    before their child level-2 components in array order (required for this lookup to work, not
    independently verified since `A6O015U` is not supplied).

## Capacity (component/sub-pack array limits, no overflow error)
1. ID/Name: R-KIT-004 Array capacity limits with silent truncation, not an overflow error
2. Program/paragraph: `3000-MOVE-TO-OMGEXPL`'s `PERFORM VARYING` bound (307-309) and
   `4000-FIND-PARENT-PROD`'s search bound (500-503)
3. Preconditions: applies to every call
4. Data deps: `WS-MAX-ITEMS-IN-PACK` (`PIC 999 VALUE 700`, matching `OMGEXPL-COMP-INFO`'s own
   `OCCURS 700`) and `WS-MAX-SUB-PACKS` (`PIC 999 VALUE 300`, matching `WS-SUB-PACK-ARRAY`'s
   `OCCURS 300`)
5. Priority: N/A
6. Calculation: N/A
7. Output: N/A
8. Dates: N/A
9. Exclusions/fallbacks: **CONFIRMED, and a genuine finding worth flagging prominently since it
   contrasts with `A6U01`'s convention** (`docs/cobol-analysis/error-catalog.md` §4 documents
   `A6U01` raising explicit "exceeded max limit" errors, albeit inconsistently, for its own
   array bounds) — **`A6O012U` has no equivalent error for either limit.** The main processing
   loop's `PERFORM VARYING SUB ... UNTIL SUB >= WS-MAX-ITEMS-IN-PACK OR SUB > OMGPK-NUMBER-OF-ITEMS
   OR ...` simply stops at 700 items if `OMGPK-NUMBER-OF-ITEMS` legitimately exceeds that —
   remaining components are silently never processed, with no error flag, no message, and
   `OMGEXPL-NUMBER-OF-ITEMS` reporting only the truncated count. The same is true of
   `4000-FIND-PARENT-PROD`'s 300-slot sub-pack search — a pack with more than 300 distinct
   sub-packs would silently fail to find parents for sub-packs beyond that limit (falling into
   R-KIT-003 item 9's silent-drop path). **This is a genuine risk for any sufficiently large
   kit**, not merely a theoretical edge case, since the limits (700 items, 300 sub-packs) are
   concrete and reachable for a complex enough pack structure.
10. Errors: none — this is the finding
11. Confidence: CONFIRMED

## R-KIT-005 CICS LINK failure to the explosion engine
1. ID/Name: R-KIT-005 A6O015U link failure handling
2. Program/paragraph: `2000-LINK-TO-EXPLODER` (292-299) and the `EVALUATE` in `0020-PROCESS`
   (262-274)
3. Preconditions: `0015-VALIDATE` passed (R-KIT-001)
4. Data deps: `WS-XCTL-CICS-RESP` (the CICS RESP code from the `LINK`)
5. Priority: three-way, matching the general pattern already documented in
   `program-inventory.md` §6.5 and `error-catalog.md` §6, with this program's own specific
   messages: `DFHRESP-NORMAL` -> proceed to `3000-MOVE-TO-OMGEXPL`; `DFHRESP-PGMIDERR` ->
   `'A6U15 TRANSACTION IS UNAVAILABLE'`; `OTHER` -> `'A6U15 LINK ERROR '`
6. Calculation: N/A
7. Output: `OMGEXPL-ERROR-MSG`, `OMGEXPL-ERROR-RESPONSE-FLAG` set to `WS-NONABEND-ERROR` (`'E'`)
   in both failure cases — **CONFIRMED, notably NOT `'A'` (abend)** despite a total inability to
   perform the requested explosion at all; the severity distinction between `'A'` and `'E'` in
   this program does not correlate with "how bad is this failure" in an obviously consistent way
   (a missing required product number, R-KIT-001, and a total link failure to the core engine
   both land on the same `'E'` classification).
8. Dates: none
9. Exclusions/fallbacks: no retry logic of any kind — a single failed `LINK` attempt is final for
   this call
10. Errors: no numeric `OMGEXPL-ERROR-NUMBER` is set for either LINK failure case (unlike
    R-KIT-001's validation errors, which all carry a specific 612xx number) — only the message
    text and response flag distinguish a link failure from any other error, which is a gap
    relative to this program's own error-catalog convention for validation errors.
11. Confidence: CONFIRMED

## R-KIT-006 Expiration dates: pure pass-through, no comparison logic
1. ID/Name: R-KIT-006 Overhead/third-party fee expiration dates
2. Program/paragraph: `3100-MOVE-HDR-DATA` (336-353)
3. Preconditions: runs once per call, before the component loop
4. Data deps: `OMGPK-OH-EFF-DATE`/`-OH-EXP-DATE`, `OMGPK-TP-COST-EFF-DATE`/`-EXP-DATE`,
   `OMGPK-TP-SELL-EFF-DATE`/`-EXP-DATE`
5. Priority: N/A
6. Calculation: none — straight `MOVE`s, six date fields copied from `OMGPK` to `OMGEXPL`
   unchanged
7. Output: `OMGEXPL-OH-EFF-DATE`/`-OH-EXP-DATE`/`-TP-COST-EFF-DATE`/`-TP-COST-EXP-DATE`/
   `-TP-SELL-EFF-DATE`/`-TP-SELL-EXP-DATE`
8. Dates: **CONFIRMED, worth stating explicitly as a contrast to `A6U01`'s behavior** — `A6O012U`
   has no closest-expiration-date resolution mechanism at all (nothing analogous to
   `docs/rules/date-selection.md`'s `R-DATE-003`/`R-DATE-004`). It simply relays whatever three
   pairs of effective/expiration dates `A6O015U` (or the underlying `OMGPK` data) supplied,
   unexamined and uncompared. Any "which of these six dates is the one that matters soonest"
   logic, if it exists at all, must live in `A6O015U` or in whatever program ultimately consumes
   `OMGEXPL`'s output — BLOCKED for both.
9. Exclusions/fallbacks: none — always runs
10. Errors: none
11. Confidence: CONFIRMED

## R-KIT-007 Rollup-cost switch: pass-through only, actual rollup arithmetic is BLOCKED
1. ID/Name: R-KIT-007 Cost-rollup switch is forwarded, not evaluated, by this program
2. Program/paragraph: `1000-MOVE-INP-OMGPK` (285-290)
3. Preconditions: `OMGEXPL-ROLLUP-COST-SW` already validated by R-KIT-001 (must be
   `LOW-VALUES`/`'Y'`/`'N'`/`' '`)
4. Data deps: none beyond the validated input field
5. Priority: N/A
6. Calculation: **BLOCKED** — `A6O012U`'s own logic contains no cost-rollup arithmetic
   whatsoever; the copybook comment for this switch (`OMGEXPL.CPY` lines 24-30) states its
   meaning precisely — `'Y'` = "ADD UP RESPECTIVE OH & THIRD PARTY FEES OF PACK & SUB PACKS,"
   `'N'` = "DON'T ADD RESPECTIVE OH & THIRD PARTY FEES OF PACK & SUB PACKS" — but the actual
   summation (or non-summation) this describes happens entirely inside `A6O015U`, not supplied.
   `A6O012U` only validates the switch's value and forwards it unchanged
   (`MOVE OMGEXPL-ROLLUP-COST-SW TO OMGPK-ROLLUP-COST-SW`).
7. Output: `OMGPK-ROLLUP-COST-SW` (the forwarded value)
8. Dates: none
9. Exclusions/fallbacks: N/A
10. Errors: invalid values are caught by R-KIT-001, not here
11. Confidence: CONFIRMED that this program does not itself compute any rollup; BLOCKED for what
    `A6O015U` actually does with the switch.

---

## What was scoped out of this pass

- **`A6O015U`'s own logic** — the actual pack-explosion engine. Every business rule for *how* a
  kit is exploded into its components (database access, cost/OH/TP-fee rollup arithmetic per
  R-KIT-007, `OMGPK-COMPLETE-EXPLODE-SW`'s meaning, `OMGPK-PACK-STATUS`'s meaning) lives here and
  is entirely BLOCKED — not supplied in `upload/`.
- **`OMGPK.CPY`'s formal field definitions** — BLOCKED, not supplied; see
  `omgexpl-data-dictionary.md`'s `OMGPK` section for what could be inferred from usage alone.
- **`SYH208`/`SYU208`'s own date-validation logic** — treated as a black-box shared utility, not
  independently examined (out of scope for kit-processing specifically).

## Assumptions

1. `A6O015U` is assumed to return `OMGPK-COMP-INFO` rows with sub-pack (`'S'`) entries appearing
   before the level-2 component rows that reference them, since `4000-FIND-PARENT-PROD`'s
   sequential-array lookup only works correctly under that ordering (R-KIT-003 item 3) — not
   independently verified.
2. The `'A'`/`'E'`/`'W'` severity distinction on `OMGEXPL-ERROR-RESPONSE-FLAG` is assumed to
   matter only to callers outside `A6O012U` itself, since this program's own logic treats `'A'`
   and `'E'` identically (both skip `0020-PROCESS`) — not confirmed against any actual caller.

## Blockers

1. `A6O015U` — the actual explosion engine; not supplied. This is the single largest blocker for
   fully characterizing kit processing, on par with `CUS120` for JIT/fee processing.
2. `OMGPK.CPY` — not supplied; every field's exact `PIC` clause and full field list beyond what
   `A6O012U.CBL` happens to reference by name is unknown.
3. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set.

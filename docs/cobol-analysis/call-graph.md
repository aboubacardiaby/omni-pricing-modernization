# Call Graph — CICS Links, COMMAREA Structures, and Product-Type Routing (T002)

**Scope:** A standalone call-graph artifact for every program supplied in `upload/`: caller/callee
relationships, the exact record exchanged on each edge (with size where confirmable), CICS
response-code handling, product-type/request routing conditions, and unresolved (unsupplied)
targets. This document supersedes the embedded summary in
`docs/cobol-analysis/program-inventory.md` §3 with directly re-verified, edge-by-edge detail —
every routing condition and error message below was re-read from source for this task, not
carried forward from the summary.

**Source of truth:** COBOL as read directly in `upload/A6X01.CBL` (full, 230 lines),
`upload/A6O011U.CBL` (top-level orchestration, ~450 lines of its 1045 read), `upload/CUP100
(1).CBL` (the `CUP120` call site), `upload/A6U01.CBL` (the NDP call site and its surrounding
gate), `upload/A6R10.CPY` (full, 45 lines), plus the already-complete `A6O012U.CBL`/`OMGEXPL.CPY`
read for T004 (`docs/cobol-analysis/kit-processing.md`).

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Diagram (CONFIRMED, re-verified edge-by-edge below)

```text
(unsupplied online driver) --?--> CUP100 --CALL 'CUP120'--> CUP120 (compile-time shell)
                                                                 --INC(compile-time)--> CUS120  [MISSING]

(unsupplied online driver) --EIBCALEN-sized COMMAREA--> A6X01
    A6X01 --CICS LINK, COMMAREA=A6R10-REC(250B)--> A6O010U --CICS LINK--> A6O016U
                                                                 --SQL SELECT--> VNG02,VNG06,ING01,VNG05
    A6X01 --CICS LINK, COMMAREA=OMGPR--> A6O011U   [only if A6W10-PROD-TYPE = 'O', i.e. a kit]
        A6O011U --CICS LINK, COMMAREA=A6W013U-PARM-LIST--> A6O013U --CICS LINK--> A6O016U (shared DAO)
        A6O011U --CICS LINK, COMMAREA=OMGEXPL--> A6O012U --CICS LINK, COMMAREA=OMGPK--> A6O015U  [MISSING]
        A6O011U --CICS LINK, COMMAREA=OMGPR--> A6U01   [once per exploded component, looped]
    A6X01 --CICS LINK, COMMAREA=OMGPR--> A6U01   [else: A6W10-PROD-TYPE <> 'O', regular component]

    A6U01 --dynamic CALL (no CICS LINK, no RESP trap), COMMAREA=A6W001-PARM-LIST--> A6P001WB [MISSING]
              (gated on OMGPR-ECOMMERCE-PRICING; the on/off feature-flag check that would
               additionally gate this is commented out, see R-CALL-006)
    A6U01 --~183 EXEC SQL SELECT/cursor paragraphs--> DB2 (see docs/cobol-analysis/
              sql-query-inventory.csv and db2-table-inventory.md, T005)
```

---

## R-CALL-001 `A6X01` entry: variable-length COMMAREA, unconditional date fetch, unconditional product-type lookup
1. ID/Name: R-CALL-001 A6X01 entry and mandatory product-type lookup
2. Program/paragraph: `A6X01`, `000-LINK-SUBROUTINE` (68-93), `025-GET-DATE` (94-112),
   `200-LINK-A6O010` (180-217)
3. Mechanism: `A6X01`'s own `DFHCOMMAREA` is declared `05 COMMAREA PIC X OCCURS 1 TO 32767
   DEPENDING ON EIBCALEN` — a variable-length byte string, not a fixed record. If `EIBCALEN = 0`
   (caller sent no data), `A6X01` never attempts any downstream call at all — it immediately sets
   `OMGPR-F-PRICER-ERROR='Y'`, message `'NO DATA RECEIVED BY A6X01'`, and returns. Otherwise the
   raw COMMAREA bytes are moved into a working `OMGPR` copy (line 70), meaning the caller is
   expected to have sent something shaped like `OMGPR` (or a valid prefix of it), though nothing
   in `A6X01` itself validates the length against `OMGPR`'s own size.
4. Routing/gating condition: after fetching the current date (SQL `CURRENT_TIMESTAMP`, fatal on
   any `SQLCODE` other than 0 — error text `'A6X01- SQL ERROR'`), `A6X01` **unconditionally**
   calls `A6O010U` next (`200-LINK-A6O010`), via `A6R10-REC` — **NOT** the full `OMGPR` record —
   a fixed 250-byte structure (`A6R10.CPY`, confirmed by direct read) keyed by
   `OMGPR-I-VENDOR`/`OMGPR-I-VND-PRODUCT` (12 bytes of input), returning
   `A6W10-PROD-DESC`/`-PROD-TYPE`/`-PROD-STATUS`/`-PROD-EFF-DATE`/`-PROD-EXP-DATE` plus the
   shared `GENERAL-RESPONSE-FLAG`/`ERROR-NUMBER`/`ABEND-MSG` fields already documented in
   `program-inventory.md` §6.4. **CONFIRMED discrepancy worth flagging:** the program's own
   header comment (line 26-28) reads "ADDED LOGIC TO LINK TO A6O010U IF PROD TYPE NOT GIVEN IN
   OMGPR," but the actual code calls `A6O010U` unconditionally on every request, with no
   `IF OMGPR-INPUT-PRODUCT-TYPE = SPACES`-style guard anywhere in `000-LINK-SUBROUTINE`. Either
   the guard was removed at some point without updating the comment, or it never worked the way
   the comment describes — the code, not the comment, is authoritative per this repository's
   conventions, and the code always performs the lookup.
5. Response handling: `A6O010U`'s `EXEC CICS LINK` uses the standard 3-way pattern (already
   documented generally in `program-inventory.md` §6.5, exact messages confirmed here):
   `DFHRESP-NORMAL` with `A6W10-ABEND`/`A6W10-ERROR` clear -> `A6W10-PROD-TYPE` copied into BOTH
   `OMGPR-INPUT-PRODUCT-TYPE` and `OMGPR-C-PRODUCT-TYPE`; `DFHRESP-NORMAL` with abend/error set ->
   fatal, `OMGPR-ERROR-MESSAGE` from `A6W10-ERROR-MESSAGE`; `DFHRESP-PGMIDERR` -> fatal,
   `'A6O010U TRANSACTION IS UNAVAILABLE'`; `OTHER` -> fatal, `'ERROR-UNABLE TO LINK TO A6O010U'`.
   **Any failure here — abend/error from the lookup itself, or a CICS-level LINK failure — aborts
   the whole request before `050-PROCESS-PRICE` (R-CALL-002) ever runs**: `000-LINK-SUBROUTINE`
   checks `A6W10-ABEND OR A6W10-ERROR` immediately after the LINK and skips straight to returning
   the error if either is set (lines 75-81).
6. Confidence: CONFIRMED

---

## R-CALL-002 Product-type routing: kit (`A6O011U`) vs. regular component (`A6U01`)
1. ID/Name: R-CALL-002 A6X01's product-type dispatch to the kit path or the direct pricer path
2. Program/paragraph: `A6X01`, `050-PROCESS-PRICE` (113-125), `100-LINK-TO-PRICER` (126-155),
   `110-LINK-TO-A6U11` (156-179)
3. Mechanism/condition: **CONFIRMED, the single routing decision this whole document is most
   concerned with:** `IF A6W10-PROD-TYPE = 'O' PERFORM 110-LINK-TO-A6U11 ELSE PERFORM
   100-LINK-TO-PRICER` (line 117-123). **Disambiguation worth stating explicitly, since the same
   letter means something different elsewhere in this codebase:** here, `A6W10-PROD-TYPE = 'O'`
   means "this product IS a kit/pack that must be exploded before it can be priced" — a
   completely different meaning from `OMGPR-C-PRIVATE-LBL = 'O'` (INFERRED "Owens-sourced," per
   `docs/rules/fees-and-adjustments.md`'s `R-JIT-005`) or `A6O016U`'s `'O'`=Owens-kit/`'S'`=
   supplier-kit product-TYPE convention (`program-inventory.md` §1.6). Three different `'O'`
   domains exist across this codebase; do not assume a shared meaning from the letter alone.
4. Non-kit path (`100-LINK-TO-PRICER`): `INITIALIZE OMGPR-DATA-FOR-PACKS` and
   `OMGPR-PRC-OUTPUT-FIELDS`, set `OMGPR-PRICING-REQ-SW='P'` (pricing request) and
   `OMGPR-INPUT-PRODUCT-TYPE='R'` (regular component), then `EXEC CICS LINK PROGRAM('A6U01')
   COMMAREA(OMGPR)` — full `OMGPR` record. On `DFHRESP-NORMAL`, additionally sets
   `OMGPR-C-PRODUCT-TYPE='R'`.
5. Kit path (`110-LINK-TO-A6U11`): `INITIALIZE OMGPR-PRC-OUTPUT-FIELDS` only (does not reset
   `OMGPR-DATA-FOR-PACKS`, unlike the non-kit path), then `EXEC CICS LINK PROGRAM('A6O011U')
   COMMAREA(OMGPR)` — also the full `OMGPR` record. `A6O011U` itself sets
   `OMGPR-C-PRODUCT-TYPE='O'` at its own `0030-EOJ` (confirmed, `A6O011U.CBL` line 279), so this
   field's final value differs depending on which path ran, independent of what `A6X01` itself
   set.
6. Response handling: both paths use the standard 3-way `DFHRESP` pattern with path-specific
   messages: pricer path — `'A6U01 TRANSACTION IS UNAVAILABLE'` / `'ERROR-UNABLE TO LINK TO
   A6U01'`; kit path — `'A6O011 TRANSACTON IS UNAVAILABLE'` (**CONFIRMED literal typo in source,
   missing an "I" — preserved verbatim, not corrected**) / `'ERROR-UNABLE TO LINK TO A6O011U'`.
7. Confidence: CONFIRMED

---

## R-CALL-003 `A6O011U`'s internal orchestration: category lookup, then explosion, then per-component pricing
1. ID/Name: R-CALL-003 Kit-path internal call sequence
2. Program/paragraph: `A6O011U`, `0020-PROCESS` (241-275), `0100-GET-CATEGORY` (288-324),
   `0200-EXPLODE` (326-353), `2000-PROCESS-PRICE` (361-...)
3. Mechanism/sequence (CONFIRMED, directly read, three sequential stages, each gated on the
   previous succeeding):
   1. **`0100-GET-CATEGORY`** -> `EXEC CICS LINK PROGRAM(A6W013U-PROGRAM) COMMAREA
      (A6W013U-PARM-LIST)` to `A6O013U`, keyed by vendor/product/UOM/division. On success:
      populates `OMGPR-S-PROD-CATEGORY`, `OMGPR-C-VND-PROD-BASE-UM`, `OMGPR-C-INV-CLASS`, and —
      only if the ordered UOM differs from the product's base UOM — `OMGPR-ALT-ORD-UM`,
      `OMGPR-ALT-ORD-CONV-FACTOR`, and (notably) `OMGPR-D-EXPIRATION` directly from
      `A6W13-PROD-CAT-EXP-DATE` (a direct assignment, not fed through any closest-expiration-array
      mechanism — `A6O011U` has no equivalent of `A6U01`'s `date-selection.md` machinery). On
      `DFHRESP-PGMIDERR`/`OTHER`: fatal, `'A6O013U TRAN IS UNAVAILABLE'` /
      `'CICS LINK ERROR TO A6O013U'`. `A6W13-ABEND`/`A6W13-ERROR` (the lookup itself failing, even
      on a normal CICS response) is also fatal, built via `9998-BUILD-ERROR-MSG` (not
      independently re-traced this pass).
   2. **`0200-EXPLODE`** (only reached if step 1 succeeded) -> `EXEC CICS LINK
      PROGRAM(A6W012U-PROGRAM) COMMAREA(A6W012U-PARM-LIST)` to `A6O012U`, with
      `OMGEXPL-REQUEST-TYPE` explicitly set to `'C'` (Components-only — per
      `docs/cobol-analysis/kit-processing.md`'s `R-KIT-002`, this means sub-pack rows are tracked
      internally but never returned, and level-2 component parent references are blanked). On
      `DFHRESP-PGMIDERR`/`OTHER`: fatal, `'A6O012U TRAN IS UNAVAILABLE'` /
      `'CICS LINK ERROR TO A6O012U'`. `OMGEXPL-ABEND`/`OMGEXPL-ERROR` (the explosion itself
      failing) is also fatal.
   3. **`2000-PROCESS-PRICE`** (only reached if step 2 succeeded) -> `PERFORM VARYING SUB FROM 1
      BY 1 UNTIL SUB > OMGEXPL-NUMBER-OF-ITEMS OR WS-ERROR OR OMGPR-PRICER-ERROR`: for **each**
      exploded component row, `A6O011U` calls `9000-LINK-TO-PRICER` (`EXEC CICS LINK
      PROGRAM(A6U01-PROGRAM) COMMAREA(A6U01-PARM-LIST)`, i.e. `A6U01` again, full `OMGPR` record)
      to price that individual component, accumulating pack-level totals
      (`WS-PACK-TOTAL-COST`, `WS-PACK-TOTAL-ADJCST`, `WS-PACK-JIT-*`, `WS-PACK-CUR-*-REBATE`, and
      many more — a long initialized list, lines 364-387). **`A6U01` is therefore called once per
      exploded component, not once per kit** — a kit with, say, 5 components results in 5
      separate `A6U01` LINK calls from within this one loop, each pricing one component
      independently, with `A6O011U` itself responsible for summing the results into pack totals.
4. Two additional, narrower `9000-LINK-TO-PRICER` call sites exist beyond the main loop
   (`4010-GET-JIT-ON-COST`, setting `OMGPR-PRICING-REQ-SW='J'` first, and a kit-level suggested-
   sell pass around line 740 that overrides `OMGPR-A-TOTAL-COST`/`OMGPR-C-ORD-LIN-CUST-UOM` before
   calling) — these were not traced to full business-rule depth this pass (BLOCKED/deferred,
   consistent with this document's call-graph scope rather than full decision-table extraction);
   flagged here only as confirmation that `A6U01` is invoked from more than one place within
   `A6O011U`, not only the per-component loop.
5. Confidence: CONFIRMED for the three-stage sequence and the per-component pricing loop.
   BLOCKED/deferred for the two additional `9000-LINK-TO-PRICER` call sites' full business
   context (out of scope for a call-graph document; candidate follow-on work for a kit-specific
   decision-table extraction).

---

## R-CALL-004 Shared DAO pattern: two independent online wrappers over the same lower-level program
1. ID/Name: R-CALL-004 A6O016U as a shared data-access target
2. Program/paragraph: `A6O010U` -> `A6O016U`, and `A6O013U` -> `A6O016U` (both CICS LINK; neither
   independently re-verified to full internals this pass beyond what `program-inventory.md` §1.6/
   §1.7 already established)
3. Mechanism: **CONFIRMED (carried forward from `program-inventory.md` §3, not contradicted by
   anything found this pass):** `A6O016U` is the actual SQL-issuing program behind both `A6O010U`
   (product description/type/status lookup, called from `A6X01`) and `A6O013U` (product
   category/UOM/inventory-class lookup, called from `A6O011U`'s kit path) — two structurally
   independent thin online wrappers converging on one shared lower-level data-access program, the
   same "thin wrapper over a shared engine" shape `A6O012U`/`A6O015U` also follows (R-CALL-005).
4. Confidence: CONFIRMED (via T001's already-established read of `A6O016U`, re-cited not
   re-derived this pass)

---

## R-CALL-005 `A6O012U` → `A6O015U`: full detail in `kit-processing.md`
1. ID/Name: R-CALL-005 Kit-explosion engine delegation
2. Cross-reference: fully documented in `docs/cobol-analysis/kit-processing.md`'s `R-KIT-005`
   (CICS LINK, `COMMAREA=OMGPK`, messages `'A6U15 TRANSACTION IS UNAVAILABLE'` /
   `'A6U15 LINK ERROR '`, both landing on the non-abend `'E'` severity). Not re-derived here;
   included in this document's diagram for completeness since it's part of the same overall call
   graph.
3. Confidence: CONFIRMED (per T004)

---

## R-CALL-006 `A6U01` → `A6P001WB`: the one edge that is a raw `CALL`, not a CICS LINK
1. ID/Name: R-CALL-006 NDP (net-delivered-price) web-service delegation
2. Program/paragraph: `A6U01`, `9750-CHK-FOR-NET-PRICING` (entry gated at line 12361,
   `IF OMGPR-ECOMMERCE-PRICING`), `9800-LINK-TO-NDP` (24706-24713)
3. Mechanism: **CONFIRMED, and a genuine structural outlier among every other cross-program edge
   in this document** — `A6U01` does not `EXEC CICS LINK` to `A6P001WB` at all. It performs a
   plain COBOL `CALL A6W001-PROGRAM USING A6W001-PARM-LIST` (line 24709), where
   `A6W001-PROGRAM` is a working-storage field hardcoded to the literal `'A6P001WB'` (line 24615)
   — a dynamic-call-by-content, but with no `RESP` clause and no `DFHRESP` evaluation of any kind
   around the call itself. **Every other program-to-program edge in this call graph is a CICS
   `LINK` with the standard 3-way `DFHRESP-NORMAL`/`PGMIDERR`/`OTHER` graceful-degradation
   pattern; this is the only edge with no CICS-level failure trap.** If `A6P001WB` were
   unavailable or failed to load, the behavior would be whatever plain COBOL `CALL` failure
   behavior the runtime provides (e.g. an uncaught abend) — there is no graceful
   "TRANSACTION IS UNAVAILABLE" message path for this specific edge the way there is for every
   `LINK`-based one. (`CUP100`'s `CALL 'CUP120'`, R-CALL-007, shares this same raw-`CALL`
   shape and the same absence of a CICS-level trap.)
4. Routing/gating condition: entered only when `OMGPR-ECOMMERCE-PRICING` (an 88-level condition,
   not independently re-verified against its own field/value this pass) is true. **CONFIRMED,
   notable finding:** an additional on/off feature-flag guard is visibly present in the source but
   **entirely commented out** (lines 24606-24610: `NK0615*    IF  WS-TURN-NDP-ON-YN = 'Y' ...
   ELSE GO TO 9750-EXIT ... END-IF` — every line prefixed with a comment marker). `9810-CHECK-
   NDP-ON-OFF-SWITCH` (a paragraph that reads a `SWITCH_001`/`CHAR_001` pair from a DB2 "SMT"
   table, presumably intended to drive `WS-TURN-NDP-ON-YN`) still exists and is presumably still
   called somewhere to populate that field, but **the check that would actually use it to skip the
   NDP call is disabled**. As currently written, the NDP call fires unconditionally whenever
   `OMGPR-ECOMMERCE-PRICING` is true, regardless of whatever the database-driven switch says. This
   is the same "commented-out control flow leaves a stale short-circuit" pattern already flagged
   elsewhere in this codebase (PANDAC's dropped early-exit in `fees-and-adjustments.md`'s
   `R-PANDAC-000`; the historical JIT-exempt short-circuit in `sell-selection-rules.md`'s
   `R-SELL-001` item 9) — a recurring maintenance idiom in this codebase worth calling out as a
   pattern in its own right, not three unrelated coincidences.
5. Response handling (of the CALL's OUTPUT, since there is no CICS-level trap per item 3):
   `A6W001-WEB-ERROR-NBR` is evaluated after the `CALL` returns: `404` ("price not found in NDP")
   -> `GO TO 9750-EXIT`, non-fatal, falls through to whatever pricing path runs next; `ZEROES`
   (success) -> continue, populate `OMGPR-A-CUS-UOM-SELL-PRC` directly from `A6W001-WEB-NDP` plus
   several other web-response fields (`OMGPR-C-SELLGROUP-INFO`, `OMGPR-T-SELL-COMMENT`,
   `OMGPR-N-BUY-GROUP-SHORT-SELL` from `A6W001-WEB-AGREEMENT-ID`), set
   `OMGPR-C-SELL-LEVEL`/`OMGPR-C-BG-TYPE-SELL='W'` and `OMGPR-PRICING-METHOD='N.D.P.'`; any other
   value -> fatal, `OMGPR-Q-ERROR-NBR` set directly from `A6W001-WEB-ERROR-NBR`,
   `OMGPR-ERROR-MESSAGE` built from the literal `'NDP'` prefix plus `A6W001-WEB-ERROR-MSG`,
   `GO TO 0020-EXIT-PRICER`.
6. Dates: `A6W001-WEB-EXPIRATEDATE` (note: literal field name as spelled in source, not a
   transcription error), if populated, is contributed to the standard closest-expiration array
   (`docs/rules/date-selection.md`'s `R-DATE-003`) — this IS fed through the normal mechanism,
   unlike `A6O011U`'s direct `OMGPR-D-EXPIRATION` assignment in R-CALL-003.
7. Confidence: CONFIRMED for the mechanism, the disabled feature-flag finding, and the response
   handling. BLOCKED for `A6P001WB`'s own behavior (not supplied) and for
   `9810-CHECK-NDP-ON-OFF-SWITCH`'s consuming context (confirmed to read a DB2 switch row, not
   confirmed where/whether `WS-TURN-NDP-ON-YN` is still set from it elsewhere or is now simply
   vestigial).

---

## R-CALL-007 `CUP100` → `CUP120`: batch-style dynamic `CALL`, not CICS LINK
1. ID/Name: R-CALL-007 JIT/fee-adjustment data population via CUP120
2. Program/paragraph: `CUP100`, `A400-GET-JIT-ADJ` (567-...)
3. Mechanism: **CONFIRMED** — `MOVE 'CUP120' TO WS-PGM-ID` then
   `CALL WS-PGM-ID USING WS-DUMMY, WS-DUMMY, SQLCA, CUR120` (lines 577-579) — a dynamic-name
   `CALL` with four parameters (two unused dummy placeholders, the SQL communication area, and
   the `CUR120` record, `EXEC SQL INCLUDE`d as `CUP100`'s own working storage). Same raw-`CALL`
   shape as R-CALL-006 — no CICS LINK, no `RESP`/`DFHRESP` trap.
4. Routing/gating condition: called unconditionally from `CUP100`'s own processing sequence (not
   gated on any product-type or request-type condition — every `CUP100` invocation populates
   `CUR120` this way).
5. Response handling: `IF CUR120-GENERAL-RESPONSE-FLAG = 'A' OR 'E'` -> fatal,
   `OMGPR-Q-ERROR-NBR=2`, message from `CUR120-GENERAL-RESPONSE-MSG` (already cataloged in
   `program-inventory.md` §6.2 and `error-catalog.md`); else -> roughly a dozen `CUR120-JIT-*`
   fields are copied into their `OMGPR-JIT-*` counterparts (the same fields
   `fees-and-adjustments.md`'s JIT section, built independently in T009, documents as consumed by
   `A6U01`'s `7225-PRO-JIT-ADJ-010` — this call site is where those fields are actually
   populated, upstream of `A6U01` ever running).
6. **CONFIRMED, the deepest and most consequential unresolved target in this entire call graph:**
   `CUP120` (present as a file in `upload/`) is, per `program-inventory.md` §1.9, an empty
   compile-time shell — its real logic is `-INC CUS120` (a compile-time `COPY`/include, not a
   further `CALL`), and `CUS120` is **not supplied**. This means the actual business logic behind
   every `OMGPR-JIT-*` field's value — the entire JIT/fee-eligibility determination that
   `fees-and-adjustments.md`'s JIT/LUOM sections trace usage of but cannot trace origin for — is
   completely BLOCKED. This is the single largest evidence gap affecting the fee/adjustment rule
   area, larger in impact than any missing DCLGEN, since it is missing *logic*, not just missing
   *schema*.
7. Confidence: CONFIRMED for the call mechanism and response handling. BLOCKED for `CUS120`'s own
   logic (not supplied) — already flagged as a standing blocker in `program-inventory.md` §7 and
   every downstream document that consumes `OMGPR-JIT-*` fields; repeated here because this is
   the call graph's own record of exactly where that gap sits structurally.

---

## Unresolved (unsupplied) call targets — consolidated

| Missing program | Called by | Mechanism | Consequence |
|---|---|---|---|
| `CUS120` | `CUP120` (compile-time `-INC`, not a `CALL`) | N/A — compile-time include | The actual JIT/fee-eligibility logic behind every `CUR120`/`OMGPR-JIT-*` field is entirely unknown; the largest blocker in this codebase (R-CALL-007) |
| `A6O015U` | `A6O012U` | CICS LINK | The actual kit-explosion engine; `kit-processing.md`'s R-KIT-003/007 depend on inferring its behavior from `A6O012U`'s consumption of its output |
| `A6P001WB` | `A6U01` (dynamic `CALL`, no CICS trap) | raw `CALL` | The NDP/e-commerce net-delivered-price web service; entire "N.D.P." sell-pricing-method path (`docs/rules/sell-selection-rules.md` does not yet cover this method — candidate gap for that document) |
| `A6O011UB` | (none — mentioned only in `A6O011U`'s header comment, never invoked anywhere in this upload set) | N/A | Batch sibling of `A6O011U`; no confirmed call site exists to characterize |
| `CUP121` | (none — mentioned only in `CUP120`'s header comment) | N/A | Batch sibling of `CUP120`; same standing as above |
| `CUP110` | (none — mentioned only in an `A6U01` comment) | N/A | Purpose unconfirmed |
| (unsupplied online transaction driver) | — | — | Whatever ultimately invokes `CUP100` and `A6X01` from a real terminal/transaction is outside this upload set entirely |

---

## What was scoped out of this pass

- **`A6O011U`'s two additional `9000-LINK-TO-PRICER` call sites** beyond the main per-component
  loop (`4010-GET-JIT-ON-COST` and the kit-level suggested-sell pass) — confirmed to exist and to
  target `A6U01`, not traced to full business-rule depth (R-CALL-003 item 4).
- **`9810-CHECK-NDP-ON-OFF-SWITCH`'s own SQL and the SMT table it reads** — confirmed to exist and
  read `SWITCH_001`/`CHAR_001`, not traced further since the check it feeds is itself disabled
  (R-CALL-006).
- **Full re-verification of `A6O010U`→`A6O016U` and `A6O013U`→`A6O016U`'s own internals** — cited
  from `program-inventory.md` §1.6/§1.7/§3, not re-read line-by-line this pass (R-CALL-004).

## Assumptions

1. `OMGPR-ECOMMERCE-PRICING` is assumed to be an 88-level condition gating R-CALL-006 as its name
   suggests — its own field/value declaration was not independently re-verified this pass.
2. `A6W001-PARM-LIST`'s exact byte size was not computed (unlike `OMGEXPL`'s exact reconciliation
   in T004) — its field list is confirmed by usage (R-CALL-006 item 5) but not its total length.

## Blockers

1. `CUS120` — see R-CALL-007, the largest blocker in this document.
2. `A6O015U`, `A6P001WB` — see R-CALL-005/006.
3. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set;
   in particular, R-CALL-006's raw-`CALL`-with-no-trap finding could only be confirmed as a
   *structural* difference from the LINK-based edges via static analysis — its actual runtime
   failure behavior (does an unavailable `A6P001WB` abend the whole CICS transaction? per COBOL/
   CICS convention this is likely, but not observed) was not verified against a live system.

# A6U01 and Related Programs — Error Catalog (T006)

**Scope:** Every error number/code/message-producing path across the pricing-engine programs
supplied in `upload/`, mapped to its triggering paragraph, the `OMGPR` fields it touches, and
whether it stops (aborts the entire pricer) or continues (a local/soft failure). Covers
validation, SQL, CICS, kit, and component error paths per T006's acceptance criteria.

**Source of truth:** COBOL as read directly in `upload/A6U01.CBL` (the primary source of this
document's error-site inventory) and, for the CICS/kit/component categories, the per-program
detail already built in `docs/cobol-analysis/program-inventory.md` §6 (T001), re-verified and
extended here with stop/continue behavior that §6 explicitly deferred as out of its own scope
("full per-code precondition/paragraph mapping... is not T001 inventory scope").

**Methodology:** the SQL-error inventory (§3, and the full reference table in the appendix) was
built **mechanically** — a script scanned every `MOVE <n> TO OMGPR-Q-ERROR-NBR` and
`MOVE <n> TO WS-DB-ERROR-NBR` site in `A6U01.CBL` (213 raw sites, 183 distinct numeric codes after
normalizing leading-zero/sign formatting variants of the same literal), tracked which paragraph
each site falls in (using the same THRU-continuation-line fix already applied in T005's
`sql_pipeline.py`), and checked whether `GO TO 0020-EXIT-PRICER` appears within the following 15
lines to classify each site as fatal ("STOP") or not. This is a verifiable, repeatable technique,
not a sample — every numbered error site in `A6U01.CBL` is accounted for in the appendix table.
Validation-category and CICS/kit/component sections were read directly rather than mechanically
scanned, since those categories are smaller and more heterogeneous in shape.

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## 1. Architecture of error handling in `A6U01` (read this first)

### 1.1 Two independent fields: a specific error number and a separate severity bucket

`OMGPR.CPY` declares two distinct output fields that are easy to conflate but are **assigned
independently**:

- **`OMGPR-Q-ERROR-NBR`** — a specific, paragraph-chosen numeric code. Values observed in
  `A6U01.CBL` range from 1 to 1026 (see appendix) — this is a flat, per-program numbering space,
  not a bounded severity scale.
- **`OMGPR-Q-ERROR-CODE`** — a severity-bucket field with 88-level names already cataloged in
  `program-inventory.md` §6.1 (`OMGPR-PRICER-NO-ERROR`=0, `OMGPR-UM-NOT-FOUND`=1, ...,
  `OMGPR-DB2-FATAL-ERROR`=70, with 1–39/40–69/70–99 read as warning/medium/fatal bands).

**CONFIRMED, and worth stating plainly since it resolves an apparent contradiction in the T001
severity table:** the overwhelming majority of `A6U01`'s fatal SQL-error paths — 182 of the 183
distinct `OMGPR-Q-ERROR-NBR` codes found — set `OMGPR-Q-ERROR-CODE` to the **same hardcoded
constant**, `WS-DB2-FATAL-ERROR-IND`, declared once as `PIC 9(03) VALUE 70` (line 699) and moved
into `OMGPR-Q-ERROR-CODE` at every one of those sites. This means most `OMGPR-Q-ERROR-NBR` values
(e.g. 5, 45, 90 — numbers that would fall in the "warning" 1–39 band if the two fields shared one
scale) are paired with a severity bucket of 70 (fatal), not a value matching their own numeric
range. **The two fields must not be treated as one field with an inferred severity from its
number** — `OMGPR-Q-ERROR-NBR` identifies *which specific check failed*; `OMGPR-Q-ERROR-CODE`
(almost always 70 in practice) identifies *how the caller should react*. The rare exceptions where
`OMGPR-Q-ERROR-CODE` is set to something other than 70 are called out individually in §2.

### 1.2 The dominant pattern: any unexpected `SQLCODE` aborts the entire pricer

**CONFIRMED, mechanically verified across all 183 codes:** 182 of them follow one identical shape,
repeated essentially verbatim at every SQL-driven paragraph in `A6U01.CBL`:

```cobol
WHEN OTHER
    MOVE 'SELECT'          TO WS-DB-OPERATION
    MOVE '<table-name>'    TO WS-DB-NAME
    MOVE SQLCODE           TO WS-DB-RETURN-CODE
    MOVE 'DB ERROR'        TO WS-DB-STATUS-MSG
    MOVE <n>               TO WS-DB-ERROR-NBR OMGPR-Q-ERROR-NBR
    MOVE WS-DB2-FATAL-ERROR-IND TO OMGPR-Q-ERROR-CODE
    MOVE WS-DB-OPERATION-MSG    TO OMGPR-ERROR-MESSAGE
    MOVE WS-YES-IND        TO OMGPR-F-PRICER-ERROR
    GO TO 0020-EXIT-PRICER
```

`0020-EXIT-PRICER` (already referenced throughout `docs/rules/*.md`) is the single shared exit
point that moves the whole `OMGPR` record to `DFHCOMMAREA` and returns control to CICS — **there
is no partial-result path**. An unexpected `SQLCODE` anywhere in this 26,657-line program, from
any of the ~180 SELECT/cursor paragraphs it contains, produces the same outcome: the entire
pricing request for that line fails, with `OMGPR-F-PRICER-ERROR='Y'`, `OMGPR-ERROR-MESSAGE`
identifying the failed table/operation, and no sell price of any kind returned. `SQLCODE=100`
(not found) and `SQLCODE=0`/`+001` (found) are the only two outcomes every one of these paragraphs
treats as non-fatal — every other `SQLCODE`, including transient/deadlock codes a retry might
otherwise recover from, is fatal-by-default in this codebase.

**Only one confirmed exception exists to this pattern in the entire mechanical scan: `#145`**
(the closest-expiration-date array overflow, already fully documented in
`docs/rules/date-selection.md`'s `R-DATE-003` — it sets the error fields but returns normally via
`GO TO 7695-EXIT` rather than aborting, and this session's earlier analysis flagged that not every
calling site necessarily escalates it). See §4 for this and the other limit/overflow errors as a
family, and §5 for Surcharge's own, differently-shaped exception.

### 1.3 Message assembly

Every fatal-path message is assembled from up to four working-storage pieces
(`WS-DB-OPERATION`, `WS-DB-NAME`, `WS-DB-RETURN-CODE`, `WS-DB-STATUS-MSG`) concatenated into
`WS-DB-OPERATION-MSG` and then moved into `OMGPR-ERROR-MESSAGE` — i.e. the outbound message text
is templated, not individually authored per error number, except for the validation-category
errors in §2, which write literal, hand-authored message text.

---

## 2. Validation errors (hand-authored messages, not generic SQL-failure text)

These are the errors worth reading individually rather than via the mechanical table, because
their preconditions are business/input-shape checks rather than "a SELECT returned an unexpected
SQLCODE."

### 2.1 Missing required input (102–105)
- **Program/paragraph:** `A6U01`, `7185-PRO-PASSED-DATA-010` (lines 12187–12264, per
  `program-inventory.md` §6.2)
- **Trigger:** `OMGPR-I-DIVISION`/`OMGPR-S-ACCOUNT` (or `OMGPR-I-ACCOUNT`)/`OMGPR-I-VENDOR`/
  `OMGPR-I-VND-PRODUCT` blank/zero on entry to the pricer
- **Codes/messages:** 102 `'MUST PASS DIVISION NUMBER TO A6X01'`, 103 `'...ACCOUNT NUMBER...'`,
  104 `'...VENDOR NUMBER...'`, 105 `'...PRODUCT NUMBER...'`
- **CONFIRMED bug, preserved as-is (already flagged in `program-inventory.md` §6.2, repeated here
  since it belongs in this catalog too):** the same paragraph's missing-pricing-date check writes
  message text `'#135 - MUST PASS DATE TO A6X01'` but sets `OMGPR-Q-ERROR-NBR` to **102** (not
  135) at line 12262 — the message and the numeric code disagree, a copy-paste artifact.
  Downstream consumers keying off the numeric code rather than the message text would misclassify
  this specific failure as "division missing."
- **`CUP100`'s independent, non-unified equivalents:** 102 (`A100-PRO-PASS-DATA`, division
  missing), 103 (same paragraph, account missing), 106 (`A200-SEL-ACCOUNT`, account not found —
  note this REUSES 102/103's numeric neighborhood for a semantically different check), 108
  (`A310-SEL-ACTIVE-CUST`, active customer not found). **`OMGPR-Q-ERROR-NBR` 102/103 mean
  different things depending on which program set them** — confirmed not globally unique
  (`program-inventory.md` §6.2's own flag, repeated here for completeness).
- **Behavior:** STOP (fatal `GO TO 0020-EXIT-PRICER`) in all cases.

### 2.2 Pricing-method-invalid family (112, 115–119)
- **Program/paragraph:** one per sell-arrangement-level SELECT paragraph documented in
  `docs/rules/sell-selection-rules.md`'s `R-SELL-002` level table — `7122-SEL-SELL-SSC-010` (112),
  `7125-SEL-SELL-CAT-010` (115), `7115-SEL-SELL-DFLT-010` (116), `7130-SEL-SELL-PRODUCT-010`
  (117), `7135-SEL-SELL-VEND-CNT-010` (118), `7120-SEL-SELL-VENDOR-010` (119)
- **Trigger:** message text `'PRICING METHOD IS INVALID FOR '` (each with its own level-specific
  suffix, not individually re-transcribed here) — fires when the row found at that sell-arrangement
  level carries a pricing-method code this paragraph doesn't recognize
- **Behavior:** STOP. **Cross-reference:** these six paragraphs are exactly the `7130`/`7135`/
  `7125`/`7120`/`7115`/`7122` sub-paragraphs `R-SELL-002` (item 11) left BLOCKED for internal
  detail — this error family is the one piece of their internals this pass did confirm, via the
  mechanical scan, without opening the full paragraph bodies.

### 2.3 Buy-group price-parameter errors: missing row (124) vs. unrecognized flag value (135)
- **#124**, `7810-SEL-BG-FLAGS-010` (line ~18657): fires on `SQLCODE=+100` — the buy-group's own
  `BGG20` price-parameter row doesn't exist at all. **Notably, this is one of the few sites where
  `OMGPR-Q-ERROR-CODE` is NOT the generic 70-fatal constant** — it's set to
  `WS-BG-PRC-PARM-NOT-FND-IND`, which matches `program-inventory.md` §6.1's severity-bucket code
  41 (`OMGPR-BG-PRC-PARM-NOT-FND`) exactly. This is one of the rare cases where the severity-bucket
  field carries real per-error meaning rather than the blanket 70.
- **#135**, `0205-PRO-FREIGHT-COST-010` and `7820-PRO-CORP-FRT-010` (already documented in
  `docs/rules/cost-selection-rules.md`'s cross-references and `fees-and-adjustments.md`'s freight
  section): fires when the `BGG20` row **does** exist but its flag value doesn't match any of the
  expected cases (`'N'`/`'C'`/`'Y'`) — message `'FLG IN BG PRICE PARAMETER NOT CORRECT'`, a
  defensive "should be unreachable" guard, not a normal business condition.
- **Behavior:** STOP for both — but they represent genuinely different failure classes (row
  absent vs. row present with unexpected content) that happen to sound similar; do not conflate
  them in a reimplementation's exception taxonomy.

### 2.4 Special-contract and cost-contract-scenario errors (601, 602, 603)
- **#601** `0235-SEL-INDV-CNT-010`: `'SPECIAL CONTRACT NOTFOUND FOR PRODUCT'` — already fully
  documented in `docs/rules/cost-selection-rules.md`'s `R-COST-001` item 9 (a special-contract
  pricing request whose bypass-flagged individual contract search comes up empty aborts rather
  than falling back to group/acquisition cost).
- **#602/#603** `7090-PRO-SELL-AMTS-010`: `'INVALID WS-COST-CONTRACT ... SCENARIO FOR GRSSMGN'`
  (602) / the cost-plus equivalent (603) — already documented in
  `docs/rules/sell-selection-rules.md`'s `R-SELL-003` item 10, defensive guards for an
  account-pricing-method branch structure that is logically exhaustive by design.
- **Behavior:** STOP for all three.

---

## 3. SQL errors (the dominant pattern)

The ~180 remaining codes all follow §1.2's identical shape. Rather than repeat that shape 180
times, this section groups them by what they protect and points to the full per-code table in the
appendix (§7) plus the decision-table documents where each already has full business context:

- **Cost-selection SQL errors** (individual/group contract lookup, healthcare override,
  price-list/acquisition fallback: codes 5, 12–29, 37–48, 58, 67–68, 88, 97–101, 107, 109–111,
  123, 125–127, 130–133, 148–159, 945–953, 1025–1026) — business context in
  `docs/rules/cost-selection-rules.md`.
- **Sell-selection SQL errors** (sell-arrangement cascades, price lock: codes 8, 32–33, 46–57,
  62–66, 70–85, 90, 94, 107, 123, 148–149) — business context in
  `docs/rules/sell-selection-rules.md`.
- **Fee/adjustment SQL errors** (freight, JIT, PANDAC, delivery, surcharge cascade internals,
  overhead, inventory class: codes 1–4, 6–7, 9, 36, 54, 60–61, 89, 92–93, 109–111, 136, 139–144,
  160, 162, 200–213, 301, 707, 900–904, 924) — business context in
  `docs/rules/fees-and-adjustments.md`.
- **Date/UOM/rounding-adjacent SQL errors** (alternate-UOM resolution, price-lock UOM conversion:
  codes 86–87, 95–96, 701–703) — business context in `docs/rules/date-selection.md` and the
  low-UOM/break-bulk section of `fees-and-adjustments.md`.

Every code, its exact paragraph(s), and its message (where distinct from the generic templated
text) is in the appendix table (§7) — that table, not this narrative, is the authoritative
per-code reference. This section exists to route a reader from "I found error #83" to the right
business-context document quickly.

---

## 4. Limit/overflow errors as a family — inconsistent severity, confirmed

Three structurally similar "array/cursor exceeded its configured maximum" errors exist, and they
do **not** share the same severity — worth calling out as a family precisely because a reader
might otherwise assume they behave the same way:

| Code | What overflowed | Paragraph | Behavior |
|---|---|---|---|
| 142 | Buy-group priority array (surcharge's own priority walk) | `0288`/`0290`/`0292`/`0295`/`0300`/`0305` (6 sites, all surcharge-family) | **STOP** (fatal) |
| 145 | Closest-expiration-date candidate array (`WS-D-CHECK`, max 50 entries) | `7695-ADD-EXP-DATE-ARRA-010` | **CONTINUE** (soft — see `date-selection.md` R-DATE-003 item 10 for the full analysis, including the open question of whether every caller escalates it) |
| 701–703 | Alternate-UOM working array (`WS-MAX-ALTER-UOM-ENTRIES`, LUOM/break-bulk resolution) | `7872-LOAD-ALTER-UOM` | **STOP** (fatal — see `fees-and-adjustments.md`'s low-UOM/break-bulk `R-LUOM-002` item 10) |

**CONFIRMED:** #145 is the only one of these four overflow scenarios that doesn't abort the
pricer. There is no comment anywhere in the source explaining why the expiration-date array's
overflow is treated more leniently than the other two — flagged as a genuine, unexplained
asymmetry rather than assumed to be intentional.

---

## 5. Surcharge's parallel, mostly-siloed error mechanism

**CONFIRMED, and the reason this category didn't show up at all in the mechanical
`OMGPR-Q-ERROR-NBR`/`WS-DB-ERROR-NBR` scan:** `0215-SURCHARGE-DTLS-010` and its sixteen-branch
cascade (`docs/rules/fees-and-adjustments.md`'s `R-SURCHARGE-001`) use an **entirely separate**
error-tracking mechanism — a local `A9G36-SURCHARGE-FAILURE` flag plus per-branch
`WS-WS-VNGnn-SQLCODE`/`WS-VNGnn-ERROR-MESSAGE` fields (e.g. `WS-WS-VNG16-SQLCODE`,
`WS-VNG16-ERROR-MESSAGE`) — neither of which is `OMGPR-Q-ERROR-NBR` or `WS-DB-ERROR-NBR`. This
was already identified qualitatively in `fees-and-adjustments.md`'s `R-SURCHARGE-001` item 5 and
`R-SURCHARGE-002` item 10 ("a genuine outlier in this codebase's error-handling conventions") —
restated here because it means **surcharge's SQLCODE-OTHER failures are invisible to any
downstream system that only watches `OMGPR-Q-ERROR-NBR`**, and confirmed by this session's
mechanical scan finding zero surcharge-cascade sites among the 183 `OMGPR-Q-ERROR-NBR`/
`WS-DB-ERROR-NBR` assignments.

**Nuance not previously stated: surcharge's own severity is not uniform either.** Ordinary
`SQLCODE OTHER` failures within the 16-branch cascade set `A9G36-SURCHARGE-FAILURE` and return
normally (soft, per §4's table this is the same family as #145's leniency). But surcharge's own
buy-group priority-array overflow (**#142**, §4) — reached from within the *same* surcharge
subsystem — **is** fatal and **does** use the standard `OMGPR-Q-ERROR-NBR`/`GO TO
0020-EXIT-PRICER` path. In other words, surcharge has its own internal hard/soft split, layered
on top of being itself an exception to the rest of the pricer's hard-by-default convention. Three
distinct severity behaviors coexist in this one subsystem: (a) most of the pricer — hard by
default; (b) surcharge's ordinary SQL failures — soft, siloed from `OMGPR-Q-ERROR-NBR`; (c)
surcharge's own array-overflow — hard, using the standard mechanism.

---

## 6. CICS errors

**CONFIRMED, carried forward from `program-inventory.md` §6.5 and extended here with explicit
stop/continue behavior.** `A6U01` itself issues no `EXEC CICS LINK` (it has no equivalent
category of error). The identical 3-way pattern appears in `A6X01`, `A6O010U`, `A6O011U`,
`A6O012U`, `A6O013U` (every DAO-tier program in this call chain except the innermost,
`A6O016U`, which is the bottom of the chain and issues no `LINK` of its own):

| `WS-XCTL-CICS-RESP` value | Meaning | Message | Behavior |
|---|---|---|---|
| `DFHRESP-NORMAL` | Link succeeded | none | CONTINUE |
| `DFHRESP-PGMIDERR` | Target program not installed/available in CICS | `'<program> TRANSACTION IS UNAVAILABLE'` | STOP (propagates as a fatal error to that program's own caller — each program's `A6Rnn`-family response-flag structure, §7, carries this back up) |
| `OTHER` (any other RESP code) | Unclassified CICS failure | `'ERROR-UNABLE TO LINK TO <program>'` / `'CICS LINK ERR '` with the numeric RESP captured | STOP |

Since `A6U01` (the pricing engine itself, the subject of every other document in this repository)
does not participate in this pattern directly, a `CICS LINK` failure surfaces to `A6U01` only
indirectly — as whatever error the failing DAO-tier program's own `A6Rnn` response-flag structure
propagates back (§7).

---

## 7. Kit and component errors

**CONFIRMED, carried forward from `program-inventory.md` §6.2/§6.4 and extended with stop/
continue behavior.** These are the "kit" (multi-component order/pack processing, `A6O012U`/
`A6O013U`/`A6O016U`) and "component" (the shared `A6R10`/`A6R13`/`A6R16` response-flag
copybooks) error paths named in T006's scope, distinct from `A6U01`'s own `OMGPR-Q-ERROR-NBR`
space.

### 7.1 Per-program numbering spaces
- **`A6O016U`** (bottom of the DAO chain, never touches `OMGPR` directly): its own
  `A6W16-ERROR-MESSAGE` number space — `61601`/`61604` (vendor/product not provided, STOP),
  `61602` (SQL error, `VNG02`, STOP), `61603` (product not found, `VNG02` — CONTINUE or STOP
  depends on caller, since "not found" here is a data-absence result the caller may treat as
  non-fatal; not independently re-verified this pass), `61605` (category not found, `VNG06`),
  `61606` (**reused for three different failures** — SQL error on `VNG06`, SQL error on `ING01`,
  and "UOM not found" on `VNG05` — already flagged in `program-inventory.md` §1.6 as a
  code-reuse ambiguity), `61607` (SQL error, `VNG05`).
- **`A6O013U`**: `61301`/`61304` (vendor/product not provided, STOP) — same `6<transid>nn`
  numbering convention as `A6O010U`'s `61001`/`61004`.
- **`A6O012U`**: its own `OMGEXPL-ERROR-NUMBER` space — `61201` (product# not provided), `61202`
  (effective date invalid), `61203` (request type invalid), `61204` (rollup-cost switch invalid),
  `61210` (SQL error fetching current date). All STOP per `program-inventory.md` §1.8's
  characterization of this program's error handling as abend-on-any-failure.
- **`A6U01` NDP path** (`A6P001WB` web-service call): whatever numeric value the called program
  returns in `A6W001-WEB-ERROR-NBR` passes straight through to `OMGPR-Q-ERROR-NBR`, EXCEPT `404`
  (treated as "not found, not fatal" — CONTINUE) and `0`/`ZEROES` (success) — every other value is
  STOP. The actual catalog of NDP-specific codes is BLOCKED (owned by the unsupplied `A6P001WB`).

### 7.2 The shared component response-flag pattern (`A6R10`/`A6R13`/`A6R16`)
**CONFIRMED, identical shape in all three copybooks:** an 88-level `GENERAL-RESPONSE-FLAG` with
three severities (`'A'`=`-ABEND`, `'E'`=`-ERROR`, `'W'`=`-WARNING`), a 5-character
`GENERAL-ERROR-NUMBER`, and an 85-character `ABEND-MSG` (80-char message + 5-char SQLCODE). Used
identically by `A6O010U` (as `A6W10`/`A6W16`), `A6O013U` (as `A6W13`, propagated from `A6W16` in
its own `0020-PROCESS`), and `A6O016U` (`A6W16`, the program that sets these flags in the first
place — everything upstream just relays them).
- **`'A'` (ABEND):** STOP — this is the component-layer equivalent of `A6U01`'s
  `OMGPR-F-PRICER-ERROR='Y'`/`GO TO 0020-EXIT-PRICER`, propagated up through however many `LINK`
  levels separate the failing component from its ultimate caller.
- **`'E'` (ERROR):** INFERRED to be STOP as well for most callers (not independently re-verified
  for every consuming paragraph this pass) — the distinction between `'A'` and `'E'` severity was
  not traced to a behavioral difference in how any calling paragraph reacts differently to one
  versus the other; both were observed leading to the caller's own fatal path in the sites spot-
  checked. Flagged as INFERRED rather than CONFIRMED since this wasn't exhaustively verified
  across every consumer.
- **`'W'` (WARNING):** INFERRED to be CONTINUE, by the label's plain meaning — not independently
  confirmed against an actual consuming site that reacts differently to `'W'` than to `'A'`/`'E'`.
- **CONFIRMED inconsistency (already flagged in `program-inventory.md` §6.4):** `A6O011U` does
  not consume this flag pattern at all — its own `9998-BUILD-ERROR-MSG` reads
  `A6W13-ERROR-SQLCODE`/`A6W13-ERROR-MESSAGE` directly rather than branching on the abend/error
  88-levels, even though it has an `A6W13` structure available. Not corrected, preserved as an
  observed inconsistency.

---

## What was scoped out of this pass

- **Individual formula-level tracing of every one of the 183 `A6U01` codes' exact preconditions**
  — the appendix table gives paragraph and message for all of them; deep per-code business
  context exists only for the ones already covered by `docs/rules/*.md` (cross-referenced in §3)
  or individually written up in §2/§4/§5.
- **`A6O016U`'s `61603`/similar "not found vs. fatal" ambiguity** — not independently resolved,
  flagged as-is.
- **Exhaustive verification of `'E'`/`'W'` severity behavior** across every consuming site of the
  `A6R10`/`A6R13`/`A6R16` pattern (§7.2) — spot-checked only.
- **`A6P001WB`'s own NDP error-code catalog** — BLOCKED, program not supplied.

## Assumptions

1. `'E'` (ERROR) severity in the `A6R10`/`A6R13`/`A6R16` pattern behaves the same as `'A'`
   (ABEND) for practical purposes (both fatal to the caller); `'W'` (WARNING) is assumed
   non-fatal by its label. Neither is exhaustively confirmed (§7.2).
2. `A6O016U`'s `61603` (product not found) is assumed to be a normal "no match" result a caller
   can treat as non-fatal, by analogy to how not-found conditions are handled elsewhere in this
   codebase — not independently confirmed for this specific code.

## Blockers

1. `A6P001WB`'s NDP error-code catalog — not supplied.
2. Whether every one of the dozens of `7695-ADD-EXP-DATE-ARRA-010` callers (§4, #145) escalates
   the soft-fail — same standing blocker already recorded in `date-selection.md`.
3. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set;
   this catalog's stop/continue classifications are derived from static control-flow analysis
   (presence or absence of `GO TO 0020-EXIT-PRICER`), not from observing actual runtime behavior.

---

## Appendix: full mechanical reference table (183 codes, `A6U01.CBL`)

Generated by scanning every `OMGPR-Q-ERROR-NBR`/`WS-DB-ERROR-NBR` assignment site in
`A6U01.CBL` and checking for a following `GO TO 0020-EXIT-PRICER` within 15 lines. "Message" is
populated only where a hand-authored message was found near the site (see §2); blank means the
generic templated SQL-failure text from §1.2 applies. Business context for each code, where it
exists beyond this table, is in the `docs/rules/*.md` document named in §3's groupings.

| OMGPR-Q-ERROR-NBR / WS-DB-ERROR-NBR | Paragraph(s) | Message (where distinct from generic SQL text) | Behavior |
|---|---|---|---|
| 1 | 7325-CHK-FOR-MEDC-OVERRIDES, 7335-CHK-FOR-VEND-OVERRIDES | (generic SELECT/DB ERROR message) | STOP |
| 2 | 7325-CHK-FOR-MEDC-OVERRIDES, 7330-CHK-FOR-PCAT-OVERRIDES, 7335-CHK-FOR-VEND-OVERRIDES | (generic SELECT/DB ERROR message) | STOP |
| 3 | 7325-CHK-FOR-MEDC-OVERRIDES, 7335-CHK-FOR-VEND-OVERRIDES, 7745-SEL-ACCT-FIN-CHRG-010 | (generic SELECT/DB ERROR message) | STOP |
| 4 | 7325-CHK-FOR-MEDC-OVERRIDES, 7330-CHK-FOR-PCAT-OVERRIDES, 7335-CHK-FOR-VEND-OVERRIDES, 7760-SEL-ACCT-OVERHD-010 | (generic SELECT/DB ERROR message) | STOP |
| 5 | 0310-SEL-ACCT-PRD-CAT-010, 0340-PRO-CUG13-010 | (generic SELECT/DB ERROR message) | STOP |
| 6 | 7755-SEL-ACCT-PREPAY-D-010 | (generic SELECT/DB ERROR message) | STOP |
| 7 | 7855-SEL-ACCT-PRI-VER-010 | (generic SELECT/DB ERROR message) | STOP |
| 8 | 7730-SEL-ACCT-SELL-ARR-010 | (generic SELECT/DB ERROR message) | STOP |
| 9 | 0345-PRO-CUG12-010 | (generic SELECT/DB ERROR message) | STOP |
| 12 | 0315-SEL-ALL-ACCT-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 13 | 0315-SEL-ALL-ACCT-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 14 | 0315-SEL-ALL-ACCT-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 15 | 0325-SEL-ALL-CUST-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 16 | 0325-SEL-ALL-CUST-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 17 | 0325-SEL-ALL-CUST-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 18 | 7750-SEL-CUST-NON-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 19 | 0350-PRO-CUG09-010 | (generic SELECT/DB ERROR message) | STOP |
| 20 | 7905-SEL-CUST-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 21 | 7865-SEL-CUST-PRI-VER-010 | (generic SELECT/DB ERROR message) | STOP |
| 22 | 0355-PRO-CUG08-010 | (generic SELECT/DB ERROR message) | STOP |
| 23 | 7060-SEL-DIV-INV-010 | (generic SELECT/DB ERROR message) | STOP |
| 24 | 7940-SEL-ELIG-MEMBER-010 | (generic SELECT/DB ERROR message) | STOP |
| 25 | 7315-SEL-GRP-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 26 | 7315-SEL-GRP-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 27 | 0360-SEL-GRP-CNT-MIN-010 | (generic SELECT/DB ERROR message) | STOP |
| 28 | 0360-SEL-GRP-CNT-MIN-010 | (generic SELECT/DB ERROR message) | STOP |
| 29 | 0360-SEL-GRP-CNT-MIN-010 | (generic SELECT/DB ERROR message) | STOP |
| 32 | 7580-SEL-GRP-SELL-ARR-010 | (generic SELECT/DB ERROR message) | STOP |
| 33 | 7733-FND-CUST-BASE-SELL-ARR | (generic SELECT/DB ERROR message) | STOP |
| 36 | 7280-SEL-IN-FRT-BG-VEN-010, 7915-PRO-DEF-BG-FRT-010 | (generic SELECT/DB ERROR message) | STOP |
| 37 | 0235-SEL-INDV-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 38 | 0235-SEL-INDV-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 39 | 7805-SEL-INV-CLS-ITEM-010 | (generic SELECT/DB ERROR message) | STOP |
| 41 | 0270-SEL-MIN-INDV-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 42 |  | (generic SELECT/DB ERROR message) | STOP |
| 43 |  | (generic SELECT/DB ERROR message) | STOP |
| 44 | 7321-SEL-PARENT-COST-01-010 | (generic SELECT/DB ERROR message) | STOP |
| 45 | 7105-SEL-PRC-LST-010 | (generic SELECT/DB ERROR message) | STOP |
| 46 | 7860-SEL-PRIMARY-ACCT-010 | (generic SELECT/DB ERROR message) | STOP |
| 47 | 7870-SEL-PRIMARY-CUST-010 | (generic SELECT/DB ERROR message) | STOP |
| 48 | 7775-SEL-PROD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 49 | 7125-SEL-SELL-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 50 | 7115-SEL-SELL-DFLT-010 | (generic SELECT/DB ERROR message) | STOP |
| 51 | 7130-SEL-SELL-PRODUCT-010 | (generic SELECT/DB ERROR message) | STOP |
| 52 | 7135-SEL-SELL-VEND-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 53 | 7120-SEL-SELL-VENDOR-010 | (generic SELECT/DB ERROR message) | STOP |
| 54 | 7800-SEL-DELIVERY-010 | (generic SELECT/DB ERROR message) | STOP |
| 55 | 7700-SEL-SPEC-ITEM-010 | (generic SELECT/DB ERROR message) | STOP |
| 56 | 7780-SEL-VEND-PROD-010 | (generic SELECT/DB ERROR message) | STOP |
| 57 | 7790-SEL-VENDOR-010 | (generic SELECT/DB ERROR message) | STOP |
| 58 | 0275-PRO-ACCT-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 59 | 7740-SEL-CUST-RISK-PRE-010 | (generic SELECT/DB ERROR message) | STOP |
| 60 | 7785-SEL-DIV-VEND-010 | (generic SELECT/DB ERROR message) | STOP |
| 61 | 7220-SEL-INV-CLS-CUST-010 | (generic SELECT/DB ERROR message) | STOP |
| 62 | 7170-SEL-SPEC-VER-010 | (generic SELECT/DB ERROR message) | STOP |
| 63 | 0135-SEL-SUGG-BROKERAG-010 | (generic SELECT/DB ERROR message) | STOP |
| 64 | 0140-SEL-SUGG-COST-PLU-010 | (generic SELECT/DB ERROR message) | STOP |
| 65 | 0145-SEL-SUGG-STATED-010 | (generic SELECT/DB ERROR message) | STOP |
| 66 | 0150-SEL-SUGG-COST-DIS-010 | (generic SELECT/DB ERROR message) | STOP |
| 67 | 0275-PRO-ACCT-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 68 | 0275-PRO-ACCT-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 70 | 7660-SEL-CONT-REF-GROS-010 | (generic SELECT/DB ERROR message) | STOP |
| 71 | 7665-SEL-CONT-REF-COST-010 | (generic SELECT/DB ERROR message) | STOP |
| 72 | 7670-SEL-CONT-REF-VEND-010 | (generic SELECT/DB ERROR message) | STOP |
| 73 | 7675-SEL-CONT-REF-SUGG-010 | (generic SELECT/DB ERROR message) | STOP |
| 74 | 7640-SEL-PROD-GROSS-MA-010 | (generic SELECT/DB ERROR message) | STOP |
| 75 | 7645-SEL-PROD-COST-PLU-010 | (generic SELECT/DB ERROR message) | STOP |
| 76 | 7650-SEL-PROD-VEND-PUB-010 | (generic SELECT/DB ERROR message) | STOP |
| 77 | 7655-SEL-PROD-STATED-P-010 | (generic SELECT/DB ERROR message) | STOP |
| 78 | 7625-SEL-PROD-CAT-GROS-010 | (generic SELECT/DB ERROR message) | STOP |
| 79 | 7630-SEL-PROD-CAT-COST-010 | (generic SELECT/DB ERROR message) | STOP |
| 80 | 7635-SEL-PROD-CAT-VEND-010 | (generic SELECT/DB ERROR message) | STOP |
| 81 | 7610-SEL-VENDOR-GROSS-010 | (generic SELECT/DB ERROR message) | STOP |
| 82 | 7615-SEL-VENDOR-COST-P-010 | (generic SELECT/DB ERROR message) | STOP |
| 83 | 7620-SEL-VENDOR-VEND-P-010 | (generic SELECT/DB ERROR message) | STOP |
| 84 | 7600-SEL-DFLT-GROSS-MA-010 | (generic SELECT/DB ERROR message) | STOP |
| 85 | 7605-SEL-DFLT-COST-PLU-010 | (generic SELECT/DB ERROR message) | STOP |
| 86 | 7595-CVT-UM-010, 7875-FIND-ALT-UOM-DESIGNATOR | (generic SELECT/DB ERROR message) | STOP |
| 87 | 7595-CVT-UM-010 | (generic SELECT/DB ERROR message) | STOP |
| 88 | 7945-SEL-GRP-CNT-NBR-010 | (generic SELECT/DB ERROR message) | STOP |
| 89 | 7705-SEL-PANDAC-SPECIF-010 | (generic SELECT/DB ERROR message) | STOP |
| 90 | 7735-SEL-BG-SHORT-NAME-010 | (generic SELECT/DB ERROR message) | STOP |
| 92 | 0200-SEL-DELIVERY-010 | (generic SELECT/DB ERROR message) | STOP |
| 93 | 0172-PROCESS-FOR-DEFUALT-SHIP, 7706-SEL-PANDAC-SPECIF-NEW | (generic SELECT/DB ERROR message) | STOP |
| 94 | 0155-SEL-SUGG-FIXED-RE-010 | (generic SELECT/DB ERROR message) | STOP |
| 95 | 7595-CVT-UM-010 | #95 - ALTERNATE UNIT OF MEASURE IS  | STOP |
| 96 | 7595-CVT-UM-010 | NOT FOUND IN A6X01 | STOP |
| 97 | 0275-PRO-ACCT-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 98 | 0280-PRO-CUST-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 99 | 0280-PRO-CUST-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 100 | 0280-PRO-CUST-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 101 | 0280-PRO-CUST-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 102 | 7185-PRO-PASSED-DATA-010 | #102 - MUST PASS DIVISION NUMBER TO A6X01; #135 - MUST PASS DATE TO A6 | STOP |
| 103 | 7185-PRO-PASSED-DATA-010 | #103 - MUST PASS ACCOUNT NUMBER TO A6X01 | STOP |
| 104 | 7185-PRO-PASSED-DATA-010 | #104 - MUST PASS VENDOR NUMBER TO A6X01 | STOP |
| 105 | 7185-PRO-PASSED-DATA-010 | #105 - MUST PASS PRODUCT NUMBER TO A6X01 | STOP |
| 107 | 7900-SEL-ACCT-PRI-010 | (generic SELECT/DB ERROR message) | STOP |
| 109 | 7810-SEL-BG-FLAGS-010 | (generic SELECT/DB ERROR message) | STOP |
| 110 | 7770-SEL-BGM-WITH-NBR-010 | (generic SELECT/DB ERROR message) | STOP |
| 111 | 7590-SEL-BGM-WITHOUT-N-010 | (generic SELECT/DB ERROR message) | STOP |
| 112 | 7122-SEL-SELL-SSC-010 | #112 - PRICING METHOD IS INVALID FOR  | STOP |
| 113 | 7585-SEL-PARENT-SELL-010, 7925-FND-BG-FATHER-010 | (generic SELECT/DB ERROR message) | STOP |
| 115 | 7125-SEL-SELL-CAT-010 | #115 - PRICING METHOD IS INVALID FOR  | STOP |
| 116 | 7115-SEL-SELL-DFLT-010 | #116 - PRICING METHOD IS INVALID FOR  | STOP |
| 117 | 7130-SEL-SELL-PRODUCT-010 | #117 - PRICING METHOD IS INVALID FOR  | STOP |
| 118 | 7135-SEL-SELL-VEND-CNT-010 | #118 - PRICING METHOD IS INVALID FOR  | STOP |
| 119 | 7120-SEL-SELL-VENDOR-010 | #119 - PRICING METHOD IS INVALID FOR  | STOP |
| 121 | 7780-SEL-VEND-PROD-010 | #121 - VENDOR PRODUCT NOT FOUND IN A6X01 | STOP |
| 122 | 7790-SEL-VENDOR-010 | #122 - VENDOR NOT FOUND IN A6X01 | STOP |
| 123 | 7765-SEL-PRC-LOCK-010 | (generic SELECT/DB ERROR message) | STOP |
| 124 | 7810-SEL-BG-FLAGS-010 | #124 - BUY GROUP PRICE PARMS  | STOP |
| 125 | 7145-SEL-CUST-PRNT-SEL-010 | (generic SELECT/DB ERROR message) | STOP |
| 126 | 7140-SEL-ACCT-PRNT-SEL-010 | (generic SELECT/DB ERROR message) | STOP |
| 127 | 7680-GET-TIER-START-010 | (generic SELECT/DB ERROR message) | STOP |
| 130 | 7575-SEL-MAX-PRCLST-010 | #130 - MAX PRICE LIST NOT FOUND; #130 -100 MAX PRICE LIST NOT FOUND | STOP |
| 131 | 7575-SEL-MAX-PRCLST-010 | (generic SELECT/DB ERROR message) | STOP |
| 132 | 7575-SEL-MAX-PRCLST-010 | #132 +1 MAX PRICE LIST NOT FOUND; #132 - MAX PRICE LIST NOT FOUND | STOP |
| 133 | 7575-SEL-MAX-PRCLST-010 | (generic SELECT/DB ERROR message) | STOP |
| 135 | 0205-PRO-FREIGHT-COST-010, 7820-PRO-CORP-FRT-010 | FLG IN BG PRICE PARAMETER NOT CORRECT | STOP |
| 136 | 7895-PRO-DVN-VEND-FRT-010 | (generic SELECT/DB ERROR message) | STOP |
| 139 | 7285-GET-ACT-BG-PRI-LI-010 | (generic SELECT/DB ERROR message) | STOP |
| 140 | 7285-GET-ACT-BG-PRI-LI-010, 7300-GET-CUST-BG-PRI-L-010 | (generic SELECT/DB ERROR message) | STOP |
| 141 | 7285-GET-ACT-BG-PRI-LI-010 | (generic SELECT/DB ERROR message) | STOP |
| 142 | 0288-ACT-BG-PROD-CATE-SURCHG, 0290-PRO-ACT-BG-SURCHG-010, 0292-CUST-BG-PROD-CATE-SURCHG, 0295-CUST-BG-SURCHG-010... | ERR #142 ; NO OF PRIIORITIES EXCEEDED MAX LIMIT; ERR #142 ; NO OF PRIO | STOP |
| 143 | 7300-GET-CUST-BG-PRI-L-010 | (generic SELECT/DB ERROR message) | STOP |
| 144 | 7300-GET-CUST-BG-PRI-L-010 | (generic SELECT/DB ERROR message) | STOP |
| 145 | 7695-ADD-EXP-DATE-ARRA-010 | ERR#145 NO OF EXPIRATION DATES EXCEEDS LIMIT | CONTINUE |
| 148 | 7685-FIND-CORP-IACT-010 | (generic SELECT/DB ERROR message) | STOP |
| 149 | 7690-SEL-CORP-SELL-010 | (generic SELECT/DB ERROR message) | STOP |
| 150 | 0310-SEL-ACCT-PRD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 151 | 0310-SEL-ACCT-PRD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 152 | 0320-SEL-ACCT-VEND-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 153 | 0320-SEL-ACCT-VEND-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 154 | 0320-SEL-ACCT-VEND-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 155 | 0330-SEL-CUST-PRD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 156 | 0330-SEL-CUST-PRD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 157 | 0330-SEL-CUST-PRD-CAT-010 | (generic SELECT/DB ERROR message) | STOP |
| 158 | 0335-CUST-VEND-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 159 | 0335-CUST-VEND-BG-010 | (generic SELECT/DB ERROR message) | STOP |
| 160 | 9650-PRO-ACT-VEND-FREIGHT-CHG | (generic SELECT/DB ERROR message) | STOP |
| 162 | 9655-SELECT-CONT-EXCL | (generic SELECT/DB ERROR message) | STOP |
| 200 | 7777-CHK-NON-OM-SLCT-VEND-FLAG | (generic SELECT/DB ERROR message) | STOP |
| 201 | 9810-CHECK-NDP-ON-OFF-SWITCH | (generic SELECT/DB ERROR message) | STOP |
| 202 | 7742-SEL-CID-RISK-PRE-010 | (generic SELECT/DB ERROR message) | STOP |
| 203 | 0202-SEL-DELIVERY-CID-010 | (generic SELECT/DB ERROR message) | STOP |
| 204 | 7747-SEL-CID-FIN-CHRG-010 | (generic SELECT/DB ERROR message) | STOP |
| 205 | 7757-SEL-CID-PREPAY-D-010 | (generic SELECT/DB ERROR message) | STOP |
| 206 | 7762-SEL-CID-OVERHD-010 | (generic SELECT/DB ERROR message) | STOP |
| 207 | 7328-CHK-FOR-MEDC-OVERRIDES-CD | (generic SELECT/DB ERROR message) | STOP |
| 208 | 7328-CHK-FOR-MEDC-OVERRIDES-CD | (generic SELECT/DB ERROR message) | STOP |
| 209 | 7332-CHK-FOR-PCAT-OVERRIDES-CD | (generic SELECT/DB ERROR message) | STOP |
| 210 | 7337-CHK-FOR-VEND-OVERRIDES-CD | (generic SELECT/DB ERROR message) | STOP |
| 211 | 7337-CHK-FOR-VEND-OVERRIDES-CD | (generic SELECT/DB ERROR message) | STOP |
| 212 | 9652-PRO-CID-VEND-FREIGHT-CHG | (generic SELECT/DB ERROR message) | STOP |
| 213 | 7752-SEL-CUST-NON-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 301 | 7885-FIND-VEND-EXCL-010 | (generic SELECT/DB ERROR message) | STOP |
| 601 | 0235-SEL-INDV-CNT-010 | #601-SPECIAL CONTRACT NOTFOUND FOR  | STOP |
| 602 | 7090-PRO-SELL-AMTS-010 | INVALID WS-COST-CONTRACT  | STOP |
| 603 | 7090-PRO-SELL-AMTS-010 | INVALID WS-COST-CONTRACT  | STOP |
| 701 |  | (generic SELECT/DB ERROR message) | STOP |
| 702 |  | (generic SELECT/DB ERROR message) | STOP |
| 703 |  | VNG05 | STOP |
| 707 | 7707-CHECK-PANDAC-ACCT-FLAG | (generic SELECT/DB ERROR message) | STOP |
| 888 | 9037-SQL-ROW-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 889 | 9643-SQL-ROW-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 890 | 9283-SQL-ROW-CNT-010 | (generic SELECT/DB ERROR message) | STOP |
| 900 | 9660-SELECT-AS-OF-PROD | (generic SELECT/DB ERROR message) | STOP |
| 901 | 9665-SELECT-AS-OF-VEND | (generic SELECT/DB ERROR message) | STOP |
| 902 | 9670-SELECT-AS-OF-GRP | (generic SELECT/DB ERROR message) | STOP |
| 903 | 0217-CHK-PROD-CATE-DEFLT-VEND, 9675-SELECT-AS-OF-COST, 9680-SQL-SELECT, 9685-SQL-SELECT... | (generic SELECT/DB ERROR message) | STOP |
| 904 | 9676-SELECT-AS-OF-COST | (generic SELECT/DB ERROR message) | STOP |
| 924 | 9910-SQL-SELECT-010 | (generic SELECT/DB ERROR message) | STOP |
| 945 | 9945-CHECK-HC-COST-ACCOUNT | (generic SELECT/DB ERROR message) | STOP |
| 946 | 9950-CHECK-HC-COST-CID | (generic SELECT/DB ERROR message) | STOP |
| 947 | 9960-CHECK-HC-SELL-ACCOUNT | (generic SELECT/DB ERROR message) | STOP |
| 948 | 9965-CHECK-HC-SELL-CID | (generic SELECT/DB ERROR message) | STOP |
| 949 | 9970-SELECT-HC-COST-VNG03 | (generic SELECT/DB ERROR message) | STOP |
| 950 | 7122-SEL-SELL-SSC-010 | (generic SELECT/DB ERROR message) | STOP |
| 951 | 7970-SEL-SSC-GROS-010 | (generic SELECT/DB ERROR message) | STOP |
| 952 | 7975-SEL-SSC-COST-010 | (generic SELECT/DB ERROR message) | STOP |
| 953 | 7980-SEL-SSC-VEND-010 | (generic SELECT/DB ERROR message) | STOP |
| 1025 | 7795-FIND-WHICH-JOIN-010 | (generic SELECT/DB ERROR message) | STOP |
| 1026 | 7795-FIND-WHICH-JOIN-010 | (generic SELECT/DB ERROR message) | STOP |
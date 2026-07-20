# OMGEXPL Data Dictionary (T004)

**Scope:** Field-by-field mapping of the `OMGEXPL` copybook (`upload/OMGEXPL.CPY`, 63 lines), the
COMMAREA structure used to transfer kit/pack-explosion requests and results between any calling
process and `A6O012U` (the online kit-explosion orchestrator). Companion CSV:
`omgexpl-data-dictionary.csv` (same fields, offsets, and notes in tabular form).

**Source of truth:** COBOL as read directly in `upload/OMGEXPL.CPY` and `upload/A6O012U.CBL`
(both read in full — 63 and 525 lines respectively, small enough for complete coverage rather
than sampling).

**Byte-length reconciliation (CONFIRMED, exact match — contrast with `OMGPR`'s ambiguous
1,773–1,789-byte gap in `docs/mappings/omgpr-data-dictionary.md`):** `A6O012U`'s
`DFHCOMMAREA` is declared `PIC X(23367)` (line 129). Every field in `OMGEXPL-C` is `DISPLAY`
(no `COMP`/`COMP-3` usage clause anywhere in the copybook, so there is no packed-decimal sizing
ambiguity to reconcile). Summing every field's byte length — 56 bytes of input fields, 120 bytes
of output-header fields, `33 bytes/occurrence × 700 = 23,100 bytes` for the `OMGEXPL-COMP-INFO`
array, and 91 bytes of error fields — totals **exactly 23,367 bytes**, matching the declared
COMMAREA size with zero slack. Independently re-verified by script (not just hand arithmetic).

**Confidence key:** CONFIRMED (directly read/mechanically derived from source), INFERRED
(reasonable but unverified assumption), BLOCKED (cannot be determined from supplied files).

---

## Structure

`OMGEXPL-C` (01-level, all fields DISPLAY) divides into four groups, in physical order:

1. **`OMGEXPL-INPUT-FIELDS`** (56 bytes) — the request: which pack to explode, as-of date,
   request type, rollup switch.
2. **`OMGEXPL-PRC-OUTPUT-FIELDS`** header portion (120 bytes) — pack-level attributes (product
   type, base UOM, overhead/third-party fee amounts and their date windows) and the item count.
3. **`OMGEXPL-COMP-INFO` array** (23,100 bytes = 33 bytes × 700 occurrences) — the actual
   exploded component/sub-pack rows, the real payload of this structure.
4. **Error fields** (91 bytes) — response flag, error number, message, captured SQLCODE.

## Field notes beyond what the CSV already tabulates

- **`OMGEXPL-REQUEST-TYPE`** is the single field that determines the *shape* of the entire
  response — see `docs/cobol-analysis/kit-processing.md`'s `R-KIT-002` for the full three-way
  dispatch (Structure / Level-1-only / Components-only) this field drives.
- **`OMGEXPL-COMP-QTY`**, for level-2 (nested) components, is **not a pass-through** of whatever
  `A6O015U` returned — `A6O012U` itself recomputes it by multiplying the component's own quantity
  by its parent sub-pack's quantity within the pack (`docs/cobol-analysis/kit-processing.md`'s
  `R-KIT-003`). This is the one field in the whole structure where this program contributes real
  arithmetic rather than just relaying `A6O015U`'s output.
- **`OMGEXPL-NUMBER-OF-ITEMS`** is likewise overwritten by `A6O012U` (with the actual count of
  rows it wrote to `OMGEXPL-COMP-INFO` this call) rather than left as `A6O015U`'s raw
  `OMGPK-NUMBER-OF-ITEMS` value — worth knowing since the two counts could in principle differ if
  the 700-row cap is ever reached (see `kit-processing.md`'s Capacity section for the confirmed
  absence of an overflow error in that case).
- **Three fields are declared but never referenced anywhere in `A6O012U.CBL`'s own procedural
  logic** — `OMGEXPL-T-NEXT-PRODUCT-NO`, `OMGEXPL-PACK-STATUS`, `OMGEXPL-NEXT-SUBPACK-PROD-NO`.
  They may be populated directly by `A6O015U` into the shared COMMAREA before `A6O012U` ever reads
  `OMGPK` back out, or may be entirely vestigial — BLOCKED, since `A6O015U`'s own source is not
  supplied.

## `OMGPK` — the paired parameter-list structure (BLOCKED as a formal copybook, INFERRED by usage)

`A6O012U` also `COPY OMGPK`s a second structure (`A6W015U-PARM-LIST`, line 117) that it populates
before the `EXEC CICS LINK` to `A6O015U` and reads back after. **`OMGPK.CPY` itself is not
supplied in `upload/`** — every `OMGPK-*` field cited in this document (and in
`kit-processing.md`) is known only by name and by how `A6O012U` uses it, never by its own `PIC`
clause. The following fields are CONFIRMED to exist (referenced by name in `A6O012U.CBL`) with
INFERRED shapes based on their `OMGEXPL` counterparts (same names, presumably same or similar
`PIC` clauses, since `A6O012U` moves most of them straight across):

`OMGPK-PACK-FULL-PRODNO`, `OMGPK-INP-EFF-DATE`, `OMGPK-ROLLUP-COST-SW`, `OMGPK-PRODUCT-TYPE`,
`OMGPK-CUSTOM-SOURCE`, `OMGPK-BASE-UOM`, `OMGPK-OH-FEE`, `OMGPK-OH-EFF-DATE`,
`OMGPK-OH-EXP-DATE`, `OMGPK-TP-FEE-COST`, `OMGPK-TP-COST-EFF-DATE`, `OMGPK-TP-COST-EXP-DATE`,
`OMGPK-TP-FEE-SELL`, `OMGPK-TP-SELL-EFF-DATE`, `OMGPK-TP-SELL-EXP-DATE`,
`OMGPK-NUMBER-OF-ITEMS`, `OMGPK-COMPLETE-EXPLODE-SW`, and the repeating
`OMGPK-COMP-PROD-TYPE(SUB)` / `OMGPK-COMP-PROD-NO(SUB)` / `OMGPK-SUB-PACK-NUM(SUB)` /
`OMGPK-COMP-UOM(SUB)` / `OMGPK-COMP-QTY(SUB)` (indexed 1 to `OMGPK-NUMBER-OF-ITEMS`, bounded at
`WS-MAX-ITEMS-IN-PACK`=700 by the caller's own loop, not by any confirmed bound inside `OMGPK`
itself).

**Two important differences from `OMGEXPL`'s domain, confirmed by usage even without the
copybook:**
- `OMGPK-COMP-PROD-TYPE(SUB)` uses a **three-value domain — `'S'`, `'1'`, `'2'`** — not the
  two-value `'C'`/`'S'` domain `OMGEXPL-COMP-PROD-TYPE` ends up with. `A6O012U` re-classifies:
  `'S'` (sub-pack) stays `'S'`-equivalent conceptually but is tracked separately in
  `WS-SUB-PACK-ARRAY` rather than written to `OMGEXPL-COMP-INFO` directly in most modes; `'1'` and
  `'2'` (component at level 1 and level 2 respectively) both become `OMGEXPL-COMP-PROD-TYPE='C'`
  with `OMGEXPL-LEVEL` set to the original `'1'`/`'2'` value. **`OMGPK`'s "level" and "type" are
  the same field; `OMGEXPL`'s are two separate fields** — a detail easy to lose in translation.
- `OMGPK-SUB-PACK-NUM(SUB)`, for a level-2 component row, is **not a product number** — it is
  looked up against `WS-SUB-PACK-SL-NO` (a "slot/sequence number" populated when sub-pack rows
  were encountered) via `4000-FIND-PARENT-PROD`, not compared directly. See
  `kit-processing.md`'s `R-KIT-003` for the full mechanism.

## What was scoped out of this pass

- `OMGPK.CPY`'s actual field-level `PIC` clauses, full field list, and byte layout — BLOCKED,
  not supplied. Everything stated about `OMGPK` above is INFERRED from usage in `A6O012U.CBL`,
  not read from a copybook.
- `A6O015U`'s own logic (how it actually performs the explosion, resolves cost/OH/TP-fee
  rollup when `OMGPK-ROLLUP-COST-SW='Y'`, or determines `OMGPK-COMPLETE-EXPLODE-SW`) — BLOCKED,
  program not supplied.

## Blockers

1. `OMGPK.CPY` — not supplied; every field described here is INFERRED from `A6O012U.CBL`'s usage.
2. `A6O015U` — not supplied; the actual explosion engine and all cost-rollup arithmetic live here.
3. No DB2/CICS/COBOL execution environment — standing blocker across every document in this set.

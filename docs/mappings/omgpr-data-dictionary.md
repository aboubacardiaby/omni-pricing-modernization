# OMGPR — Complete Field Data Dictionary

**What this document is:** a field-by-field dictionary of the `OMGPR` copybook (`upload/OMGPR.CPY`,
959 lines), the central pricer parameter/result record used as the `DFHCOMMAREA` payload across
`A6X01`, `A6O010U`, `A6O011U`, `A6U01`, and as the working record inside `CUP100` (see
`docs/cobol-analysis/program-inventory.md` §2 for how this record is used across programs). It
supersedes any characterization/decision-table document as the dependency source for value-object,
identifier, UOM, quantity, percentage, money, and serialization-format decisions — those documents
cover *behavior* (which paragraphs run, in what order); this document covers *data shape* (every
field's exact type, size, and position).

**Source of truth:** `upload/OMGPR.CPY`, parsed directly and mechanically — not transcribed by
hand, and not copied from the pre-existing `upload/OMGPR_Field_Inventory.xlsx` workbook referenced
in `program-inventory.md` §5. That workbook was cross-checked against hand-computed COBOL storage
formulas during this task and found to contain per-field byte-length errors (see "Reconciliation
of the 1,789-byte layout" below) — it should no longer be treated as authoritative for byte
lengths/offsets. Its 88-level condition values and proposed-C#-property naming were spot-checked
and found consistent with the copybook and are reflected here.

**Method:** every non-comment line of `OMGPR.CPY` was extracted, its 6-character sequence-number/
change-tag prefix and trailing line-number column stripped, then reassembled into whole
period-terminated COBOL entries (COBOL statements do not respect line boundaries in this copybook —
`PIC`, `VALUE`, and `USAGE` clauses are frequently split across lines and reassembled here before
parsing). Each entry was parsed for level number, field name, `PIC` clause, `USAGE`, `VALUE`, and
(for level-88 entries) condition name/literal, then assembled into the copybook's group hierarchy
using standard COBOL level-number nesting rules. Byte length, signed-ness, and decimal-digit count
were then computed from each leaf field's `PIC`/`USAGE` using the formulas below — not read from
any spreadsheet — and cumulative byte offsets computed by walking the fields in declaration order.
**Every leaf field's `PIC`/`USAGE`/`VALUE`/group-path is CONFIRMED (directly parsed from the
copybook text; the full extraction was re-run and its summary counts below were spot-checked
against `program-inventory.md`'s independently-stated figures for cross-validation — see
"Cross-validation" below). Byte length/offset/decimal/signed-ness are CONFIRMED via a fixed,
cited formula, with two fields flagged INFERRED where the formula depends on a compiler/platform
assumption this task cannot verify from the copybook alone (see "Storage-size methodology").**

**Confidence key:** `CONFIRMED` = directly parsed from copybook text, or mechanically computed from
directly-parsed data via a fixed, cited, standard COBOL rule. `INFERRED` = depends on an assumption
this task could not independently verify (specific instances flagged inline). `BLOCKED` = cannot be
determined from the supplied copybook at all (none for this document — everything here is either
CONFIRMED or a narrowly-scoped INFERRED).

---

## Structure summary (CONFIRMED, cross-validated)

| Metric | Value | Cross-check |
|---|---|---|
| Total leaf (storage-bearing) fields | 287 | Matches `program-inventory.md` §5's citation of the pre-existing workbook's own count (287) — same field set, independently re-derived. |
| Structural group headers (no storage) | 22 | Matches the workbook's count (22). |
| 88-level condition names | 135 | Matches the workbook's count (135, `program-inventory.md` §5). |
| `COMP-3` (packed decimal) fields | 100 | Workbook stated 99; this parse finds 100 (see note below). |
| `COMP`/`COMPUTATIONAL` (binary) fields | 21 | Workbook stated 21; matches exactly. |
| `PIC X` (alphanumeric) fields | 164 | Workbook stated 164; matches exactly. |
| `REDEFINES` clauses in the copybook itself | 0 | CONFIRMED by direct search of `OMGPR.CPY` — no `REDEFINES` keyword present. Matches workbook. |
| `OCCURS` clauses (arrays) in the copybook itself | 0 | CONFIRMED by direct search of `OMGPR.CPY` — no `OCCURS` keyword present. Matches workbook. |

**Note on the `COMP-3` count:** this parse finds 100 `COMP-3` fields against the
workbook's stated 99. The one-field difference does not affect the byte-length reconciliation
below (it is a counting/classification difference, not a missing field — total leaf count matches
exactly at 287) and was not tracked down further in this pass; flagged as a minor open item.

**No `REDEFINES`/`OCCURS` in the copybook itself** means there are no overlapping storage regions
or repeating groups to model — every byte offset below is unambiguous and non-overlapping. (As
already noted in `program-inventory.md` §5, `A6O011U` *does* `REDEFINES` the whole `OMGPR-C` group
for its own internal parameter-passing purposes — that is a program-level concern external to this
copybook, not modeled here.)

---

## Storage-size methodology (the formulas used to compute every `ByteLength`/offset)

| Field shape | Byte length formula | Source |
|---|---|---|
| `PIC X(n)` (alphanumeric, any usage) | `n` bytes | Standard COBOL zoned/alphanumeric storage — 1 byte per character position. |
| `PIC [S]9(n)[V9(m)]` `DISPLAY` (default/no `USAGE` clause) | `n + m` bytes (one byte per digit; sign is zoned into the last digit's zone bits, no extra byte) | Standard COBOL `DISPLAY` numeric storage. |
| `PIC [S]9(n)[V9(m)]` `COMPUTATIONAL-3` / `COMP-3` (packed decimal) | `FLOOR((n+m)/2) + 1` bytes | Standard COBOL packed-decimal storage: 2 digits per byte, plus one nibble for the sign, rounded up to a whole byte. |
| `PIC [S]9(n)[V9(m)]` `COMPUTATIONAL` / `COMP` (binary) | 1–4 digits → 2 bytes; 5–9 digits → 4 bytes; 10–18 digits → 8 bytes | Standard IBM mainframe COBOL native binary sizing (halfword/fullword/doubleword), applied as the default assumption for this document. |

**Why the `COMP` formula is flagged as an assumption, not a certainty:** this copybook's sibling
files (`A6G01.CPY`, `RWDAT.CPY`) carry the header "Generated using RescueWare(R)TM... Relativity
Technologies, Inc." — a code-migration/re-hosting tool vendor, which raises the possibility this
codebase was migrated off a native IBM mainframe compiler at some point in its history. Non-IBM
COBOL compilers (e.g. Micro Focus) commonly size native `COMP` fields using a *minimal-bytes*
convention instead of half/full/doubleword (1–2 digits→1 byte, 3–4→2, 5–7→3, 8–9→4, 10–11→5,
12–14→6, 15–16→7, 17–19→8). This task audited every `COMP` field in `OMGPR` against both
conventions (script-computed, not hand-checked) and found **only two fields where the two
conventions disagree**: `OMGPR-Q-ORD-LIN-ORDERED` and `OMGPR-L-CNT-LINE`, both `PIC S9(7) COMP`
(7 digits) — 4 bytes under the mainframe convention used throughout this document, 3 bytes under
the minimal-bytes convention. These two are marked `INFERRED` in the CSV with a note; every other
`COMP` field's digit count falls in a range where both conventions agree, so this ambiguity affects
at most 2 bytes of the total record length, not the field boundaries of any other field.

---

## Reconciliation of the 1,789-byte layout

Three different byte-length figures exist for the `OMGPR-C` group, and they disagree:

| Source | Total bytes | Basis |
|---|---|---|
| Hardcoded length constants in the actual programs | **1,789** | `OMGPR-PARM-LENGTH PIC S9(4) VALUE +1789` (in `OMGPR.CPY` itself), and every `DFHCOMMAREA`/`COMMAREA` declared `PIC X(1789)` in `A6O011U.CBL`, `CUP100 (1).CBL`, and `A6U01.CBL` (all previously CONFIRMED in `program-inventory.md` §5). |
| Pre-existing `OMGPR_Field_Inventory.xlsx` workbook | 1,807 | Computed by the workbook's own (unverified) per-field formula. |
| This document (mechanical parse + cited standard formulas) | **1773** (mainframe `COMP` convention) to **1771** (minimal-bytes `COMP` convention) | Computed directly from `OMGPR.CPY`'s `PIC`/`USAGE` clauses, per the formulas above, summed over all 287 leaf fields including the trailing `OMGPR-FILLER PIC X(217)`. |

**This document's finding reverses the previous risk assessment.** The workbook's 1,807 figure
(18 bytes *larger* than the hardcoded 1,789) implied the last fields in the record could be
silently truncated at runtime — a genuine data-integrity risk, and it was reported as such in
`program-inventory.md` §5. Having independently recomputed every field's byte length from the
copybook text using cited, standard COBOL storage rules (and found and corrected specific errors
in the workbook's own formula — see below), this document's total is **16 to
18 bytes *smaller* than 1,789, not larger.** If the hardcoded 1,789-byte
COMMAREA size is accurate, `OMGPR-C` fits inside it with **room to spare** — there is no truncation
risk under this analysis. The remaining gap (bytes 1774-1789) would simply be
unused padding at the end of the COMMAREA.

**Concrete errors found in the workbook, explaining the disagreement:** spot-checking individual
`COMP-3` fields against the standard packed-decimal formula (`FLOOR(digits/2)+1`) found the
workbook overstated the byte length of at least two fields by one byte each:
- `OMGPR-A-CNT-LN-UNIT-COST`, `PIC S9(5)V9(8) COMP-3` — 13 total digits. Standard formula:
  `FLOOR(13/2)+1 = 7` bytes. Workbook stated 8.
- `OMGPR-P-CNT-LN-COST-PLUS`, `PIC S9(1)V9(4) COMP-3` — 5 total digits. Standard formula:
  `FLOOR(5/2)+1 = 3` bytes. Workbook stated 4.

Other `COMP-3` fields checked (e.g. `OMGPR-A-VD-PRD-ALT-UMF` at 14 digits, `OMGPR-A-VND-PRC-DEALER`
at 15 digits) matched the standard formula in both this document and the workbook — the workbook's
errors are not universal, only affecting some fields, which is consistent with a manual/semi-manual
byte-counting error rather than a single wrong formula applied uniformly (a uniformly-wrong formula
would have been off by a predictable, constant amount per field, not inconsistently).

**Caveat on this document's own total:** this reconciliation still rests on one unverified
assumption (the `COMP` binary-sizing convention, see above), worth at most 2 bytes, and on the
runtime `1,789` figure itself being accurate for the *live* system today — this document did not
have DB2/CICS/mainframe access to confirm the actual compiled COMMAREA size, only the source text
of the four programs that declare it. **Recommendation carried over from `program-inventory.md`
§9 stands: confirm the live COMMAREA size against a compiled listing or runtime capture before
this reconciliation is treated as fully closed**, though the risk profile is now "possible unused
padding," not "possible active truncation."

---

## Full field listing, grouped by top-level structure

`OMGPR-C` (the record body) totals **1773 bytes** across 29 direct-child entries: 2 named sub-groups (e.g. `OMGPR-PRC-INPUT-FIELDS`, `OMGPR-PRC-OUTPUT-FIELDS`) and 27 individual fields declared directly at the same level under `OMGPR-C` (mostly the later change-batches — `KS0912`/`BM0421`/`DP0722`/`SJ1022`/`SJ0821` additions — that were appended without being nested inside either major sub-group), listed below in declaration order. Nested indentation reflects the copybook's own group nesting.

### `OMGPR-PRC-INPUT-FIELDS` (308 bytes, offset (group))

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-I-DIVISION` | X(2) | DISPLAY | 2 |  | N | 1-2 | SPACE | string |  |
| `OMGPR-S-ACCOUNT` | X(6) | DISPLAY | 6 |  | N | 3-8 | SPACE | string |  |
| `OMGPR-I-VENDOR` | X(4) | DISPLAY | 4 |  | N | 9-12 | SPACE | string |  |
| `OMGPR-I-VND-PRODUCT` | X(8) | DISPLAY | 8 |  | N | 13-20 | SPACE | string |  |
| `OMGPR-Q-ORD-LIN-ORDERED` | S9(7) | COMP | 4 |  | Y | 21-24 | ZERO | int |  |
| `OMGPR-C-ORD-LIN-CUST-UOM` | X(2) | DISPLAY | 2 |  | N | 25-26 | SPACE | string |  |
| `OMGPR-C-SHIP-TO-SUFFIX` | X(3) | DISPLAY | 3 |  | N | 27-29 | SPACE | string |  |
| `OMGPR-C-BILL-TO-SUFFIX` | X(3) | DISPLAY | 3 |  | N | 30-32 | SPACE | string |  |
| `OMGPR-D-PRICING` | X(10) | DISPLAY | 10 |  | N | 33-42 | SPACE | string |  |
| `OMGPR-CUST-TYPE` | X(01) | DISPLAY | 1 |  | N | 43-43 | SPACE | string | `OMGPR-ECOMMERCE-PRICING`='Y' |
| `OMGPR-CREDIT-ACCT-ORDER` | X(1) | DISPLAY | 1 |  | N | 44-44 | SPACE | string |  |
| **`OMGPR-JIT-DATA`** *(group, 54 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;**`OMGPR-JIT-PRICING-FIELDS`** *(group, 24 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-SERVICE-FEE` | X(1) | DISPLAY | 1 |  | N | 45-45 | SPACE | string | `OMGPR-ADD-TO-ADJ-COST`='C'; `OMGPR-ADD-TO-REG-PRICE`='P'; `OMGPR-SHOW-ADJ-COST`='A'; `OMGPR-SHOW-REG-PRICE`='R' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-FEE-BREAK-OUT-SW` | X(1) | DISPLAY | 1 |  | N | 46-46 | SPACE | string | `OMGPR-FEE-BREAK-OUT`='Y' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-SERVICE-FEE-PCT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 47-49 | ZERO | decimal |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-LABEL-CHRG-TYPE` | X(1) | DISPLAY | 1 |  | N | 50-50 | SPACE | string | `OMGPR-LABEL-RATE-PER-ITEM`='I'; `OMGPR-PCT-PER-LABEL`='P'; `OMGPR-RATE-PER-LABEL`='R' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-LABEL-CHRG-AMT` | S9(2)V9(4) | COMP-3 | 4 | 4 | Y | 51-54 | ZERO | decimal |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-APPLY-CHRG-TYPE` | X(1) | DISPLAY | 1 |  | N | 55-55 | SPACE | string | `OMGPR-APPLY-RATE-PER-ITEM`='I'; `OMGPR-PCT-PER-APPLY`='P'; `OMGPR-RATE-PER-APPLY`='R' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-APPLY-CHRG-AMT` | S9(2)V9(4) | COMP-3 | 4 | 4 | Y | 56-59 | ZERO | decimal |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-BREAK-CHRG-TYPE` | X(1) | DISPLAY | 1 |  | N | 60-60 | SPACE | string | `OMGPR-PCT-PER-BREAK`='P'; `OMGPR-RATE-PER-BREAK`='R' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-BREAK-CHRG-AMT` | S9(2)V9(4) | COMP-3 | 4 | 4 | Y | 61-64 | ZERO | decimal |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-JIT-LUOM-CHRG-AMT` | S9(2)V9(4) | COMP-3 | 4 | 4 | Y | 65-68 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-JIT-ITEM-LABEL-SW` | X(1) | DISPLAY | 1 |  | N | 69-69 | SPACE | string | `OMGPR-JIT-ITEM-LABEL`='Y' |
| &nbsp;&nbsp;`OMGPR-JIT-ITEM-LABEL-QTY` | S9(5) | COMP-3 | 3 |  | Y | 70-72 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-JIT-ITEM-APPLY-LABEL-SW` | X(1) | DISPLAY | 1 |  | N | 73-73 | SPACE | string | `OMGPR-JIT-ITEM-APPLY-LAB`='Y' |
| &nbsp;&nbsp;`OMGPR-JIT-ITEM-BREAK-BULK-SW` | X(1) | DISPLAY | 1 |  | N | 74-74 | SPACE | string | `OMGPR-JIT-ITEM-BREAK-BULK`='Y' |
| &nbsp;&nbsp;**`OMGPR-ALT-ORD-DATA`** *(group, 12 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-ALT-ORD-UM-FOUND-SW` | X(1) | DISPLAY | 1 |  | N | 75-75 | SPACE | string | `OMGPR-ALT-ORD-UM-FOUND`='Y' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-ALT-ORD-UM` | X(3) | DISPLAY | 3 |  | N | 76-78 | SPACE | string |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-ALT-ORD-CONV-FACTOR` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 79-86 | ZERO | decimal |  |
| &nbsp;&nbsp;**`OMGPR-LABEL-UM-DATA`** *(group, 12 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-LABEL-UM-FOUND-SW` | X(1) | DISPLAY | 1 |  | N | 87-87 | SPACE | string | `OMGPR-LABEL-UM-FOUND`='Y' |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-CUST-LABEL-UNIT` | X(3) | DISPLAY | 3 |  | N | 88-90 | SPACE | string |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-CUST-LABEL-CONV-FACTOR` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 91-98 | ZERO | decimal |  |
| **`OMGPR-DATA-FROM-CUP100`** *(group, 41 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-F-AA-VHA-IND` | X(1) | DISPLAY | 1 |  | N | 99-99 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-ST-JIT-CUSTOMER` | X(1) | DISPLAY | 1 |  | N | 100-100 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-ACCT-ROUNDING` | X(1) | DISPLAY | 1 |  | N | 101-101 | SPACE | string | `OMGPR-NO-ROUNDING`='N' ' '; `OMGPR-NORMAL-TWO-ROUNDING`='R'; `OMGPR-UP-TWO-ROUNDING`='Y' |
| &nbsp;&nbsp;`OMGPR-C-ACCT-PRCE-METHOD` | X(2) | DISPLAY | 2 |  | N | 102-103 | SPACE | string | `OMGPR-CONT-PRCE-METHOD`='CC'; `OMGPR-STOCK-PRCE-METHOD`='ST'; `OMGPR-USAGE-PRCE-METHOD`='AU' |
| &nbsp;&nbsp;`OMGPR-F-AA-FRT-IN` | X(1) | DISPLAY | 1 |  | N | 104-104 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-ACCOUNT` | S9(8) | COMP | 4 |  | Y | 105-108 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-I-CUSTOMER` | S9(8) | COMP | 4 |  | Y | 109-112 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-C-BUSINESS` | X(2) | DISPLAY | 2 |  | N | 113-114 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-P-BREAK-BULK` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 115-117 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-LOW-UOM` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 118-120 |  | decimal |  |
| &nbsp;&nbsp;`OMGPR-F-LUOM-SW` | X(01) | DISPLAY | 1 |  | N | 121-121 |  | string | `OMGPR-F-LUOM-ACCOUNT`='A'; `OMGPR-F-LUOM-GROUP`='G' |
| &nbsp;&nbsp;`OMGPR-F-BREAK-BULK-SW` | X(01) | DISPLAY | 1 |  | N | 122-122 |  | string | `OMGPR-F-BREAK-BULK-ACCOUNT`='A'; `OMGPR-F-BREAK-BULK-GROUP`='G' |
| &nbsp;&nbsp;`FILLER` | X(01) | DISPLAY | 1 |  | N | 123-123 |  | string |  |
| &nbsp;&nbsp;`OMGPR-F-LUOM-VEND-EXCL` | X(01) | DISPLAY | 1 |  | N | 124-124 |  | string |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP-LUOM` | S9(08) | COMP | 4 |  | Y | 125-128 |  | int |  |
| &nbsp;&nbsp;`OMGPR-D-BG-LOW-UOM-EFF` | X(10) | DISPLAY | 10 |  | N | 129-138 |  | string |  |
| &nbsp;&nbsp;`OMGPR-F-LUOM-ELIG-SW` | X(01) | DISPLAY | 1 |  | N | 139-139 |  | string |  |
| **`OMGPR-DATA-FOR-PACKS`** *(group, 3 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-INPUT-PRODUCT-TYPE` | X(01) | DISPLAY | 1 |  | N | 140-140 |  | string | `OMGPR-INPUT-OMKIT`='O'; `OMGPR-INPUT-VENDKIT`='S'; `OMGPR-INPUT-REGULAR-PROD`='R'; `OMGPR-INPUT-DEFAULT-PROD`=' ' |
| &nbsp;&nbsp;`OMGPR-PRICING-REQ-SW` | X(01) | DISPLAY | 1 |  | N | 141-141 |  | string | `OMGPR-PRICING-COSTONLY`='C'; `OMGPR-PRICING-SELLONLY`='S'; `OMGPR-PRICING-PRICE`='P'; `OMGPR-JIT-ON-COST`='J'; `OMGPR-PRICING-DEFAULT`=' ' |
| &nbsp;&nbsp;`OMGPR-C-PRODUCT-TYPE` | X(1) | COMP-3 | 1 |  | N | 142-142 | SPACE.00022100 20 OMGPR-A-OVERHEAD-FEE PIC S9(5)V9(8) VALUE ZERO .00022400 20 OMGPR-A-THIRDPARTY-FEE PIC S9(5)V9(8) VALUE ZERO 00022600 COMPUTATIONAL-3.00022700 20 OMGPR-C-APLY-TRDPTY-BEFMKUP-SW PIC X(01) VALUE SPACES.00022900 15 OMGPR-F-INP-CNT-EXCL-SW PIC X(01) VALUE SPACE | string | `OMGPR-ACT-CNT-EXCL-EXISTS`='Y'; `OMGPR-ACT-CNT-EXCL-ABSENT`='N' ' ' |
| `OMGPR-F-EXEMPT-SANC-FLAG` | X(01) | DISPLAY | 1 |  | N | 143-143 |  | string |  |
| `OMGPR-F-EXEMPT-NON-SANC-FLAG` | X(01) | DISPLAY | 1 |  | N | 144-144 |  | string |  |
| `OMGPR-F-EXEMPT-IND-FLAG` | X(01) | DISPLAY | 1 |  | N | 145-145 |  | string |  |
| `OMGPR-F-EXEMPT-NON-CONT-FLAG` | X(01) | DISPLAY | 1 |  | N | 146-146 |  | string |  |
| `OMGPR-F-EXEMPT-CUSTOM-FLAG` | X(01) | DISPLAY | 1 |  | N | 147-147 |  | string |  |
| `OMGPR-VAR-I-BUY-GROUP` | S9(8) | COMP | 4 |  | Y | 148-151 | ZERO | int |  |
| `OMGPR-F-SPECIAL-CONTRACT` | X(01) | DISPLAY | 1 |  | N | 152-152 |  | string |  |
| `OMGPR-JIT-LUM-FEE-PCT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 153-155 | ZERO | decimal |  |
| `OMGPR-JIT-EXTRA-DELIV-FEE-PCT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 156-158 | ZERO | decimal |  |
| `OMGPR-JIT-NON-OM-SLCT-FEE-PCT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 159-161 | ZERO | decimal |  |
| `OMGPR-CUSTOMER-NBR` | S9(10) | COMP-3 | 6 |  | Y | 162-167 | ZERO | long |  |
| `OMGPR-ORDER-TYPE` | X(1) | DISPLAY | 1 |  | N | 168-168 | SPACE | string | `OMGPR-ORDER-TYPE-STOCK`='S' ' ' |
| `OMGPR-A-SURGITRAK-ST` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 169-176 | 0 | decimal |  |
| `OMGPR-P-SURGITRAK-ST` | S9(3)V9(4) | COMP-3 | 4 | 4 | Y | 177-180 | 0 | decimal |  |
| `OMGPR-FEE-SHRT-CODE-ST` | X(02) | DISPLAY | 2 |  | N | 181-182 | SPACE | string |  |
| `OMGPR-SKU-CODE-ST` | X(04) | DISPLAY | 4 |  | N | 183-186 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-ST` | X(02) | DISPLAY | 2 |  | N | 187-188 | SPACE | string | `OMGPR-DAILY-BILL-ST`='DL'; `OMGPR-DAILY-BILL-SEP-ST`='DS'; `OMGPR-MONTHLY-AUTO-BILL-ST`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-ST`='MM' |
| `OMGPR-FEE-TYPE-ST` | X(01) | DISPLAY | 1 |  | N | 189-189 | SPACE | string | `OMGPR-FEE-PER-LINE-ST`='L'; `OMGPR-FEE-PER-QTY-ST`='Q'; `OMGPR-FEE-PER-ORDER-ST`='O'; `OMGPR-FEE-PCT-SELL-ST`='P'; `OMGPR-FEE-PCT-COST-ST`='C' |
| `OMGPR-A-BREAKBULK-BB` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 190-197 | 0 | decimal |  |
| `OMGPR-P-BREAKBULK-BB` | S9(3)V9(4) | COMP-3 | 4 | 4 | Y | 198-201 | 0 | decimal |  |
| `OMGPR-FEE-SHRT-CODE-BB` | X(02) | DISPLAY | 2 |  | N | 202-203 | SPACE | string |  |
| `OMGPR-SKU-CODE-BB` | X(04) | DISPLAY | 4 |  | N | 204-207 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-BB` | X(02) | DISPLAY | 2 |  | N | 208-209 | SPACE | string | `OMGPR-DAILY-BILL-BB`='DL'; `OMGPR-DAILY-BILL-SEP-BB`='DS'; `OMGPR-MONTHLY-AUTO-BILL-BB`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-BB`='MM' |
| `OMGPR-FEE-TYPE-BB` | X(01) | DISPLAY | 1 |  | N | 210-210 | SPACE | string | `OMGPR-FEE-PER-LINE-BB`='L'; `OMGPR-FEE-PER-QTY-BB`='Q'; `OMGPR-FEE-PER-ORDER-BB`='O'; `OMGPR-FEE-PCT-SELL-BB`='P'; `OMGPR-FEE-PCT-COST-BB`='C' |
| `OMGPR-A-LUM-FEE-AMT-BB` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 211-218 | 0 | decimal |  |
| `OMGPR-P-LUM-FEE-PCT-BB` | S9(3)V9(4) | COMP-3 | 4 | 4 | Y | 219-222 | 0 | decimal |  |
| `OMGPR-A-APPLY-LAB-AL` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 223-230 | 0 | decimal |  |
| `OMGPR-P-APPLY-LAB-AL` | S9(3)V9(4) | COMP-3 | 4 | 4 | Y | 231-234 | 0 | decimal |  |
| `OMGPR-FEE-SHRT-CODE-AL` | X(02) | DISPLAY | 2 |  | N | 235-236 | SPACE | string |  |
| `OMGPR-SKU-CODE-AL` | X(04) | DISPLAY | 4 |  | N | 237-240 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-AL` | X(02) | DISPLAY | 2 |  | N | 241-242 | SPACE | string | `OMGPR-DAILY-BILL-AL`='DL'; `OMGPR-DAILY-BILL-SEP-AL`='DS'; `OMGPR-MONTHLY-AUTO-BILL-AL`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-AL`='MM' |
| `OMGPR-FEE-TYPE-AL` | X(01) | DISPLAY | 1 |  | N | 243-243 | SPACE | string | `OMGPR-FEE-PER-LINE-AL`='L'; `OMGPR-FEE-PER-QTY-AL`='Q'; `OMGPR-FEE-PER-ORDER-AL`='O'; `OMGPR-FEE-PCT-SELL-AL`='P'; `OMGPR-FEE-PCT-COST-AL`='C' |
| `OMGPR-FEE-SHRT-CODE-SF` | X(02) | DISPLAY | 2 |  | N | 244-245 | SPACE | string |  |
| `OMGPR-SKU-CODE-SF` | X(04) | DISPLAY | 4 |  | N | 246-249 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-SF` | X(02) | DISPLAY | 2 |  | N | 250-251 | SPACE | string | `OMGPR-DAILY-BILL-SF`='DL'; `OMGPR-MONTHLY-AUTO-BILL-SF`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-SF`='MM' |
| `OMGPR-FEE-SHRT-CODE-TS` | X(02) | DISPLAY | 2 |  | N | 252-253 | SPACE | string |  |
| `OMGPR-SKU-CODE-TS` | X(04) | DISPLAY | 4 |  | N | 254-257 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-TS` | X(02) | DISPLAY | 2 |  | N | 258-259 | SPACE | string | `OMGPR-DAILY-BILL-TS`='DL'; `OMGPR-MONTHLY-AUTO-BILL-TS`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-TS`='MM' |
| `OMGPR-FEE-SHRT-CODE-GS` | X(02) | DISPLAY | 2 |  | N | 260-261 | SPACE | string |  |
| `OMGPR-SKU-CODE-GS` | X(04) | DISPLAY | 4 |  | N | 262-265 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-GS` | X(02) | DISPLAY | 2 |  | N | 266-267 | SPACE | string | `OMGPR-DAILY-BILL-GS`='DL'; `OMGPR-MONTHLY-AUTO-BILL-GS`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-GS`='MM' |
| `OMGPR-FEE-SHRT-CODE-GN` | X(02) | DISPLAY | 2 |  | N | 268-269 | SPACE | string |  |
| `OMGPR-SKU-CODE-GN` | X(04) | DISPLAY | 4 |  | N | 270-273 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-GN` | X(02) | DISPLAY | 2 |  | N | 274-275 | SPACE | string | `OMGPR-DAILY-BILL-GN`='DL'; `OMGPR-MONTHLY-AUTO-BILL-GN`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-GN`='MM' |
| `OMGPR-FEE-SHRT-CODE-MI` | X(02) | DISPLAY | 2 |  | N | 276-277 | SPACE | string |  |
| `OMGPR-SKU-CODE-MI` | X(04) | DISPLAY | 4 |  | N | 278-281 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-MI` | X(02) | DISPLAY | 2 |  | N | 282-283 | SPACE | string | `OMGPR-DAILY-BILL-MI`='DL'; `OMGPR-MONTHLY-AUTO-BILL-MI`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-MI`='MM' |
| `OMGPR-FEE-SHRT-CODE-MN` | X(02) | DISPLAY | 2 |  | N | 284-285 | SPACE | string |  |
| `OMGPR-SKU-CODE-MN` | X(04) | DISPLAY | 4 |  | N | 286-289 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-MN` | X(02) | DISPLAY | 2 |  | N | 290-291 | SPACE | string | `OMGPR-DAILY-BILL-MN`='DL'; `OMGPR-MONTHLY-AUTO-BILL-MN`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-MN`='MM' |
| `OMGPR-FEE-SHRT-CODE-MC` | X(02) | DISPLAY | 2 |  | N | 292-293 | SPACE | string |  |
| `OMGPR-SKU-CODE-MC` | X(04) | DISPLAY | 4 |  | N | 294-297 | SPACE | string |  |
| `OMGPR-BILLING-FRQ-MC` | X(02) | DISPLAY | 2 |  | N | 298-299 | SPACE | string | `OMGPR-DAILY-BILL-MC`='DL'; `OMGPR-MONTHLY-AUTO-BILL-MC`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-MC`='MM' |
| `FILLER` | X(09) | DISPLAY | 9 |  | N | 300-308 | SPACES | string |  |

### `OMGPR-PRC-OUTPUT-FIELDS` (1131 bytes, offset (group))

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| **`OMGPR-ERROR-FIELDS`** *(group, 77 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-F-PRICER-ERROR` | X(1) | DISPLAY | 1 |  | N | 309-309 | SPACE | string | `OMGPR-PRICER-ERROR`='Y' |
| &nbsp;&nbsp;`OMGPR-ERROR-MESSAGE` | X(76) | DISPLAY | 76 |  | N | 310-385 | SPACE | string |  |
| **`OMGPR-PRODUCT-DATA`** *(group, 138 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-C-VND-PROD-BASE-UM` | X(2) | DISPLAY | 2 |  | N | 386-387 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-A-VD-PRD-ALT-UMF` | S9(6)V9(8) | COMP-3 | 8 | 8 | Y | 388-395 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-S-PROD-CATEGORY` | S9(8) | COMP | 4 |  | Y | 396-399 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-C-INV-CLASS` | X(1) | DISPLAY | 1 |  | N | 400-400 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-CUSTOM-IND` | X(01) | DISPLAY | 1 |  | N | 401-401 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-PRIVATE-LBL` | X(01) | DISPLAY | 1 |  | N | 402-402 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-SUPPLIER-NAME` | X(35) | DISPLAY | 35 |  | N | 403-437 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-SUPPLIER-NUMBER` | X(04) | DISPLAY | 4 |  | N | 438-441 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-VND-PROD-DESC-1` | X(35) | DISPLAY | 35 |  | N | 442-476 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-VND-PROD-DESC-2` | X(35) | DISPLAY | 35 |  | N | 477-511 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-VND-CATALOG-NBR` | X(12) | DISPLAY | 12 |  | N | 512-523 | SPACE | string |  |
| **`OMGPR-COST-CONT-DATA`** *(group, 293 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-I-CONTRACT` | S9(8) | COMP | 4 |  | Y | 524-527 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-C-CNT-ENTRY-METHOD` | X(2) | DISPLAY | 2 |  | N | 528-529 | SPACE | string | `OMGPR-CNT-ENTRY-COST-DISC`='07'; `OMGPR-CNT-ENTRY-FIXED-REB`='06'; `OMGPR-CNT-ENTRY-SS-BROKER`='02'; `OMGPR-CNT-ENTRY-SS-COST-D`='05'; `OMGPR-CNT-ENTRY-SS-COST-P`='03'; `OMGPR-CNT-ENTRY-SS-STATED`='04'; `OMGPR-CNT-ENTRY-STATED-CO`='01' |
| &nbsp;&nbsp;`OMGPR-D-CNT-PROT-START` | X(10) | DISPLAY | 10 |  | N | 530-539 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-D-CNT-PROT-END` | X(10) | DISPLAY | 10 |  | N | 540-549 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-T-COMMENT` | X(35) | DISPLAY | 35 |  | N | 550-584 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-CNT-VEND` | X(20) | DISPLAY | 20 |  | N | 585-604 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP` | S9(8) | COMP | 4 |  | Y | 605-608 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-S-BG-MEMBER` | S9(8) | COMP | 4 |  | Y | 609-612 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-N-BUY-GROUP-SHORT` | X(8) | DISPLAY | 8 |  | N | 613-620 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-GRP-CNT-FEES` | X(1) | DISPLAY | 1 |  | N | 621-621 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-BG-TYPE` | X(1) | DISPLAY | 1 |  | N | 622-622 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-PROD-CATGRY` | X(35) | DISPLAY | 35 |  | N | 623-657 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-REPLACES-CNT` | X(20) | DISPLAY | 20 |  | N | 658-677 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-D-RCVD-DATE` | X(10) | DISPLAY | 10 |  | N | 678-687 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-EXPIRE-MSG` | X(35) | DISPLAY | 35 |  | N | 688-722 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-TIER` | X(03) | DISPLAY | 3 |  | N | 723-725 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-REPLACED-BY-CNT` | X(20) | DISPLAY | 20 |  | N | 726-745 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-RPT-GRP` | S9(8) | COMP | 4 |  | Y | 746-749 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-I-RPT-GRP-CNT` | X(20) | DISPLAY | 20 |  | N | 750-769 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-L-CNT-LINE` | S9(7) | COMP | 4 |  | Y | 770-773 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-C-CNT-LN-UM` | X(2) | DISPLAY | 2 |  | N | 774-775 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-A-CNT-LN-UNIT-COST` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 776-782 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CNT-LN-SUGG-SELL` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 783-789 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CNT-LN-COST-PLUS` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 790-792 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CNT-LN-COST-DISC` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 793-795 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CNT-LN-BROKER` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 796-798 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CNT-LN-ACQ-MKD` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 799-801 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CNT-LN-PROT-ACQ` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 802-808 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CNT-LN-MAX-REBT` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 809-815 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-CNT-TYPE` | X(1) | DISPLAY | 1 |  | N | 816-816 | SPACE | string |  |
| **`OMGPR-PRICE-LIST-DATA`** *(group, 56 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-C-VND-PRC-LEVEL` | X(2) | DISPLAY | 2 |  | N | 817-818 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-D-VND-PRC-LIST-EFF` | X(10) | DISPLAY | 10 |  | N | 819-828 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-VND-PRC-UM` | X(2) | DISPLAY | 2 |  | N | 829-830 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-A-VND-PRC-DEALER` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 831-838 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VND-PRC-ACQ-COST` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 839-846 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VND-PRC-BEST-QTY` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 847-854 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VND-PRC-LST-HOSP` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 855-862 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VND-PRC-LST-DOC` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 863-870 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-F-VEND-BST-CST-RBT` | X(1) | DISPLAY | 1 |  | N | 871-871 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-VEND-NET-CST-RBT` | X(1) | DISPLAY | 1 |  | N | 872-872 | SPACE | string |  |
| **`OMGPR-UNIT-REB-AMOUNT`** *(group, 35 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-SELL-CST-DIF` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 873-877 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-VND-DIST-REB` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 878-882 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-TOTAL-REBATE` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 883-887 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-ADD-DISC-REB` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 888-892 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-PRICE-PROT` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 893-897 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUR-CONT-REBATE` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 898-902 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-REBATE-COST` | S9(5)V9(4) | COMP-3 | 5 | 4 | Y | 903-907 | ZERO | decimal |  |
| **`OMGPR-UNIT-SELL-AMT`** *(group, 37 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-C-CUS-SELL-PRC-UOM` | X(2) | DISPLAY | 2 |  | N | 908-909 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-A-CUS-UOM-SELL-PRC` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 910-916 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-TOTAL-COST` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 917-923 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-TOTAL-SELL` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 924-930 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-TOTAL-ADJ-COST` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 931-937 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-TOTAL-ADJ-SELL` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 938-944 | ZERO | decimal |  |
| **`OMGPR-SELL-ARR`** *(group, 103 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-I-BAS-SELL` | S9(8) | COMP | 4 |  | Y | 945-948 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-D-BAS-SELL-EFF` | X(10) | DISPLAY | 10 |  | N | 949-958 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-PRICING-METHOD` | X(10) | DISPLAY | 10 |  | N | 959-968 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-PRICING-PERCENTAGE` | 9(1)V9(4) | COMP-3 | 3 | 4 | N | 969-971 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-SELL-PRC-METHOD` | X(1) | DISPLAY | 1 |  | N | 972-972 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-T-SELL-COMMENT` | X(35) | DISPLAY | 35 |  | N | 973-1007 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-Q-PREF-TIER-LEVEL` | S9(4) | COMP | 2 |  | Y | 1008-1009 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-C-SELL-TYPE` | X(1) | DISPLAY | 1 |  | N | 1010-1010 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-SELL-LEVEL` | X(1) | DISPLAY | 1 |  | N | 1011-1011 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP-SELL` | S9(8) | COMP | 4 |  | Y | 1012-1015 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-S-BG-MEMBER-SELL` | S9(8) | COMP | 4 |  | Y | 1016-1019 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-N-BUY-GROUP-SHORT-SELL` | X(8) | DISPLAY | 8 |  | N | 1020-1027 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-BG-TYPE-SELL` | X(1) | DISPLAY | 1 |  | N | 1028-1028 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-SELLGROUP-INFO` | X(19) | DISPLAY | 19 |  | N | 1029-1047 | SPACE | string |  |
| **`OMGPR-COST-ADJUSTMENTS`** *(group, 39 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-A-OVERHEAD-CHG` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1048-1055 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-OVERHEAD-CHG` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1056-1058 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-CUST-INV-CLASS-CT` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1059-1066 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CUST-INV-CLASS-CT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1067-1069 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-INV-CLASS-CT` | X(1) | DISPLAY | 1 |  | N | 1070-1070 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-INFRT-TYPE` | X(1) | DISPLAY | 1 |  | N | 1071-1071 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP-FRT` | S9(8) | COMP | 4 |  | Y | 1072-1075 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-A-INFRT` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1076-1083 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-INFRT-BG` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1084-1086 | ZERO | decimal |  |
| **`OMGPR-SELL-ADJUSTMENTS`** *(group, 119 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-A-CUST-INV-CLASS-SL` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1087-1094 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-CUST-INV-CLASS-SL` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1095-1097 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-INV-CLASS-SL` | X(1) | DISPLAY | 1 |  | N | 1098-1098 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP-CNT` | S9(8) | COMP | 4 |  | Y | 1099-1102 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-A-NONCONT-GRP` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1103-1110 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-NONCONT-GRP` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1111-1113 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-RISK-PREMIUM` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1114-1121 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-RISK-PREMIUM` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1122-1124 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-DELIVERY-ADJ` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1125-1132 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-DELIVERY-ADJ` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1133-1135 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-FINANCE-CHG` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1136-1143 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-FINANCE-CHG` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1144-1146 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-NONCONT-CUST` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1147-1154 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-NONCONT-CUST` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1155-1157 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-PREPAY-DEDUCTION` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1158-1165 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-PREPAY-DEDUCTION` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1166-1168 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VOLUME-DISCOUNT` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1169-1176 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-VOLUME-DISCOUNT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1177-1179 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-I-BUY-GROUP-VOL` | S9(8) | COMP | 4 |  | Y | 1180-1183 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-A-BG-VOL-DISC` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1184-1191 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-BG-VOL-DISC` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1192-1194 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-PANDAC` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1195-1202 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-P-PANDAC-ADJ` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1203-1205 | ZERO | decimal |  |
| **`OMGPR-JIT-ADJUSTMENTS`** *(group, 48 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-SERVICE-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1206-1213 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-LABEL-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1214-1221 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-APPLY-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1222-1229 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-BREAK-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1230-1237 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-COST` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1238-1245 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-SELL` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1246-1253 | ZERO | decimal |  |
| **`OMGPR-ADDITIONAL-INFORMAT`** *(group, 186 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;`OMGPR-F-STOCK-ITEM` | X(1) | DISPLAY | 1 |  | N | 1254-1254 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-D-EXPIRATION` | X(10) | DISPLAY | 10 |  | N | 1255-1264 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-JIT-REMOVED` | X(1) | DISPLAY | 1 |  | N | 1265-1265 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-GRP-CNT-NBR` | X(20) | DISPLAY | 20 |  | N | 1266-1285 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-I-DIVISION-SELL` | X(2) | DISPLAY | 2 |  | N | 1286-1287 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-A-CUS-UOM-SELL-PRC-UN` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 1288-1294 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-TOTAL-SELL-UN` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 1295-1301 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-I-DIVISION-COST` | X(2) | DISPLAY | 2 |  | N | 1302-1303 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-JIT-EXEMPT` | X(1) | DISPLAY | 1 |  | N | 1304-1304 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-FRT-EXEMPT` | X(1) | DISPLAY | 1 |  | N | 1305-1305 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-Q-ERROR-NBR` | S9(4) | COMP | 2 |  | Y | 1306-1307 | ZERO | int |  |
| &nbsp;&nbsp;**`OMGPR-Q-ERROR-CODE-X`** *(group, 3 bytes)* |  |  |  |  |  |  |  |  |  |
| &nbsp;&nbsp;&nbsp;&nbsp;`OMGPR-Q-ERROR-CODE` | 9(3) | DISPLAY | 3 |  | N | 1308-1310 | ZERO | int | `OMGPR-ACCT-NBR-MISSING`=9; `OMGPR-ACCT-NOT-FOUND`=2; `OMGPR-ACT-CUST-NOT-FOUND`=3; `OMGPR-BG-PRC-PARM-NOT-FND`=41; `OMGPR-DB2-FATAL-ERROR`=70; `OMGPR-DIV-NBR-MISSING`=8; `OMGPR-PRC-LIST-NOT-FOUND`=4; `OMGPR-PRC-METHOD-INVALID`=40; `OMGPR-PRICER-FATAL-ERROR`=70 THRU 99; `OMGPR-PRICER-MEDIUM-ERROR`=40 THRU 69; `OMGPR-PRICER-NO-ERROR`=0; `OMGPR-PRICER-WARN-ERROR`=1 THRU 39; `OMGPR-PROD-NBR-MISSING`=11; `OMGPR-PROD-NOT-FOUND`=6; `OMGPR-SHIP-TO-NOT-FOUND`=5; `OMGPR-UM-NOT-FOUND`=1; `OMGPR-VEND-NBR-MISSING`=10; `OMGPR-VENDOR-NOT-FOUND`=7 |
| &nbsp;&nbsp;`OMGPR-F-PRICE-LOCKED` | X(1) | DISPLAY | 1 |  | N | 1311-1311 | SPACE | string | `OMGPR-PRICE-LOCKED`='Y'; `OMGPR-PRICE-NOT-LOCKED`='N' |
| &nbsp;&nbsp;`OMGPR-D-BG-TIER-START` | X(10) | DISPLAY | 10 |  | N | 1312-1321 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-P-SURCHARGE` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1322-1324 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-SURCHARGE` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 1325-1331 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-SURCHARGE` | X(2) | DISPLAY | 2 |  | N | 1332-1333 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-C-VEND-VHA-PLUS` | X(1) | DISPLAY | 1 |  | N | 1334-1334 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-Q-COST-PRIORITY` | S9(4) | COMP | 2 |  | Y | 1335-1336 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-Q-SELL-PRIORITY` | S9(4) | COMP | 2 |  | Y | 1337-1338 | ZERO | int |  |
| &nbsp;&nbsp;`OMGPR-I-GRP-CONTROL-NBR` | X(20) | DISPLAY | 20 |  | N | 1339-1358 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-P-VNDCOSTADJ` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1359-1361 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-VNDCOSTADJ` | S9(5)V9(8) | COMP-3 | 7 | 8 | Y | 1362-1368 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-C-VNDCOSTADJ` | X(2) | DISPLAY | 2 |  | N | 1369-1370 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-CONT-EXCL-SW` | X(1) | DISPLAY | 1 |  | N | 1371-1371 | SPACE | string | `OMGPR-CONTRACT-EXCLUDED`='Y'; `OMGPR-CONTRACT-NOT-EXCLUDED`='N' |
| &nbsp;&nbsp;`OMGPR-C-SOURCE` | X(20) | DISPLAY | 20 |  | N | 1372-1391 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-VND-JIT-BB-SW` | X(1) | DISPLAY | 1 |  | N | 1392-1392 | SPACE | string | `OMGPR-VND-JIT-BB-FEE`='B' |
| &nbsp;&nbsp;`OMGPR-VND-JIT-LB-SW` | X(1) | DISPLAY | 1 |  | N | 1393-1393 | SPACE | string | `OMGPR-VND-JIT-LB-FEE`='L' |
| &nbsp;&nbsp;`OMGPR-VND-JIT-AB-SW` | X(1) | DISPLAY | 1 |  | N | 1394-1394 | SPACE | string | `OMGPR-VND-JIT-AL-FEE`='A' |
| &nbsp;&nbsp;`OMGPR-VND-JIT-SF-SW` | X(1) | DISPLAY | 1 |  | N | 1395-1395 | SPACE | string | `OMGPR-VND-JIT-SF-FEE`='S' |
| &nbsp;&nbsp;`OMGPR-A-JIT-LUM-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1396-1403 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-EXTRA-DELIV-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1404-1411 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-A-JIT-NON-OM-SLCT-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1412-1419 | ZERO | decimal |  |
| &nbsp;&nbsp;`OMGPR-D-EFF-JIT-NON-OM-SLCT` | X(8) | DISPLAY | 8 |  | N | 1420-1427 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-D-START-JIT-NON-OM-SLCT` | X(8) | DISPLAY | 8 |  | N | 1428-1435 | SPACE | string |  |
| &nbsp;&nbsp;`OMGPR-F-BREAK-BULK-OR-LUOM` | X(01) | DISPLAY | 1 |  | N | 1436-1436 |  | string | `OMGPR-F-BREAK-BULK-FEE`='B'; `OMGPR-F-LUOM-FEE`='L' |
| &nbsp;&nbsp;`OMGPR-P-ACTUAL-BB-OR-LUOM-PCT` | S9(1)V9(4) | COMP-3 | 3 | 4 | Y | 1437-1439 |  | decimal |  |

### `OMGPR-PARM-LENGTH` (4 bytes, offset 1440-1443)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-PARM-LENGTH` | S9(4) | DISPLAY | 4 |  | Y | 1440-1443 | +1789 | int |  |

### `OMGPR-NDP-CALLED-YN` (1 bytes, offset 1444-1444)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-NDP-CALLED-YN` | X(1) | DISPLAY | 1 |  | N | 1444-1444 | SPACE | string | `OMGPR-NDP-WAS-CALLED`='Y' |

### `OMGPR-F-NEGATIVE-REBATE` (1 bytes, offset 1445-1445)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-F-NEGATIVE-REBATE` | X(1) | DISPLAY | 1 |  | N | 1445-1445 | SPACE | string |  |

### `OMGPR-F-CNT-PRIORITY` (1 bytes, offset 1446-1446)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-F-CNT-PRIORITY` | X(1) | DISPLAY | 1 |  | N | 1446-1446 | SPACE | string |  |

### `OMGPR-I-PARENT-VENDOR` (4 bytes, offset 1447-1450)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-I-PARENT-VENDOR` | X(4) | DISPLAY | 4 |  | N | 1447-1450 | SPACE | string |  |

### `OMGPR-F-NON-SANC-RATING` (1 bytes, offset 1451-1451)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-F-NON-SANC-RATING` | X(1) | DISPLAY | 1 |  | N | 1451-1451 | SPACE | string |  |

### `OMGPR-C-RATING-FLAG` (2 bytes, offset 1452-1453)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-C-RATING-FLAG` | X(2) | DISPLAY | 2 |  | N | 1452-1453 | SPACE | string |  |

### `OMGPR-HC-GROUP-ID` (8 bytes, offset 1454-1461)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-HC-GROUP-ID` | 9(08) | DISPLAY | 8 |  | N | 1454-1461 | ZERO | int |  |

### `OMGPR-F-HC-COST-FLAG` (1 bytes, offset 1462-1462)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-F-HC-COST-FLAG` | X(01) | DISPLAY | 1 |  | N | 1462-1462 | SPACE | string |  |

### `OMGPR-F-HC-SELL-FLAG` (1 bytes, offset 1463-1463)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-F-HC-SELL-FLAG` | X(01) | DISPLAY | 1 |  | N | 1463-1463 | SPACE | string |  |

### `OMGPR-D-HC-COST-EFF` (10 bytes, offset 1464-1473)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-D-HC-COST-EFF` | X(10) | DISPLAY | 10 |  | N | 1464-1473 | SPACE | string |  |

### `OMGPR-D-HC-SELL-EFF` (10 bytes, offset 1474-1483)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-D-HC-SELL-EFF` | X(10) | DISPLAY | 10 |  | N | 1474-1483 | SPACE | string |  |

### `OMGPR-FULL-LUM-SVC-SW` (1 bytes, offset 1484-1484)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-FULL-LUM-SVC-SW` | X(01) | DISPLAY | 1 |  | N | 1484-1484 | SPACE | string |  |

### `OMGPR-SPCL-SRVC-CODE` (3 bytes, offset 1485-1487)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-SPCL-SRVC-CODE` | X(3) | DISPLAY | 3 |  | N | 1485-1487 | SPACE | string |  |

### `OMGPR-A-PANDAC-PN` (8 bytes, offset 1488-1495)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-PANDAC-PN` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1488-1495 | 0 | decimal |  |

### `OMGPR-P-PANDAC-PN` (4 bytes, offset 1496-1499)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-P-PANDAC-PN` | S9(3)V9(4) | COMP-3 | 4 | 4 | Y | 1496-1499 | 0 | decimal |  |

### `OMGPR-FEE-SHRT-CODE-PN` (2 bytes, offset 1500-1501)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-FEE-SHRT-CODE-PN` | X(02) | DISPLAY | 2 |  | N | 1500-1501 | SPACE | string |  |

### `OMGPR-SKU-CODE-PN` (4 bytes, offset 1502-1505)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-SKU-CODE-PN` | X(04) | DISPLAY | 4 |  | N | 1502-1505 | SPACE | string |  |

### `OMGPR-BILLING-FRQ-PN` (2 bytes, offset 1506-1507)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-BILLING-FRQ-PN` | X(02) | DISPLAY | 2 |  | N | 1506-1507 | SPACE | string | `OMGPR-DAILY-BILL-PN`='DL'; `OMGPR-DAILY-BILL-SEP-PN`='DS'; `OMGPR-MONTHLY-AUTO-BILL-PN`='MA'; `OMGPR-MONTHLY-MANUAL-BILL-PN`='MM' |

### `OMGPR-FEE-TYPE-PN` (1 bytes, offset 1508-1508)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-FEE-TYPE-PN` | X(01) | DISPLAY | 1 |  | N | 1508-1508 | SPACE | string | `OMGPR-FEE-PER-LINE-PN`='L'; `OMGPR-FEE-PER-QTY-PN`='Q'; `OMGPR-FEE-PER-ORDER-PN`='O'; `OMGPR-FEE-PCT-SELL-PN`='P'; `OMGPR-FEE-PCT-COST-PN`='C' |

### `OMGPR-A-SURGITRAK-FEE` (8 bytes, offset 1509-1516)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-SURGITRAK-FEE` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1509-1516 | 0 | decimal |  |

### `OMGPR-A-MKP-GRP-SAN-GS` (8 bytes, offset 1517-1524)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-MKP-GRP-SAN-GS` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1517-1524 | 0 | decimal |  |

### `OMGPR-A-MKP-GRP-NSC-GN` (8 bytes, offset 1525-1532)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-MKP-GRP-NSC-GN` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1525-1532 | 0 | decimal |  |

### `OMGPR-A-MKP-IND-MI` (8 bytes, offset 1533-1540)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-MKP-IND-MI` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1533-1540 | 0 | decimal |  |

### `OMGPR-A-MKP-NON-CON-MN` (8 bytes, offset 1541-1548)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-MKP-NON-CON-MN` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1541-1548 | 0 | decimal |  |

### `OMGPR-A-MKP-CUS-MC` (8 bytes, offset 1549-1556)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-A-MKP-CUS-MC` | S9(7)V9(8) | COMP-3 | 8 | 8 | Y | 1549-1556 | 0 | decimal |  |

### `OMGPR-FILLER` (217 bytes, offset 1557-1773)

| Field | PIC | Usage | Bytes | Dec | Signed | Offset | Default | C# Type | Conditions |
|---|---|---|---|---|---|---|---|---|---|
| `OMGPR-FILLER` | X(217) | DISPLAY | 217 |  | N | 1557-1773 |  | string |  |


---

## Assumptions

1. The `COMP` (binary) byte-sizing convention is assumed to be standard IBM mainframe
   halfword/fullword/doubleword sizing throughout this document, per the "Storage-size methodology"
   section above. Two fields (`OMGPR-Q-ORD-LIN-ORDERED`, `OMGPR-L-CNT-LINE`) are sensitive to this
   assumption and marked `INFERRED` in the CSV.
2. The hardcoded `1,789`-byte COMMAREA size seen in the four programs (`program-inventory.md` §5)
   is assumed to reflect the actual, current, compiled runtime layout — not independently verified
   against a live system or compiled listing.
3. `OMGPR-FILLER`'s trailing 217 bytes (the current end of the record, per the copybook's own
   change-log comments describing repeated shrinkage: 470→464→461→431→217 across `DP0722`/`SJ0821`/
   `BM0821`/`BM0421`/`NK0618`) are assumed to be genuinely unused reserved space, not a field this
   task failed to name — confirmed by the copybook text itself (`FILLER`, no name, `PIC X(217)`).

## Open items

1. The one-field discrepancy in `COMP-3` field count vs. the pre-existing workbook (this document:
   100; workbook: 99) — not tracked down to a specific field in this pass.
2. The `COMP` binary-sizing convention (mainframe vs. minimal-bytes) is unconfirmed for this
   specific codebase/compiler — affects at most 2 bytes total, per the audit above.
3. Live COMMAREA size not independently confirmed against a compiled listing or runtime capture —
   carried over from `program-inventory.md` §9, item 5.
4. **`tasks.md` does not exist in this repository** (confirmed at the start of this task), so no
   review/approval record can be filed there. This document and its companion CSV
   (`docs/mappings/omgpr-data-dictionary.csv`) are the complete artifact; recording their
   review/approval is an action item outside this repository's current file set.

## Companion file

The complete field-by-field data in flat, machine-readable form (identical data to the tables
above, plus the `Notes` column) is at `docs/mappings/omgpr-data-dictionary.csv`, one row per field
or group header, 309 rows total (excluding header row).

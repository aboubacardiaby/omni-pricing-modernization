# OMNI Pricer — Program Inventory (T001)

**Scope:** Every `.CBL` and `.CPY` file supplied in `upload/`, plus the supplied field-inventory
workbook `upload/OMGPR_Field_Inventory.xlsx`.
**Method:** Full read of all copybooks and small/medium programs; structural extraction (COPY /
EXEC SQL INCLUDE / EXEC SQL / EXEC CICS LINK / CALL / paragraph headers / error-code assignments)
via targeted search across `A6U01.CBL` (26,657 lines — too large to read linearly), followed by
targeted reads of its PROCEDURE DIVISION entry logic and error-handling passages.
**Source of truth:** COBOL as supplied. Nothing outside `upload/` was consulted. No DB2 DCLGEN
layout or called-program behavior is asserted as fact unless the copybook/program itself is present
in `upload/`.
**Confidence key:** `CONFIRMED` = directly read in the supplied source. `INFERRED` = reasonable
reading of naming/usage patterns, not textually stated. `BLOCKED` = cannot be determined from
supplied files.

---

## 0. Files Analyzed

| File | Lines | Kind | Note |
|---|---|---|---|
| `A6U01.CBL` | 26,657 | Program | Core pricer subroutine. `PROGRAM-ID A6U01`. |
| `CUP100 (1).CBL` | 1,563 | Program | `PROGRAM-ID CUP100`. Filename has a stray `(1)` suffix — CONFIRMED artifact of the upload, not a COBOL naming convention; flagged, not corrected. |
| `A6O011U.CBL` | 1,045 | Program | `PROGRAM-ID A6O011U`. |
| `A6X01.CBL` | 230 | Program | `PROGRAM-ID A6X01`. |
| `A6O010U .CBL` | 199 | Program | `PROGRAM-ID A6O010U`. Filename has a trailing space before `.CBL` — CONFIRMED artifact of the upload, flagged, not corrected. |
| `A6O012U.CBL` | 525 | Program | `PROGRAM-ID A6O012U`. Pack/kit explosion orchestrator — supplied in a follow-up batch, resolves a §1.3/§7 BLOCKED item. See §1.8. |
| `A6O013U.CBL` | 207 | Program | `PROGRAM-ID A6O013U`. Product-category lookup — supplied in a follow-up batch, resolves a §1.3/§7 BLOCKED item. See §1.7. |
| `A6O016U.CBL` | 392 | Program | `PROGRAM-ID A6O016U`. Shared product/category/UOM DAO — supplied in a follow-up batch, resolves a §1.2/§7 BLOCKED item. See §1.6. |
| `CUP120.CBL` | 16 | Program (stub) | `PROGRAM-ID 'CUP120'`. Supplied in a follow-up batch — but the file is a near-empty shell (`-INC CUS120`); see §1.9. Only *partially* resolves the §1.5/§7 BLOCKED item — the real logic (`CUS120`) is still missing. |
| `OMGPR.CPY` | 959 | Copybook | Central pricer parameter/result record (`OMGPR`). |
| `RWDAT.CPY` | 252 | Copybook | Generic date-utility work area (RescueWare-generated). |
| `OMGEXPL.CPY` | 63 | Copybook | Pack/kit explosion request/result record. |
| `A6R13.CPY` | 47 | Copybook | Parm/result record for product-category lookup subroutine. |
| `A6R16.CPY` | 53 | Copybook | Parm/result record for product lookup subroutine. |
| `CUVFEEP.CPY` | 38 | Copybook | `CUTFEE_PRICE` table record (fee pricing, 2022 enhancement). |
| `SACIDASN.CPY` | 38 | Copybook | Sell-assignment-by-customer-ID record. |
| `A6W001.CPY` | 35 | Copybook | NDP (Net Delivered Pricing) web-service parm record. |
| `A6R10.CPY` | 44 | Copybook | Parm/result record for product-type lookup subroutine. |
| `CUVPRCMP.CPY` | 26 | Copybook | `CUTPRICE_COMPONENT` table record (2022 enhancement). |
| `HCOVDPRD.CPY` | 24 (data) | Copybook | `HC_OVRD_PRODUCT` override record. |
| `CUCIDFRT.CPY` | 19 (data) | Copybook | `CU_CID_FREIGHT_FLAG` table record. |
| `HCOVDACT.CPY` | 14 (data) | Copybook | `HC_OVRD_GROUP_ACCOUNT` override record. |
| `HCOVDGPD.CPY` | 15 (data) | Copybook | `HC_OVRD_PROD_GROUP` (detail) override record. |
| `HCOVDGRP.CPY` | 15 (data) | Copybook | `HC_OVRD_PROD_GROUP` (header) override record. |
| `HCOVDCID.CPY` | 13 (data) | Copybook | `HC_OVRD_GROUP_CUSTOMER` override record. |
| `A6G01.CPY` | 12 | Copybook | Filler-only placeholder record (1,789 bytes of `FILLER`, `VALUE SPACES`). No named fields. |
| `OMGPR_Field_Inventory.xlsx` | 3 sheets | Reference workbook | Pre-existing field-level inventory of `OMGPR.CPY` with proposed C# mapping. See §5. |

26 source files (9 programs, 16 copybooks) + 1 reference workbook. All were opened and read (the
eight smallest were read completely inline; `A6U01.CBL` and `CUP100 (1).CBL` were read completely
via a combination of full-file reads and structural search, since `A6U01.CBL` alone is larger than
one read window). `A6O012U.CBL`, `A6O013U.CBL`, `A6O016U.CBL`, and `CUP120.CBL` were supplied in a
follow-up batch and read after the rest of this document was first drafted; §1.6–§1.9 and every
other section were updated in place to fold them in — no earlier finding was left unreconciled.

---

## 1. Program Inventory

### 1.1 `A6X01` — Online pricer proxy/router
- **CONFIRMED.** `PROGRAM-ID. A6X01 INITIAL.` CICS/DB2 program. Author SATHYA, 1999-01-30, most
  recently touched under change-tag `SA0703` (kitting) and `MFMIGR` (DB2 ANSI date-conversion
  migration).
- **Purpose (CONFIRMED, header comment):** "THIS PROGRAM IS LINKED TO EXECUTE PRICER SUBROUTINE
  A6U01. THIS PROGRAM IN EFFECT HAS BECOME PROXY FOR ACTUAL PRICER WHICH IS A6U01."
- **Entry:** `DFHCOMMAREA` (`OCCURS 1 TO 32767 DEPENDING ON EIBCALEN`), moved into `OMGPR`.
- **Copybooks used:** `A6R10`, `OMGPR`. `EXEC SQL INCLUDE DFHRESP`, `EXEC SQL INCLUDE SQLCA`.
- **Main paragraphs (CONFIRMED, `000-LINK-SUBROUTINE` is the sole procedure-division entry):**
  - `000-LINK-SUBROUTINE` — top-level dispatch; guards on `EIBCALEN > 0`.
  - `025-GET-DATE` — one embedded `EXEC SQL SELECT CONVERT(CHAR(10),CURRENT_TIMESTAMP,101) INTO :WS-CURRENT-DATE`.
  - `200-LINK-A6O010` — `EXEC CICS LINK PROGRAM('A6O010U') COMMAREA(A6R10-REC)` to resolve product type when not already known.
  - `050-PROCESS-PRICE` — dispatches to `100-LINK-TO-PRICER` (regular product) or `110-LINK-TO-A6U11` (kit product, when `A6W10-PROD-TYPE = 'O'`).
  - `100-LINK-TO-PRICER` — `EXEC CICS LINK PROGRAM('A6U01') COMMAREA(OMGPR)`.
  - `110-LINK-TO-A6U11` — `EXEC CICS LINK PROGRAM('A6O011U') COMMAREA(OMGPR)`.
  - `9010-MOVE-ERROR` — normalizes SQL vs. non-SQL error message into `WS-ERR-MSG`.
- **Errors:** No input → `'Y'` to `OMGPR-F-PRICER-ERROR`, message `' NO DATA RECEIVED BY A6X01'`.
  CICS `DFHRESP-PGMIDERR` on any LINK → `'<target> TRANSACTION IS UNAVAILABLE'`. Any other non-zero
  RESP → generic `'ERROR-UNABLE TO LINK TO <target>'`. SQL failure in `025-GET-DATE` →
  `'A6X01- SQL ERROR '` with `SQLCODE`.
- **Calls out (CONFIRMED):** `A6O010U`, `A6U01`, `A6O011U` — all three present in `upload/`.
- **Called by:** Not present in `upload/` — BLOCKED. (Presumed to be a CICS transaction entry point invoked by an online screen/transaction driver not supplied.)

### 1.2 `A6O010U` — Product-type lookup (online)
- **CONFIRMED.** `PROGRAM-ID. A6O010U IS INITIAL.` Author SATHYA. Header comment: "ONLINE
  COMPONENT TO GET PRODUCT TYPE," project "KITTING," implementation date 2003-07-31.
- **Copybooks used:** `A6R10` (as `WS-WHAT-TO-PASS`), `A6R16` (as `A6W016U-PARM-LIST`).
  `EXEC SQL INCLUDE DFHRESP`, `EXEC SQL INCLUDE SQLCA`.
- **Paragraphs (CONFIRMED):** `0010-INIT` → `0020-PROCESS` → `0030-EOJ`, with helpers
  `0200-READ-PRODUCT-DATA`, `1000-VALIDATE-INPUT`, `8000-PROCESS-RETURN`,
  `9015-GET-PRODUCT-DAO`.
- **Calls out (CONFIRMED):** `EXEC CICS LINK PROGRAM('A6O016U') COMMAREA(A6W016U-PARM-LIST)` in
  `9015-GET-PRODUCT-DAO`, with `A6W16-REQUIREMENT` set to `'P'` (`WS-REQ-GET-PROD-TYPE`, i.e.
  `A6W16-GET-PROD-INFO`) — asks `A6O016U` for product type only. **`A6O016U` source was initially
  missing; it has since been supplied and is now CONFIRMED — see §1.6.**
- **Validation (CONFIRMED, `1000-VALIDATE-INPUT`):** vendor (`A6W10-I-VENDOR`) and vendor-product
  (`A6W10-I-VND-PRODUCT`) must not be spaces; error codes `'61001- VENDOR# NOT PROVIDED'` /
  `'61004- PROD# NOT PROVIDED'` if missing, flagged non-abend `'E'`.
- **Errors:** CICS `DFHRESP-PGMIDERR` → `'A6U16 TRANSACTION IS UNAVAILABLE'` (note: message text
  says A6U16, the legacy name, while the actual LINK target is `A6O016U` — CONFIRMED text
  mismatch, left as-is per "preserve legacy field/message text"). Other non-zero RESP →
  `'CICS LINK ERR '` plus edited SQLCODE.
- **Called by (CONFIRMED):** `A6X01` (`200-LINK-A6O010`).
- **Naming note (INFERRED):** the copybook header for `A6R10` says it serves "SUBROUTINE A6U10 &
  A6U10B" — the working-storage in `A6O010U` still calls itself `A6U10` in `WS-MARKER`
  (`'A6U10 BEGINS>>'`) and `WS-CURRENT-PROGRAM` (`'A6U10   '`). This suggests the program was
  renamed from `A6U10` to `A6O010U` at some point without a full internal cleanup. Not certain —
  no rename log was supplied.

### 1.3 `A6O011U` — Kit/pack pricer (online)
- **CONFIRMED.** `PROGRAM-ID. A6O011U IS INITIAL.` Author SATHYA, COBOL2-CICS-DB2. Header warns:
  "ANY CHANGE TO THIS SHOULD ALSO BE MADE TO A6O011UB" (a sibling/batch twin **not present in
  `upload/` — BLOCKED**). Most recent change tag `MD1019` (2019-10-03, PRB0051772 acquisition-cost
  fix).
- **Purpose (CONFIRMED, header):** "COMPONENT TO HANDLE PRICING REQUESTS FOR KITS" — explodes a
  kit/pack product number into its components, prices each component individually via `A6U01`,
  then aggregates cost/sell back onto one `OMGPR` record representing the pack.
- **Copybooks used:** `OMGPR` (redefined locally as `A6U01-PARM-LIST`), `A6R13` (as
  `A6W013U-PARM-LIST`), `OMGEXPL` (as `A6W012U-PARM-LIST`). `EXEC SQL INCLUDE DFHRESP`,
  `INCLUDE VNG02`, `INCLUDE VNG05`, `INCLUDE SQLCA`.
- **Main flow (CONFIRMED):** `0010-INIT` → `0020-PROCESS` → `0030-EOJ`.
  `0020-PROCESS` = `1000-VALIDATE-INPUT` (no-op, `CONTINUE`) → `0100-GET-CATEGORY` (resolve
  product category/base UOM via `A6O013U`) → `0200-EXPLODE` (explode the pack via `A6O012U`) →
  `2000-PROCESS-PRICE` (loop `PERFORM VARYING SUB` over each exploded component, pricing each via
  `9000-LINK-TO-PRICER` = `EXEC CICS LINK PROGRAM(A6U01-PROGRAM)`, accumulating cost/JIT/rebate
  buckets) → `3100-MOVE-COSTS` → `4000-COMPUTE-SELL` (re-links to `A6U01` in sell mode `'S'`, and
  in JIT-on-cost mode `'J'` when applicable) → `4200-PROCESS-EXP-DATE` → optional
  `4300-HANDLE-ALT-UOM-KIT` (unit-of-measure conversion for the whole kit).
- **Calls out (CONFIRMED):**
  - `EXEC CICS LINK PROGRAM(A6U01-PROGRAM='A6U01')` — present in `upload/`.
  - `EXEC CICS LINK PROGRAM(A6W012U-PROGRAM='A6O012U')` (paragraph `9015-LINK-TO-EXPLODER`) —
    **initially missing, now supplied and CONFIRMED — see §1.8.** Interface copybook `OMGEXPL.CPY`
    IS supplied, so the request/response shape was already CONFIRMED even before `A6O012U.CBL`
    arrived.
  - `EXEC CICS LINK PROGRAM(A6W013U-PROGRAM='A6O013U')` (paragraph `9020-LINK-TO-A6O013`) —
    **initially missing, now supplied and CONFIRMED — see §1.7.** Interface copybook `A6R13.CPY`
    IS supplied.
  - Direct `EXEC SQL SELECT` against `VNG02` (`9490-SQL-SELECT-VNG02`) and `VNG05`
    (`9495-SELECT-VNG05`) for alternate unit-of-measure conversion factors. Neither `VNG02.CPY`
    nor `VNG05.CPY` DCLGEN is supplied — still **BLOCKED** for the *full* field-level layout, but
    the columns referenced here (`C_VND_PROD_BASE_UM`, `A_VD_PRD_ALT_UMF`) are now corroborated by
    a second, independent SQL site in `A6O016U` (§1.6) that selects the same two columns plus
    `C_PRODUCT_TYPE` (`VNG02`) and `C_VD_PRD_ALT_UM` (`VNG05`, the WHERE-clause key) — still
    SQL-text-level only, not a full DCLGEN, but two independent confirmations of the same column
    names raises confidence they are complete for *this* use case.
- **Business limit (CONFIRMED, `3000-COMPUTE-COST`):** every accumulated pack-level money bucket
  is checked against `WS-AMOUNT-LIMIT = 99999`; if any exceeds it, error `'PACK SELL/COST AMT
  EXCEEDS LIMIT'` / `'SELL AMT EXCEEDS LIMIT'` / `'SELL ADJ AMT EXCEEDS LIMIT'`, error numbers
  `3000` / `4000`.
- **Called by (CONFIRMED):** `A6X01` (`110-LINK-TO-A6U11`, when the product's type resolves to
  `'O'`, i.e., an OM kit).

### 1.4 `A6U01` — Core pricer (the pricing engine)
- **CONFIRMED.** `PROGRAM-ID. A6U01 IS INITIAL.` Author SATHYA. This is the program every other
  program in this upload ultimately calls to get a price; per AGENTS.md this is the primary
  behavioral source of truth for the migration.
- **Size/shape (CONFIRMED):** 26,657 lines; single `PROCEDURE DIVISION`, no internal `CALL` to
  another compiled unit except one dynamic call described below; **no `EXEC CICS LINK` at all** —
  i.e., `A6U01` is a pure computation leaf once entered, driven entirely by embedded SQL and local
  `PERFORM`s. It is itself only ever entered via `EXEC CICS LINK` from its callers.
- **Copybooks used directly (`COPY`, CONFIRMED):** `A6G01` (filler placeholder, line 521),
  `RWDAT` (date utilities, line 2093), `DFHBMSCA` (standard CICS BMS attribute copybook, not
  application-specific), plus (via change-tag `SJ0821`) `HCOVDACT`, `HCOVDCID`, `HCOVDGRP`,
  `HCOVDGPD`, `HCOVDPRD` (the HC-OVRD-* override series), and (`NK0615`) `A6W001` (NDP parm
  record).
- **`EXEC SQL INCLUDE` DCLGEN dependencies:** ~137 distinct `INCLUDE` targets (see §4 — the large
  majority of these table layouts are **NOT supplied and are BLOCKED**). Four `INCLUDE` lines
  (`BGG12`, `BGG13`, `BGG14`, `BGG15`, plus one `SYV20`) are COBOL-commented-out
  (`*WSPASS`/`*` in the indicator column) and are **not active dependencies** — flagged so they are
  not mistaken for live ones.
- **Entry point (CONFIRMED, line 2412 onward):** `PROCEDURE DIVISION.` → `0010-OVERHEAD-010`
  → `PERFORM 0020-OLN-PRICING-ROUTINE-010` → `PERFORM 8990-EOJ-010` (which itself performs
  `8995-CICS-RETURN-010`, containing the only `EXEC CICS RETURN` / `GOBACK` in the program).
- **Dispatch logic (CONFIRMED, `0020-OLN-PRICING-ROUTINE-010`, ~line 2429):**
  1. `PERFORM 7355-Y2KNE-010` (Y2K-safe date normalization).
  2. Unless the request is sell-only or JIT-on-cost, `PERFORM 0070-INIT-FOR-COST`.
  3. Unless the request is JIT-on-cost (which uses the caller-supplied pricing date directly),
     `PERFORM 7185-PRO-PASSED-DATA-010` (parses/validates the incoming `OMGPR` record) then
     `PERFORM 7725-INIT-BGARRAY-010` ("INTRODUCED FOR PERF ENHANCEMENT" — pre-loads a buy-group
     array).
  4. `EVALUATE TRUE` on `OMGPR-PRICING-REQ-SW` (see `OMGPR.CPY` 88-levels
     `OMGPR-PRICING-COSTONLY`/`-SELLONLY`/`-PRICE`/`-JIT-ON-COST`/`-DEFAULT`):
     - Default, `'P'` (price), or `'C'` (cost-only), or `OTHER` → `PERFORM 0025-PROCESS-FOR-PRICE`.
     - `'S'` (sell-only) → `PERFORM 0035-PROCESS-FOR-SELL-ONLY`.
     - `'J'` (JIT-on-cost) → `PERFORM 7225-PRO-JIT-ADJ-010` directly (skips the rest of the
       cost/sell pipeline).
  5. `0025-PROCESS-FOR-PRICE` = `0030-PROCESS-COST` → `0035-PROCESS-FOR-SELL-ONLY` (reused) →
     `0040-PROCESS-SELL-N-ADJ` → `0050-PROCESS-EXP-DATE`.
- **Paragraph organization (CONFIRMED, numeric-range convention observed across ~625 labeled
  points in the source — includes both paragraph headers and `PERFORM ... THRU x-EXIT` target
  labels):**
  - `00xx`/`01xx` — top-level cost/sell orchestration and specific suggested-price-method
    resolution paragraphs `0135`–`0180` (`SEL-SUGG-BROKERAG`, `SEL-SUGG-COST-PLU`,
    `SEL-SUGG-STATED`, `SEL-SUGG-COST-DIS`, `SEL-SUGG-FIXED-RE`, `CVT-STATED-PRC`, `PRO-PANDAC`,
    `SEL-PANDAC`, `PRO-SELL-INDV-CON`) — these line up 1:1 with the seven `OMGPR-C-CNT-ENTRY-*`
    88-level contract-entry-method codes `01`–`07` defined in `OMGPR.CPY`
    (`STATED-CO`, `SS-BROKER`, `SS-COST-P`, `SS-STATED`, `SS-COST-D`, `FIXED-REB`, `COST-DISC`).
    Full rule-by-rule extraction of this dispatch is **out of scope for T001** (belongs to a
    later decision-table-extraction task) but the entry points are catalogued here.
  - `72xx`–`79xx` — the large majority of paragraphs (403 of ~625 labeled points): SQL
    select/cursor-driven lookups (contract, buy-group, product-category, freight, JIT, rebate,
    surcharge, non-sanctioned-rating, HC override lookups, etc.).
  - `89xx` — end-of-job / CICS return (`8990-EOJ-010`, `8995-CICS-RETURN-010`).
  - `9xxx` — additional select/support paragraphs (119 labeled points), including
    `9750-CHK-FOR-NET-PRICING` / `9800-LINK-TO-NDP` (the NDP web-service call path, see below).
- **Dynamic call out (CONFIRMED, line 24709, change-tag `NK0615`):**
  `MOVE 'A6P001WB' TO A6W001-PROGRAM.` ... `CALL A6W001-PROGRAM USING A6W001-PARM-LIST.`
  This is a **content-of-variable dynamic `CALL`**, not `EXEC CICS LINK` — a different invocation
  mechanism from every other cross-program call in this codebase. Gated in `7195-PRO-ADJ-SELL-010`
  by `IF OMGPR-ECOMMERCE-PRICING` (i.e., `OMGPR-CUST-TYPE = 'Y'`, which is itself set upstream by
  `CUP100` from `CUTMST.CUST_TYPE = 'OT'` — see §1.5). On return, `A6W001-WEB-ERROR-NBR = 404`
  means "price not found in NDP" (falls through, not fatal); `ZEROES` means success (NDP sell
  price/agreement/sell-group data moved into `OMGPR`); any other value sets
  `OMGPR-F-PRICER-ERROR` and jumps to `0020-EXIT-PRICER`. **`A6P001WB` source is NOT present in
  `upload/` — BLOCKED.** Its interface copybook `A6W001.CPY` IS supplied.
  Two more `CALL 'OMTRACE' USING WS-TRACE-DATA` lines exist but are fully commented out
  (`NK0615*`) — **not active**.
- **Error catalog:** see §6.
- **Called by (CONFIRMED):** `A6X01` (`100-LINK-TO-PRICER`, regular products) and `A6O011U`
  (`9000-LINK-TO-PRICER`, once per exploded kit component, plus again for the sell/JIT-on-cost
  passes). Header comment at line ~358 ("THE PGMS THAT CALL THE PRICER") implies there may be
  additional callers in the wider estate not included in this upload — **BLOCKED**, not assumed.

### 1.5 `CUP100` — One-time customer/account data gatherer
- **CONFIRMED.** `PROGRAM-ID. CUP100.` Author DR HUMPHRIES, written 1996-07-28. Header purpose:
  "SUBROUTINE TO PERFORM ONE-TIME CUSTOMER/ACCOUNT RELATED CALLS TO DB2 TABLES WHEN PRICING ITEMS
  FOR SAME CUSTOMER" — i.e., an account/customer-level pre-fetch that runs once per
  customer/order rather than once per line item, populating `OMGPR-DATA-FROM-CUP100` and related
  fields before per-item pricing begins.
- **Copybooks used:** `CUCIDFRT` (inline `01 CU-CID-FREIGHT-FLAG`), `CUVFEEP`, `CUVPRCMP`.
  `EXEC SQL INCLUDE`: `CUG40`, `CUG41`, `CCG25`, `BGG03`, `BGG23`, `BGG24`, `CUG02`, `CUG03`,
  `CUG06`, `CUG07`, `CUG10`, `CUG11`, `CUG53`, `CUG17`, `CUVMST`, `SQLCA`, `OMGPR`, `CUR120`.
  Of these, only `OMGPR`, `CUVFEEP`, `CUVPRCMP` (and the inline `CUCIDFRT` structure) are actually
  supplied as copybook files in `upload/` — the rest are **BLOCKED** (DCLGEN not supplied).
- **Main flow (CONFIRMED, `PROCEDURE DIVISION`):**
  `A100-PRO-PASS-DATA` (validate division/account present) → `A200-SEL-ACCOUNT` (`CUG03`) →
  `A300-PRO-CUST-INFO` (→ `A310-SEL-ACTIVE-CUST` against `CUG02`) →
  `2000-GET-PRICE-FEE-DATA` (2022 addition, tag `SJ1022`) →
  `A350-PRO-CUTMST-INFO` (2015 addition, tag `NK0615`; `CUTMST` lookup, sets `OMGPR-CUST-TYPE`) →
  `A400-GET-JIT-ADJ` (`CALL 'CUP120' USING WS-DUMMY, WS-DUMMY, SQLCA, CUR120` — see below) →
  `A425-PRO-CUG53` (freight/exemption flags; falls back to `A450-READ-CID-FREIGHT-FLAG` against
  `CU_CID_FREIGHT_FLAG` on `SQLCODE +100`) →
  `A800-GET-LOW-UOM-PCT` (→ `A810`/`A820`-SEL-*-PRIORITY, `A830-SEL-LOW-UOM`,
  `A840-SEL-LOW-UOM-EXCL`, `A850-SEL-PARENTS` — buy-group priority/parent-chain walk for low-UOM
  percentage) →
  `1000-VERFIFY-CONTRACT-EXCL` (`CCG25`; `SQLCODE = -811` is explicitly treated as "exclusion
  exists" (`'Y'`), a non-standard reuse of a DB2 "multiple rows" error code as a business signal —
  CONFIRMED at line ~1062) →
  `3000-GET-PRICE-COMPONENT-DATA` (2022 addition; `3100`–`3700` fetch freight/surcharge/markup
  fee-component rows for codes `SF`,`TS`,`GS`,`GN`,`MI`,`MN`,`MC` from `CUTPRICE_COMPONENT`).
  Note `A500-SEL-SHIP-TO` (ship-to JIT lookup against `CUG17`) is defined but its call site in the
  mainline is commented out (`002931******`) — **present but currently unreachable**; flagged, not
  assumed dead in all environments since it could be reinstated or invoked conditionally elsewhere
  not visible here.
- **`A400-GET-JIT-ADJ` (CONFIRMED):** `MOVE 'CUP120' TO WS-PGM-ID.` `CALL WS-PGM-ID USING
  WS-DUMMY, WS-DUMMY, SQLCA, CUR120.` — a **static-name-in-a-variable `CALL`** to subprogram
  `CUP120`, passing the `CUR120` copybook record. `CUP120.CBL` has since been supplied — see §1.9
  — but it turns out to be an empty shell whose real logic is a separately-compiled member
  (`CUS120`) that is **still not supplied — still BLOCKED**, just one level deeper than originally
  thought. `CUR120.CPY` is also **NOT present in `upload/` — still BLOCKED**, even though dozens of
  `CUR120-*` fields (JIT service/label/apply/break fees, LUOM/break-bulk switches, 2022 SJ1022
  break-bulk/apply-label fee fields) are consumed by name here — their exact PIC clauses/lengths
  cannot be confirmed from the supplied files.
- **Errors:** every SQL paragraph follows the same pattern — `SQLCODE +0` success,
  `+100` not-found (usually tolerated, sometimes an error, see per-paragraph error codes `102`,
  `103`, `106`, `108`, `120`), `OTHER` → `WS-DB-ERROR-NBR` (10/20/25/30/40/50/60/70/80/90/220/1000
  per table) folded into `OMGPR-Q-ERROR-NBR` and a formatted `'#nnn IN CUP100 CAUSED RTN CODE
  nnnnnn'`-style message, plus `'Y'` to `OMGPR-F-PRICER-ERROR`.
- **Called by:** Not present in `upload/` — **BLOCKED**. No program in this upload set issues
  `EXEC CICS LINK` or `CALL` naming `CUP100`; `A6U01.CBL` only references it in comments
  ("...PASSED THROUGH CUP100/CUP110" — `CUP110` is also never seen elsewhere in this upload, same
  BLOCKED status). CUP100 must be invoked by an online transaction driver upstream of `A6X01` that
  is outside this upload set.

### 1.6 `A6O016U` — Shared product/category/UOM DAO
- **CONFIRMED.** `PROGRAM-ID. A6O016U IS INITIAL.` Author SATHYA / DATA DIRECTIONS. Header purpose:
  "PRODUCT DAO... GET PRODUCT TYPE, BASE UOM, CATEGORY FOR A GIVEN PRODUCT. ALSO THIS OUTPUTS CONV
  FACTOR AN ALTERNATE UOM." Marked "DUAL COMPILER" in the header (a build/compile-variant note, not
  further explained in-file). This is the **single shared low-level product lookup routine behind
  both `A6O010U` and `A6O013U`** — those two programs differ only in which `A6W16-REQUIREMENT`
  value they pass in.
- **Copybooks/includes used:** `A6R16` (as `WS-WHAT-TO-PASS`, i.e. it owns/defines the parm record
  the same way `A6O010U`/`A6O013U` only borrow it). `EXEC SQL INCLUDE`: `VNG05`, `VNG06`, `ING01`,
  `VNG02`, `SQLCA`.
- **Main flow (CONFIRMED):** `0010-INIT` → `0020-PROCESS` → `0030-EOJ`.
  `0020-PROCESS` = `1000-VALIDATE-INPUT` (vendor/product not spaces, same `61601`/`61604` message
  numbering family as `A6O010U`'s `61001`/`61004` and `A6O013U`'s `61301`/`61304` — CONFIRMED
  these three programs share one error-numbering convention, `6<transid><seq>`) →
  `2200-READ-PRODUCT-DATA` (`SELECT C_PRODUCT_TYPE, C_VND_PROD_BASE_UM FROM VNG02`; product type
  `'O'`=Owens kit, `'S'`=supplier kit, anything else including blank → normalized to `'R'` regular
  product — CONFIRMED explicit fallback) → if `A6W16-GET-PROD-INFO` (i.e. `A6O010U`'s request),
  **stop here**, product type is all that was asked for → otherwise `2000-FIND-CAT` (`SELECT
  S_PROD_CATEGORY, D_CAT_PROD_EFFECT, D_CAT_PROD_EXPIRE FROM VNG06`, with a null-indicator check on
  the expire date) → if a branch/division was supplied, `3000-FIND-INV-CLASS` (`SELECT
  C_DIV_INV_PRC_LVL, F_STOCK_ITEM, A_DIV_INV_FREIGHT, C_INV_CLASS, C_INV_SUB_CLASS FROM ING01`,
  keyed by division+vendor+product) → if a UOM was supplied and differs from the resolved base UOM,
  `2500-FIND-CONV-FACTOR` (`SELECT A_VD_PRD_ALT_UMF FROM VNG05`, keyed by
  vendor+product+alt-UOM-code); if the requested UOM *equals* the base UOM, the conversion factor
  is set to `1` without a database call (CONFIRMED, line 141).
- **Errors (CONFIRMED, message numbers `616nn`):** `61601`/`61604` (vendor/product not provided),
  `61602` (SQL error selecting `VNG02`), `61603` (product not found in `VNG02`, non-abend),
  `61605` (product category not found in `VNG06`, warning-severity), `61606` (SQL error selecting
  `VNG06` — note this same number `61606` is reused verbatim as the message text for a *third*,
  different failure in `3000-FIND-INV-CLASS`, whose comment even misnames the table as "VNG06"
  when the SQL is actually against `ING01` — CONFIRMED copy-paste artifact, preserved as-is), and
  `61606`/`61607` for the `VNG05` conversion-factor lookup (`61606` = not found, `61607` = SQL
  error — so `61606` is used for three semantically different situations across this one program;
  flagged, not corrected).
- **SQL column shapes now CONFIRMED (SQL-text level, not full DCLGEN) for:** `VNG02`
  (`I_VENDOR`, `I_VND_PRODUCT`, `C_PRODUCT_TYPE`, `C_VND_PROD_BASE_UM`), `VNG06` (`I_VENDOR`,
  `I_VND_PRODUCT`, `S_PROD_CATEGORY`, `D_CAT_PROD_EFFECT`, `D_CAT_PROD_EXPIRE` +
  null-indicator), `ING01` (`I_DIVISION`, `I_VENDOR`, `I_VND_PRODUCT`, `C_DIV_INV_PRC_LVL`,
  `F_STOCK_ITEM`, `A_DIV_INV_FREIGHT`, `C_INV_CLASS`, `C_INV_SUB_CLASS`), `VNG05` (`I_VENDOR`,
  `I_VND_PRODUCT`, `C_VD_PRD_ALT_UM`, `A_VD_PRD_ALT_UMF`). None of these has a full DCLGEN copybook
  supplied, so any column **not** referenced in this SQL text remains BLOCKED.
- **Called by (CONFIRMED):** `A6O010U` (`9015-GET-PRODUCT-DAO`, requirement `'P'`) and `A6O013U`
  (`9015-GET-PRODUCT-DAO`, requirement `'C'`, see §1.7). No other caller found in `upload/`.

### 1.7 `A6O013U` — Product-category lookup (online)
- **CONFIRMED.** `PROGRAM-ID. A6O013U IS INITIAL.` Author SATHYA, project KITTING, implementation
  date 2003-07-31 — same project/date/author as `A6O010U`, and structurally a near-clone of it
  (identical `0010-INIT`/`0020-PROCESS`/`0030-EOJ`/`1000-VALIDATE-INPUT`/`8000-PROCESS-RETURN`/
  `9015-GET-PRODUCT-DAO` paragraph skeleton).
- **Purpose (CONFIRMED, header):** "ONLINE COMPONENT TO GET CATEGORY & PRICE LIST FOR A PRODUCT."
  It does **no SQL of its own** — it is a thin wrapper that calls `A6O016U` with
  `A6W16-REQUIREMENT = 'C'` (`WS-GET-CATEGORY`, i.e. `A6W16-GET-CAT-INFO`) and republishes the
  result into the `A6R13` shape.
- **Copybooks used:** `A6R13` (as `WS-WHAT-TO-PASS`), `A6R16` (as `A6W016U-PARM-LIST`).
  `EXEC SQL INCLUDE DFHRESP`, `EXEC SQL INCLUDE SQLCA`.
- **Calls out (CONFIRMED):** `EXEC CICS LINK PROGRAM(A6W016U-PROGRAM='A6O016U')
  COMMAREA(A6W016U-PARM-LIST)` in `9015-GET-PRODUCT-DAO` — present in `upload/`, see §1.6.
- **Validation (CONFIRMED, `1000-VALIDATE-INPUT`):** vendor/product not spaces, error messages
  `'61301- VENDOR# NOT PROVIDED'` / `'61304- PROD# NOT PROVIDED'`.
- **Errors:** CICS `DFHRESP-PGMIDERR` → `'A6U16 TRANSACTION IS UNAVAILABLE'` (again the legacy-name
  message text pointing at `A6U16` rather than `A6O016U`, identical pattern to `A6O010U`, §1.2).
  Other non-zero RESP → `'CICS LINK ERR '` + edited SQLCODE.
- **Called by (CONFIRMED):** `A6O011U` (`9020-LINK-TO-A6O013`, paragraph `0100-GET-CATEGORY`,
  §1.3), to resolve `OMGPR-S-PROD-CATEGORY`/`OMGPR-C-VND-PROD-BASE-UM`/`OMGPR-C-INV-CLASS` and, if
  the ordered UOM differs from base UOM, `OMGPR-ALT-ORD-UM`/`OMGPR-ALT-ORD-CONV-FACTOR` for each
  exploded kit component.
- **Naming note (INFERRED, same pattern as `A6O010U`):** working-storage still self-identifies as
  legacy name `A6U13` (`WS-MARKER = 'A6U13 BEGINS>>'`, `WS-CURRENT-PROGRAM = 'A6U13   '`),
  consistent with `A6R13.CPY`'s header comment ("SUBROUTINE A6U13 & A6U13B").

### 1.8 `A6O012U` — Pack/kit explosion orchestrator
- **CONFIRMED.** `PROGRAM-ID. A6O012U IS INITIAL.` Author SATHYA, project KITTING, 2003-07-31, most
  recent change tag `NK1209` (2004-12-09, incident #1667882: "PACK WITH 4 SUBPACKS W/ THE SAME
  PRODUCT # ONLY BEING COSTING 1 TIME INSTEAD OF 4" — a quantity-multiplication bug fix for nested
  sub-packs, visible in `3200`/`3400`-series `COMPUTE OMGEXPL-COMP-QTY(...) = OMGPK-COMP-QTY(SUB) *
  WS-PARENT-OF-COMP-QTY`, §below).
- **Purpose (CONFIRMED, header):** "ONLINE COMPONENT TO PROCESS THE REQUEST FOR EXPLOSION. 1. THIS
  INVOKES ANOTHER COMPONENT A6O015U TO CARRY OUT EXPLOSION. 2. GOES THROUGH RESULT GOT IN OMGPK &
  FILLS UP OMGEXPL AS PER REQUEST TYPE." **`A6O012U` does not itself explode a pack/kit — it is a
  request-shaping wrapper around a still-lower-level program, `A6O015U`, which does the actual
  explosion into a raw record shape called `OMGPK`.** This is a third layer that was not visible
  from `A6O011U` alone.
- **Copybooks/includes used:** `OMGEXPL` (as `WS-WHAT-TO-PASS`, i.e. `A6O012U` owns/defines this
  parm record the same way `A6O011U` only borrows it), `OMGPK` (as `A6W015U-PARM-LIST` — **NOT
  present in `upload/` — BLOCKED**, though its field names are extensively visible via usage: header
  fields `OMGPK-PRODUCT-TYPE`/`-CUSTOM-SOURCE`/`-BASE-UOM`/`-OH-FEE`/`-OH-EFF-DATE`/`-OH-EXP-DATE`/
  `-TP-FEE-COST`/`-TP-COST-EFF-DATE`/`-TP-COST-EXP-DATE`/`-TP-FEE-SELL`/`-TP-SELL-EFF-DATE`/
  `-TP-SELL-EXP-DATE`/`-NUMBER-OF-ITEMS`/`-COMPLETE-EXPLODE-SW`, and a component table
  `OMGPK-COMP-PROD-TYPE/-PROD-NO/-UOM/-QTY/-SUB-PACK-NUM` indexed the same way as
  `OMGEXPL-COMP-INFO` — CONFIRMED (INFERRED shape) that `OMGPK` is the raw, unfiltered explosion
  result that `A6O012U` reshapes/filters into the caller-facing `OMGEXPL` structure depending on
  request type). Also `COPY SYR000.` and `COPY SYR208.` (both **NOT present in `upload/` —
  BLOCKED**, no usage of their fields was found in the paragraphs read, so their purpose is
  unconfirmed) and, unusually, `COPY SYH208.` **called from inside paragraph `0018-VALIDATE-DATE`**
  (line 249) rather than in the DATA DIVISION — i.e. this copybook expands to *procedural*
  statements, not a data structure, driven by `SYU208-IN-DT`/checked via `SYU208-OUT-STATUS-GOOD`
  — a shared date-validation utility. **`SYH208` is NOT present in `upload/` — BLOCKED**; its exact
  validation logic cannot be confirmed, only that it's invoked with an input date and returns a
  good/bad status.
  `EXEC SQL INCLUDE DFHRESP`, `EXEC SQL INCLUDE SQLCA`.
- **Main flow (CONFIRMED):** `PROCEDURE DIVISION` → `0010-INIT` (timestamp fetch, error `61210` on
  SQL failure) → `0015-VALIDATE` (product# required, error `61201`; effective date optional —
  defaults to current date, or if supplied is checked via `0018-VALIDATE-DATE`/`SYH208`, error
  `61202` if invalid; `OMGEXPL-ROLLUP-COST-SW` must be `Y`/`N`/space/low-values, error `61204`;
  `OMGEXPL-REQUEST-TYPE` must be `S`/`L`/`C`/space — space defaults to `WS-REQUEST-DEFAULT='C'`,
  anything else is error `61203`) → if no validation error, `0020-PROCESS` = `1000-MOVE-INP-OMGPK`
  → `2000-LINK-TO-EXPLODER` (`EXEC CICS LINK PROGRAM(A6W015U-PROGRAM='A6O015U')
  COMMAREA(A6W015U-PARM-LIST)`) → on `DFHRESP-NORMAL`, `3000-MOVE-TO-OMGEXPL` (copies `OMGPK`
  header fields into `OMGEXPL`, then `PERFORM VARYING SUB` over up to `WS-MAX-ITEMS-IN-PACK = 700`
  raw component rows, dispatching per request type to `3200-PROCESS-STRUCTURE` /
  `3300-PROCESS-LEVEL` / `3400-PROCESS-COMPONENT`, each of which distinguishes sub-pack rows
  (`OMGPK-COMP-PROD-TYPE = 'S'`) from level-1 (`'1'`) and level-2 (`'2'`) component rows, using
  `4000-FIND-PARENT-PROD` to resolve a level-2 component's containing sub-pack (looked up in a
  local `WS-SUB-PACK-ARRAY OCCURS 300`) and multiplying the component's quantity by the parent
  sub-pack's own quantity for level-2 rows, per the `NK1209` fix).
- **Errors:** CICS `DFHRESP-PGMIDERR` → `'A6U15 TRANSACTION IS UNAVAILABLE'` (legacy-name message,
  same pattern as §1.2/§1.7). Other non-zero RESP → `'A6U15 LINK ERROR '`.
- **Possible length mismatch (CONFIRMED, flagged, not independently re-verified):**
  `WS-LENGTH-OF-COM-AREA PIC 9(05) VALUE 24000` (line 77, used only to set `WS-LENGTH-TO-PASS`,
  which is itself never referenced again in the paragraphs read) vs. the actual `LINKAGE SECTION`
  `01 DFHCOMMAREA. 05 COMMAREA PIC X(23367)` (line 129) — a 633-byte difference between the
  constant and the declared COMMAREA size, structurally the same *kind* of discrepancy already
  flagged for `OMGPR` in §5 (a hardcoded length constant not matching the actual record size).
  Since `WS-LENGTH-TO-PASS` doesn't appear to be used for anything beyond being set here, this one
  looks lower-risk than the `OMGPR` case, but is recorded for completeness.
- **Called by (CONFIRMED):** `A6O011U` (`9015-LINK-TO-EXPLODER`, paragraph `0200-EXPLODE`, §1.3).
- **Calls out (CONFIRMED):** `A6O015U` — **NOT present in `upload/` — new BLOCKED item.**

### 1.9 `CUP120` — JIT-adjustment subroutine (shell only)
- **CONFIRMED, but the file is materially incomplete.** `IDENTIFICATION DIVISION. PROGRAM-ID.
  'CUP120'.` (note the literal quotes around the program name in the source itself — CONFIRMED,
  unusual but harmless COBOL). The entire body of the file, after a comment header, is a single
  line: `-INC CUS120`. The header comment explains why: "THIS IS PART OF A SET OF THREE MODULES:
  CUS120: THE BODY OF THE PROGRAM, INCLUDED IN THE FOLLOWING: CUP120: A DB2 SUBROUTINE FOR ONLINE
  USE (THIS PGM), CUP121: A DB2 SUBROUTINE FOR BATCH USE... TO MAINTAIN THIS PROGRAM, FIRST UPDATE
  THE CUPS120 [sic] THEN RECOMPILE BOTH THE ONLINE AND THE BATCH PROGRAMS."
- **What this means (CONFIRMED interpretation):** `-INC` is a mainframe source-library
  include/copy directive (library-manager syntax, e.g. Panvalet/Librarian-style — distinct from
  COBOL's own `COPY` verb), pulling the actual `PROCEDURE DIVISION` logic from a separately
  maintained member `CUS120` at compile time. `CUP120.CBL` as supplied therefore contains **no
  executable logic of its own** — everything CUP100 relies on when it does
  `CALL 'CUP120' USING WS-DUMMY, WS-DUMMY, SQLCA, CUR120` (§1.5) lives in `CUS120`, which is
  **still NOT present in `upload/` — still BLOCKED**, along with its likely batch twin `CUP121`
  (referenced only in this comment, not itself called from anywhere in `upload/`).
- **Net effect on the T001 blocker list:** supplying `CUP120.CBL` does **not** resolve the original
  blocker (§7/§9) of "what does the JIT-adjustment subroutine compute" — it only narrows the
  missing piece from "the whole subroutine" to "the one library member (`CUS120`) that is its real
  body." Flagged clearly here so this isn't mistaken for a resolved dependency.

---

## 2. Copybook Inventory

| Copybook | Owning table / purpose (CONFIRMED from header/fields) | Used by (CONFIRMED) |
|---|---|---|
| `OMGPR.CPY` | Central pricer parameter+result record `OMGPR`, "REUSE-OMNI_2.0_PRICING_WS". Input fields (division, account, vendor/product, UOM, pricing date, JIT/pack/fee overrides) + output fields (error, product/contract/price-list/sell/cost-adjustment/JIT-adjustment data, ~2022-added fee-component fields for SurgiTrak/BreakBulk/ApplyLabel/PANDAC/StdFreight/markup groups). | `A6X01`, `A6O011U`, `A6U01`, `CUP100` (`EXEC SQL INCLUDE OMGPR`) |
| `RWDAT.CPY` | Generic RescueWare date-arithmetic work area (day/month tables, multiple date-format redefinitions, leap-year table). No business fields. | `A6U01` |
| `OMGEXPL.CPY` | Pack/kit explosion request (`OMGEXPL-PACK-PRODNO`, effective date, request-type 88-levels `GET-COMP-ONLY`/`GET-LEVL-ONLY`/`GET-STRUCTURE`) and result (`OMGEXPL-COMP-INFO OCCURS 700`: sub-pack/component/UOM/qty/type/level, plus overhead/third-party fee amounts and error fields). | `A6O011U` (as `A6W012U-PARM-LIST`, exchanged with `A6O012U`) |
| `A6R13.CPY` | Parm/result for "SUBROUTINE A6U13 & A6U13B" (comment) — product-category lookup: in (vendor, product, UOM, branch), out (`S-PROD-CATEGORY`, `BASE-UOM`, `CONV-FACTOR`, category eff/exp dates, `INV-CLASS`) + generic response-flag/error block. | `A6O011U` (as `A6W013U-PARM-LIST`, exchanged with `A6O013U`); `A6O013U` itself (as `WS-WHAT-TO-PASS`, its own DFHCOMMAREA shape) |
| `A6R16.CPY` | Parm/result for "SUBROUTINE A6U16 & A6U16B" — product/category detail lookup with a requirement-type switch (`DEFAULT-INFO`/`GET-ALL-INFO`/`GET-PROD-INFO`/`GET-CAT-INFO`); out includes `PROD-TYPE`, category, base UOM, conv factor, inv class + generic response-flag/error block. | `A6O010U` and `A6O013U` (both as `A6W016U-PARM-LIST`, exchanged with `A6O016U`); `A6O016U` itself (as `WS-WHAT-TO-PASS`, its own DFHCOMMAREA shape — `A6O016U` is the program that actually owns/populates this record) |
| `A6R10.CPY` | Parm/result for "SUBROUTINE A6U10 & A6U10B" — product-type lookup: in (vendor, product), out (`PROD-DESC`, `PROD-TYPE`, `PROD-STATUS`, eff/exp dates) + generic response-flag/error block. | `A6X01`, `A6O010U` |
| `CUVFEEP.CPY` | `CUTFEE_PRICE` table record — company/customer/ship-to/fee-short-code/fee-name/fee-type (88-levels per-line/qty/order/pct-sales/pct-cost)/fee-pct/fee-amt/SKU/billing-freq (88-levels daily/daily-sep/monthly-auto/monthly-manual). Added 2022, WR 22-9470. | `CUP100`, `A6U01` |
| `CUVPRCMP.CPY` | `CUTPRICE_COMPONENT` table record — company/customer/ship-to/component-short-code/name/SKU/billing-freq/cust-order-nbr/eff-exp dates. Added 2022, WR 22-9470. | `CUP100` |
| `SACIDASN.CPY` | `SA-CID-ASSN` sell-assignment-by-customer-ID record: buy-group (`SACIDASN-I-BAS-SELL`), customer nbr, sell-assignment eff/exp dates (with a full redefinition into MM/DD/YYYY sub-fields). | Not referenced by name in any of the four programs read in full or in the `A6U01` structural scan — **no confirmed consumer in this upload set.** Possibly used by a caller of `A6U01` not supplied, or superseded. Flag for T002 follow-up. |
| `A6W001.CPY` | NDP web-service parm record: program/calling-program id, select fields (account, ship-to, vendor-product, UOM, pricing date), output fields prefixed `A6W001-WEB-*` (agreement id/name, current C-plus-pct, error app/nbr/msg, expiration date, GP-pct, NDP price, price-list name, sell-group info/name/type). Added 2014, tag `NK0518`/label says 2014 but field comment says "01/01/2014 NKIMBO PRICING STRATEGY: WEB". | `A6U01` (`COPY A6W001`, exchanged by dynamic `CALL` with `A6P001WB`) |
| `CUVPRCMP.CPY`, `CUVFEEP.CPY` | (see above) | |
| `HCOVDACT.CPY` | `HC_OVRD_GROUP_ACCOUNT` — group id, division, account, start/expire timestamp (X(23)), user id. Added 2021, IR 20-0065 Phase 2. | `A6U01` (`COPY HCOVDACT`) |
| `HCOVDCID.CPY` | `HC_OVRD_GROUP_CUSTOMER` — group id, customer id (COMP-3), start/expire timestamp, user id. Same project. | `A6U01` (`COPY HCOVDCID`) |
| `HCOVDGPD.CPY` | `HC_OVRD_PROD_GROUP` (detail) — group-override id, start/expire timestamp, override flag, user id. Same project. | `A6U01` (`COPY HCOVDGPD`) |
| `HCOVDGRP.CPY` | `HC_OVRD_PROD_GROUP` (header) — group id, comment, header start/expire timestamp, user id. Same project. | `A6U01` (`COPY HCOVDGRP`) |
| `HCOVDPRD.CPY` | `HC_OVRD_PRODUCT` — group id, vendor, product, cost start/expire, sell start/expire, override flag/type/percent (COMP-3)/sell-price (COMP-3), sell UOM, user id. Same project. | `A6U01` (`COPY HCOVDPRD`) |
| `CUCIDFRT.CPY` | `CU_CID_FREIGHT_FLAG` table record — customer nbr (COMP-3), eff/exp date, freight flag, group-sanction/non-sanction/individual/non-contract/custom exemption flags, buy-group (COMP). | `CUP100` (inline `01 CU-CID-FREIGHT-FLAG`, not via `EXEC SQL INCLUDE`) |
| `A6G01.CPY` | Filler-only placeholder: `10 A6G01-C. 15 FILLER PIC X(1789) VALUE SPACES.` No named fields — looks like a reserved/legacy working-storage block. | `A6U01` (`COPY A6G01`) |

---

## 3. Call Graph

```text
(unsupplied online driver) --?-->  CUP100  --CALL-->  CUP120 (shell) --INC(compile-time)-->  CUS120   [MISSING — BLOCKED]
                                       (populates OMGPR-DATA-FROM-CUP100,
                                        JIT/fee/exemption/rounding fields
                                        ahead of per-item pricing)

(unsupplied online driver) --?-->  A6X01
        A6X01 --CICS LINK-->  A6O010U --CICS LINK-->  A6O016U --SQL SELECT-->  VNG02, VNG06, ING01, VNG05
        A6X01 --CICS LINK-->  A6U01
        A6X01 --CICS LINK-->  A6O011U (only when product type = 'O', i.e. a kit)
                A6O011U --CICS LINK-->  A6O013U --CICS LINK-->  A6O016U   (same shared DAO as above)
                A6O011U --CICS LINK-->  A6O012U --CICS LINK-->  A6O015U   [MISSING — BLOCKED]  (raw explosion)
                A6O011U --CICS LINK-->  A6U01     (once per exploded component, + sell/JIT passes)
                A6O011U --SQL SELECT-->  VNG02, VNG05 (alt-UOM conversion factors)

        A6U01 --dynamic CALL (content of A6W001-PROGRAM = 'A6P001WB')-->  A6P001WB   [MISSING — BLOCKED]
                (NDP / e-commerce net-delivered-price web service; gated on OMGPR-ECOMMERCE-PRICING)
        A6U01 --~363 embedded EXEC SQL SELECT/cursor operations across ~137 INCLUDE'd tables-->  DB2
```

- **Nodes present in `upload/` with full source:** `A6X01`, `A6O010U`, `A6O011U`, `A6U01`,
  `CUP100`, `A6O013U`, `A6O016U`, `A6O012U`. `CUP120` is present as a *file* but is an empty
  compile-time shell (§1.9) — treat it as "present but non-substantive," not as a resolved node.
- **Nodes referenced but NOT present in `upload/` (all BLOCKED for behavior):** `A6O015U` (raw
  pack-explosion engine, called by `A6O012U`), `A6P001WB`, `CUS120` (the real body behind the
  `CUP120` shell), `A6O011UB` (sibling of `A6O011U`, mentioned in its header comment but never
  invoked from anywhere in this upload set), `CUP121` (batch sibling of `CUP120`, mentioned only in
  `CUP120`'s header comment), `CUP110` (mentioned only in an `A6U01` comment), and whatever online
  transaction/driver invokes `CUP100` and `A6X01`.
- **A second shared-DAO pattern, same shape as `A6O016U` (CONFIRMED):** `A6O012U`→`A6O015U` mirrors
  `A6O010U`/`A6O013U`→`A6O016U` — a thin, validation-only online wrapper (`A6O012U`) delegating the
  actual data-producing work to one lower-level program (`A6O015U`) that is itself not supplied.
  This two-tier "thin online wrapper over a shared DAO/engine" pattern now shows up twice in this
  codebase and should be expected to recur if further programs are supplied later.
- **No `EXEC CICS XCTL` was found anywhere in `upload/`** — every cross-program hop is either
  `EXEC CICS LINK` (return-to-caller semantics) or a batch-style `CALL` (`CUP100`→`CUP120`,
  `A6U01`→`A6P001WB`). CONFIRMED by exhaustive grep across all nine programs now supplied.

---

## 4. SQL / DB2 Dependency Inventory

**No `UPDATE`, `INSERT`, or `DELETE` was found in any of the five programs — CONFIRMED. Every
`EXEC SQL` in this upload set is a `SELECT` (including cursor `DECLARE`s). The pricer, as supplied,
is read-only against DB2.**

### 4.1 Tables referenced directly by SQL text (`FROM`/`INTO`), with copybook present in `upload/`
| Table | Copybook supplied | Confirmed consumer(s) |
|---|---|---|
| (none of `CUP100`'s or `A6O011U`'s or `A6U01`'s inline-SQL tables have a *dedicated* DCLGEN copybook file in `upload/` — the closest are the OMGPR-adjacent business-record copybooks below, which are `EXEC SQL INCLUDE`d directly rather than declared inline) | | |
| `CU_CID_FREIGHT_FLAG` | `CUCIDFRT.CPY` (CONFIRMED — matches column names used in the SQL) | `CUP100` (`A450-READ-CID-FREIGHT-FLAG`) |
| `CUTFEE_PRICE` | `CUVFEEP.CPY` (CONFIRMED) | `CUP100` (`4000-SQL-SELECT-CUTFEE-PRICE`) |
| `CUTPRICE_COMPONENT` | `CUVPRCMP.CPY` (CONFIRMED) | `CUP100` (`5000-SQL-SELECT-PRICE-COMP`) |

### 4.2 Tables referenced whose DCLGEN copybook is NOT supplied — BLOCKED
**`CUP100`:** `CUG40`, `CUG41`, `CCG25`, `BGG03`, `BGG23`, `BGG24`, `CUG02`, `CUG03`, `CUG06`,
`CUG07`, `CUG10`, `CUG11`, `CUG53`, `CUG17`, `CUTMST`, `CUR120` (also its consuming record,
`CUR120.CPY`, is not supplied even though CUP100 calls a `CUP120` subprogram that presumably
populates it).

**`A6O011U`:** `VNG02`, `VNG05` (referenced only via inline `EXEC SQL SELECT`, no `EXEC SQL
INCLUDE`; column names visible in the SQL text but not the full DCLGEN).

**`A6O016U`** (referenced only via inline `EXEC SQL SELECT`, no `EXEC SQL INCLUDE` for any of
these — SQL-text-level column shapes CONFIRMED, full DCLGEN still BLOCKED): `VNG02`
(`I_VENDOR`, `I_VND_PRODUCT`, `C_PRODUCT_TYPE`, `C_VND_PROD_BASE_UM`), `VNG06` (`I_VENDOR`,
`I_VND_PRODUCT`, `S_PROD_CATEGORY`, `D_CAT_PROD_EFFECT`, `D_CAT_PROD_EXPIRE` + a null-indicator
column), `ING01` (`I_DIVISION`, `I_VENDOR`, `I_VND_PRODUCT`, `C_DIV_INV_PRC_LVL`, `F_STOCK_ITEM`,
`A_DIV_INV_FREIGHT`, `C_INV_CLASS`, `C_INV_SUB_CLASS`), `VNG05` (`I_VENDOR`, `I_VND_PRODUCT`,
`C_VD_PRD_ALT_UM`, `A_VD_PRD_ALT_UMF`). This corroborates and extends the `VNG02`/`VNG05` column
list already seen from `A6O011U`'s own inline SQL (§1.6/§1.3) and adds `VNG06`/`ING01` as two more
tables with SQL-text-level (not full-DCLGEN) confirmation.

**`A6O012U`:** `OMGPK` (the raw pack-explosion record exchanged with the still-missing `A6O015U`)
— **NOT present in `upload/` — BLOCKED**, though extensively INFERRED in shape from usage (§1.8).
Also `SYR000`, `SYR208`, `SYH208` (all `COPY`, not `EXEC SQL INCLUDE` — shared/system utility
copybooks, `SYH208` invoked as inline procedural logic rather than data) — **all three NOT present
in `upload/` — BLOCKED**, purpose unconfirmed beyond "date validation" for `SYH208`.

**`A6U01`** (~134 `EXEC SQL INCLUDE` DCLGEN targets, grouped by family; **all BLOCKED** — field
layouts cannot be confirmed):
- **`CCG*` (contract) family:** CCG01, CCG03, CCG04, CCG05, CCG06, CCG07, CCG09, CCG10, CCG11,
  CCG13, CCG14, CCG15, CCG16, CCG21, CCG25, CCG27.
- **`CUG*` (customer) family:** CUG03, CUG06, CUG07, CUG08, CUG09, CUG10, CUG11, CUG12, CUG13,
  CUG18, CUG19, CUG20, CUG21, CUG22, CUG23, CUG24, CUG25, CUG26, CUG27, CUG29, CUG31, CUG33,
  CUG34, CUG55, CUG56, CUG57, CUG60, CUG61, CUG62, CUG63, CUG64, CUG65.
- **`BGG*` (buy group) family:** BGG01, BGG02, BGG03, BGG10, BGG11, BGG19, BGG20, BGG25.
- **`SAG*` (sell assignment) family:** SAG01 through SAG26 (all 26).
- **`VNG*` (vendor) family:** VNG01, VNG02, VNG03, VNG04, VNG05, VNG06, VNG07, VNG14, VNG15,
  VNG16, VNG19, VNG20, VNG21, VNG22, VNG23, VNG24, VNG31, VNG32, VNG33, VNG34, VNG35, VNG36.
- **`VN_CID_*` / `CID_*` (2021 "Pricer Enhancement Phase 2", tag `BM0421`) adjustment family:**
  VN_CID_VN_SURCHARGE, VN_CID_PC_SURCHARGE, VN_CID_VN_COSTADJ, CID_OVRHD_CHRG_ADJ,
  CID_PREPAY_DEDUCT, CID_DELIV_ADJ, CID_RISK_PREM_ADJ, CID_FIN_CHRG_ADJ, CID_NON_CONT_ADJ,
  VN_CID_ADJ_FEE, VN_CID_ADJ_FEE_MEDC, VN_CID_ADJ_FEE_PCAT, VN_CID_VN_FREIGHT.
- **`CU_*_NONSANC_RATING` (2021 "Phase 2", tag `BM0821`) family:** CU_AC_VEND_NONSANC_RATING,
  CU_BG_VEND_NONSANC_RATING, CU_CID_VEND_NONSANC_RATING, CU_CO_VEND_NONSANC_RATING,
  CU_AC_CONT_NONSANC_RATING, CU_BG_CONT_NONSANC_RATING, CU_CID_CONT_NONSANC_RATING,
  CU_CO_CONT_NONSANC_RATING, CU_AC_PROD_NONSANC_RATING, CU_BG_PROD_NONSANC_RATING,
  CU_CID_PROD_NONSANC_RATING, CU_CO_PROD_NONSANC_RATING.
- **DC Formulary / non-OM-select family (tag `NK0813`, `BM1022`):** IN_NON_OMSELECT_PART,
  INPRDSSC, INSSC, SASSC, SASSCGM, SASSCCP, SASSCVP.
- **Misc:** A9G36, ING01, DVG01, BG_FRT_VAR_GRP, BG_FRT_VAR_VEND, BG_FRT_VAR_PROD, SYMSCSW.CPY
  (note the literal `.CPY` suffix inside the `INCLUDE` statement itself, CONFIRMED at line 2081 —
  unusual and possibly a copy-paste artifact in the source, left as-is).
- **Explicitly present as data but NOT via `EXEC SQL INCLUDE`:** `CUTADR` (`FROM CUTADR`, line
  17331) — table name visible in SQL text, no DCLGEN include found for it, still BLOCKED.

**Also present but *commented out* (NOT active — do not carry forward as a dependency):**
`BGG12`, `BGG13`, `BGG14`, `BGG15`, `SYV20` (all `A6U01`), and `DFHRESP` in `A6U01` (commented,
consistent with `A6U01` never issuing a CICS LINK itself).

### 4.3 Cursors declared with `OPTIMIZE FOR 1 ROW` (CONFIRMED, `A6U01`, lines 2104–2397)
`A6Z01_C_CUG07`, `A6Z01_C_CUG11`, `A6Z01_S_CUG07`, `A6Z01_S_CUG11` (buy-group tier/priority
resolution for customer vs. account), `ACT_PRD_CAT_CSR`, `ACT_VEN_CSR`, `CUS_PRD_CAT_CSR`,
`CUS_VEN_CSR` (buy-group override chase by account/customer × vendor/category), `MIN_GRP`,
`MIN_GRP_CONT` (added `DP0620`, an `EXISTS`-based variant of `MIN_GRP`), `MIN_INDV`
(group/individual contract-line resolution — these three encode a **priority hierarchy between
group-level and individual-level contracts** that should be preserved exactly in migration; full
rule extraction is T002 scope), `VNG05_CUR` (alt-UOM, `ORDER BY A_VD_PRD_ALT_UMF DESC`).

---

## 5. Field Inventory

`OMGPR.CPY` (959 lines) is the dominant data-interchange record across this entire codebase — it
is the `DFHCOMMAREA` payload for `A6X01`↔`A6O010U`/`A6O011U`/`A6U01`, and the working record inside
`CUP100`. A pre-existing field-level inventory of it was supplied as
`upload/OMGPR_Field_Inventory.xlsx` (3 sheets: "Field Inventory", "Condition Values (88-levels)",
"Summary & Findings"). This workbook was opened and its content read in full; it is treated here as
a **supplied reference artifact**, not something generated by this task.

**Workbook content (CONFIRMED by reading all three sheets):**
- **Field Inventory** (310 rows): every `OMGPR` field/group, with level number, full group path,
  `PIC` clause, `USAGE`, byte length, decimals, signed flag, byte start/end offset, `VALUE`
  default, and a proposed C# property name/type — 287 leaf (storage-bearing) fields, 22 structural
  group headers, matching the structure independently observed in `OMGPR.CPY` itself.
- **Condition Values (88-levels)** (136 rows): every 88-level condition name in `OMGPR.CPY` with
  its parent field, literal value(s), and a suggested C# enum/const name (135 total 88-levels per
  the workbook's own summary count).
- **Summary & Findings** (24 rows): aggregate stats (99 COMP-3 fields, 21 COMP/COMPUTATIONAL, 164
  PIC X, 0 REDEFINES/OCCURS inside `OMGPR.CPY` itself) plus one **flagged data-integrity risk**:

  > **Record-length discrepancy (from the workbook, cross-checked against the programs read for
  > this task):** the workbook computes `OMGPR-C`'s total leaf-field length as **1,807 bytes**,
  > but every COMMAREA/length constant seen in the actual programs is **1,789 bytes** —
  > `A6O011U.CBL` line 207 (`05 COMMAREA PIC X(1789)`), `CUP100 (1).CBL` line 341 (same), `A6U01`
  > line 2410 (same) and line 2416 (`MOVE 1789 TO WS-LENGTH-TO-PASS`), and `OMGPR.CPY` itself
  > (`OMGPR-PARM-LENGTH PIC S9(4) VALUE +1789`). The workbook attributes the 18-byte gap to the
  > trailing `FILLER` field being shrunk across several change batches (`DP0722`/`SJ0821`/
  > `BM0821`/`BM0421`/`NK0618`, all CONFIRMED present as comments in `OMGPR.CPY`) without a
  > matching increase to the hardcoded length constants. **This task did not independently
  > recompute the byte-for-byte sum — the discrepancy is reported here as CONFIRMED-by-the-supplied-
  > workbook, not independently re-verified, and should be spot-checked against the actual
  > mainframe COMMAREA size before being relied on.** If real, it implies the last ~18 bytes of
  > `OMGPR-C` (the trailing `OMGPR-A-MKP-CUS-MC`/`OMGPR-FILLER` fields added under `DP0722`) may be
  > silently truncated at runtime today.

No equivalent pre-built field inventory exists for any other copybook in `upload/`; those are
small enough (12–252 lines) that their full field lists are captured directly in this document's
copybook read-through (§2) and in the copybook files themselves.

---

## 6. Error / Exception Catalog

### 6.1 `OMGPR-Q-ERROR-CODE` — 88-level severity buckets (CONFIRMED, `OMGPR.CPY`)
| Code | 88-level name | Meaning |
|---|---|---|
| 0 | `OMGPR-PRICER-NO-ERROR` | success |
| 1 | `OMGPR-UM-NOT-FOUND` | unit of measure not found |
| 2 | `OMGPR-ACCT-NOT-FOUND` | account not found |
| 3 | `OMGPR-ACT-CUST-NOT-FOUND` | active customer not found |
| 4 | `OMGPR-PRC-LIST-NOT-FOUND` | price list not found |
| 5 | `OMGPR-SHIP-TO-NOT-FOUND` | ship-to not found |
| 6 | `OMGPR-PROD-NOT-FOUND` | product not found |
| 7 | `OMGPR-VENDOR-NOT-FOUND` | vendor not found |
| 8 | `OMGPR-DIV-NBR-MISSING` | division number missing |
| 9 | `OMGPR-ACCT-NBR-MISSING` | account number missing |
| 10 | `OMGPR-VEND-NBR-MISSING` | vendor number missing |
| 11 | `OMGPR-PROD-NBR-MISSING` | product number missing |
| 40 | `OMGPR-PRC-METHOD-INVALID` | pricing method invalid |
| 41 | `OMGPR-BG-PRC-PARM-NOT-FND` | buy-group pricing parm not found |
| 70 | `OMGPR-DB2-FATAL-ERROR` | DB2 fatal error |
| 1–39 | `OMGPR-PRICER-WARN-ERROR` | warning-severity range |
| 40–69 | `OMGPR-PRICER-MEDIUM-ERROR` | medium-severity range |
| 70–99 | `OMGPR-PRICER-FATAL-ERROR` | fatal-severity range |

### 6.2 `OMGPR-Q-ERROR-NBR` — program-assigned specific error numbers actually seen in source (CONFIRMED)
- **`CUP100`:** 102 (`A100-PRO-PASS-DATA`, division missing), 103 (`A100-PRO-PASS-DATA`, account
  missing), 106 (`A200-SEL-ACCOUNT`, account not found), 108 (`A310-SEL-ACTIVE-CUST`, active
  customer not found), 120 (`0510-SELECT-CUG17`, ship-to suffix not found), 2 (`A400-GET-JIT-ADJ`,
  JIT-adj subprogram `CUP120` returned abend/error).
- **`A6U01`:** paragraph `7185-PRO-PASSED-DATA-010` (input-edit paragraph, CONFIRMED lines
  12187–12264) independently assigns 102 (division missing, message `'#102 - MUST PASS DIVISION
  NUMBER TO A6X01'`), 103 (account missing, `'#103...'`), 104 (vendor missing, `'#104...'`), 105
  (product missing, `'#105...'`) — note these are the **same numeric codes 102/103 that `CUP100`
  also uses for its own division/account checks**, i.e. `OMGPR-Q-ERROR-NBR` is not a globally
  unique code space across programs, it is reused per-program (CONFIRMED, worth flagging for any
  downstream error-code enum design). The same paragraph also has a missing-pricing-date check
  whose message reads `'#135 - MUST PASS DATE TO A6X01'` but whose `OMGPR-Q-ERROR-NBR` is set to
  **102** again (line 12262) rather than 135 — CONFIRMED as written in source; this looks like a
  copy-paste bug (the message text and the error number disagree) but is preserved here verbatim
  per the "do not silently correct legacy behavior" instruction. Elsewhere in `A6U01`: 95, 96, 112,
  115, 116, 117, 118, 119, 121, 122, 124, 130, 132, 135, 601, 602, 603 (each CONFIRMED present via
  source search; full per-code precondition/paragraph mapping beyond what is noted above is T002
  decision-table-extraction scope, not T001 inventory scope — flagged here as an inventory of
  *which codes exist*, not *when each fires*).
- **`A6O011U`:** 3000 (pack cost/adjustment amount exceeds `WS-AMOUNT-LIMIT`), 4000 (sell/sell-adj
  amount exceeds limit).
- **`A6U01` NDP path:** whatever numeric value `A6P001WB` returns in `A6W001-WEB-ERROR-NBR` is
  passed straight through to `OMGPR-Q-ERROR-NBR` (except `404`, which is treated as "not found,
  not fatal," and `0`/`ZEROES`, which is success) — the catalog of NDP-specific codes is
  **BLOCKED** (owned by `A6P001WB`, not supplied).
- **`A6O016U`** uses a separate message-number space on `A6W16-ERROR-MESSAGE` (not
  `OMGPR-Q-ERROR-NBR` — this program never touches `OMGPR` at all): `61601`/`61604`
  (vendor/product not provided), `61602` (SQL error, `VNG02`), `61603` (product not found,
  `VNG02`), `61605` (category not found, `VNG06`), `61606` (reused for three different failures —
  SQL error on `VNG06`, SQL error on `ING01`, *and* "UOM not found" on `VNG05` — see §1.6), `61607`
  (SQL error, `VNG05`).
- **`A6O013U`** reuses the same `A6R13`/`A6R16` message-number space as `A6O010U` but with its own
  prefix: `61301`/`61304` (vendor/product not provided) — same family as `A6O010U`'s
  `61001`/`61004` and `A6O016U`'s `61601`/`61604`, confirming a program-specific `6<transid>nn`
  numbering convention used consistently across this DAO tier.
- **`A6O012U`** uses its own `OMGEXPL-ERROR-NUMBER` space: `61201` (product# not provided), `61202`
  (effective date invalid), `61203` (request type invalid), `61204` (rollup-cost switch invalid),
  `61210` (SQL error fetching current date).

### 6.3 `CUP100` internal DB-operation error numbering (CONFIRMED)
Each SQL paragraph that fails with an unexpected `SQLCODE` tags `WS-DB-ERROR-NBR` with a
paragraph-specific constant (10=CUG03, 20=CUG02, 25=CUTMST, 30=CUG53, 35=CU_CID_FREIGHT_FLAG,
40=CUG17, 50=CUG10/CUG11, 60=CUG06/CUG07, 70=BGG23, 80=BGG24, 90=BGG03, 220=CUTFEE_PRICE,
1000=CCG25) and formats a message `'#nnn ... IN CUP100 CAUSED RTN CODE nnnnnn'` via
`WS-DB-OPERATION-MSG`, always paired with `OMGPR-Q-ERROR-CODE = WS-DB2-FATAL-ERROR-IND (70)`.

### 6.4 `A6R10`/`A6R13`/`A6R16` generic response-flag pattern (CONFIRMED, identical shape in all three copybooks)
88-level `GENERAL-RESPONSE-FLAG`: `'A'` = `-ABEND`, `'E'` = `-ERROR`, `'W'` = `-WARNING`, plus a
5-char `GENERAL-ERROR-NUMBER` and an 85-char `ABEND-MSG` (80-char message + 5-char SQLCODE). Used
identically by `A6O010U` (`A6W10`/`A6W16`), `A6O013U` (`A6W13`, propagated up from `A6W16` in its
own `0020-PROCESS`), and `A6O016U` (`A6W16`, the program that actually sets these flags in the
first place). No equivalent consumer of `A6W13`'s flag was found active in `A6O011U`'s error paths
(it reads `A6W13-ERROR-SQLCODE`/`A6W13-ERROR-MESSAGE` directly in `9998-BUILD-ERROR-MSG` rather
than branching on the abend/error 88-levels — flagged as an inconsistency, not corrected).

### 6.5 CICS LINK failure handling (CONFIRMED, identical 3-way pattern in `A6X01`, `A6O010U`, `A6O011U`, `A6O012U`, `A6O013U`)
`EVALUATE TRUE` on `WS-XCTL-CICS-RESP`: `DFHRESP-NORMAL` → continue; `DFHRESP-PGMIDERR` →
`'<program> TRANSACTION IS UNAVAILABLE'` (target program is not installed/available in CICS);
`OTHER` → generic `'ERROR-UNABLE TO LINK TO <program>'` / `'CICS LINK ERR '` with the numeric RESP
captured. `A6U01` does not use this pattern (it issues no `EXEC CICS LINK`). `A6O016U` also does
not use it (it issues no `EXEC CICS LINK` either — it is the bottom of this call chain).

---

## 7. Consolidated Missing-Dependency List (BLOCKED)

*Updated after `A6O012U.CBL`, `A6O013U.CBL`, `A6O016U.CBL`, `CUP120.CBL` were supplied. Items
struck through below were resolved by that batch; new items surfaced by reading them are added.*

**Programs referenced but not supplied:**
- ~~`A6O016U`~~ — **RESOLVED**, supplied and read, see §1.6.
- ~~`A6O013U`~~ — **RESOLVED**, supplied and read, see §1.7.
- ~~`A6O012U`~~ — **RESOLVED**, supplied and read, see §1.8.
- `CUP120` — **PARTIALLY RESOLVED**: the file is supplied and read (§1.9), but it is a compile-time
  shell with no logic of its own.
- `A6O015U` — **NEW.** The raw pack-explosion engine `A6O012U` delegates to. Not supplied.
- `CUS120` — **NEW (replaces the old `CUP120` blocker at one more level of depth).** The actual
  JIT-adjustment logic `CUP120` includes at compile time via `-INC CUS120`. Not supplied.
- `A6O011UB` — sibling/batch twin of `A6O011U`, mentioned in its header comment, never invoked from
  anywhere in this upload set. Not supplied.
- `CUP121` — **NEW.** Batch twin of `CUP120`, mentioned only in `CUP120`'s header comment. Not
  supplied.
- `A6P001WB` — NDP web-service program `A6U01` dynamically `CALL`s. Not supplied.
- `CUP110` — comment-only reference in `A6U01`. Not supplied.
- Whatever online transaction driver invokes `CUP100` and `A6X01`. Not supplied.

**Copybooks/DCLGENs referenced but not supplied:**
- `CUR120` — still BLOCKED (despite being consumed field-by-field in `CUP100`; `CUP120`/`CUS120`
  would be the program that populates it, and `CUS120` is still missing).
- `VNG02`, `VNG05`, `VNG06`, `ING01` — still BLOCKED for a *full* DCLGEN, though `A6O016U` (§1.6)
  now gives SQL-text-level column confirmation for the specific columns these five programs
  (`A6O011U`, `A6O016U`) actually use.
- `OMGPK` — **NEW.** Raw pack-explosion record exchanged between `A6O012U` and the missing
  `A6O015U`. Extensively INFERRED in shape from usage, but not a supplied file.
- `SYR000`, `SYR208`, `SYH208` — **NEW.** Shared/system utility copybooks `COPY`'d by `A6O012U`;
  `SYH208` appears to be an inline date-validation routine. Purpose beyond that is unconfirmed.
- `CUTADR`, and the ~134-table family enumerated in §4.2 (`A6U01`'s DCLGEN includes) — unchanged,
  still BLOCKED.
- `SACIDASN` IS supplied but still has no confirmed consumer in this upload set (§2) — unchanged.

**Behavioral consequence for later tasks:** any rule that depends on the *content* of the programs
still missing (`A6O015U`'s explosion algorithm, `CUS120`'s JIT-adjustment computation, `A6P001WB`
NDP's e-commerce sell-price computation) can only be characterized at the level of "what the
calling program does with the response fields it receives," never at the level of "how that
response is computed." The scope of what *is* now traceable end-to-end has grown substantially:
the full online product/category/UOM lookup chain (`A6O010U`/`A6O013U` → `A6O016U` → `VNG02`/
`VNG06`/`ING01`/`VNG05`) and the pack-explosion request-shaping layer (`A6O011U` → `A6O012U`) are
now CONFIRMED end-to-end except for the innermost data source in each case (the DCLGEN row content,
and `A6O015U`'s explosion algorithm, respectively). Per CLAUDE.md, none of the still-missing pieces
may be inferred as fact in any downstream rule extraction.

---

## 8. Assumptions

1. The two filename irregularities (`A6O010U .CBL` trailing space, `CUP100 (1).CBL` parenthetical
   suffix) are upload/export artifacts, not COBOL source content — the `PROGRAM-ID` inside each
   file is authoritative and was used for all cross-references in this document.
2. Change-tag prefixes in column 1–6 (e.g., `SA0703`, `NK0615`, `BM0821`, `SJ1022`, `MFMIGR`) were
   read as chronological/authorship markers to help date functionality, per the standard convention
   visible in every file's revision-history comment block; they were not independently verified
   against any change-management system (none was supplied).
3. Where a paragraph's business intent is stated in an inline comment (e.g., "LOW VELOCITY (LV)
   LOGIC IS...") it is reported as CONFIRMED-from-comment, which is not the same confidence level
   as CONFIRMED-from-executable-logic; full logic tracing for such paragraphs is left to T002.
4. The `OMGPR_Field_Inventory.xlsx` byte-length discrepancy (§5) is reported as-is from the
   supplied workbook and was not independently recomputed field-by-field in this pass.

## 9. Blockers Requiring Resolution Before Deeper Rule Extraction

*Updated after `A6O012U.CBL`, `A6O013U.CBL`, `A6O016U.CBL`, `CUP120.CBL` were supplied.*

1. Missing DCLGEN copybooks for ~135 DB2 tables `A6U01`/`CUP100` depend on (§4.2) — needed to
   state any `WHERE`-clause precondition or output-field mapping as CONFIRMED rather than
   SQL-text-only. (Partially narrowed: `VNG02`/`VNG05`/`VNG06`/`ING01` now have SQL-text-level
   column confirmation via `A6O016U`, but still no full DCLGEN.)
2. ~~Missing source for `A6O016U`, `A6O012U`, `A6O013U`~~ — **RESOLVED**, all three supplied and
   read (§1.6–§1.8). Missing source for `A6P001WB` and, one level deeper than originally scoped,
   `A6O015U` (called by `A6O012U`) and `CUS120` (the real body of the `CUP120` shell) — needed to
   trace any rule that crosses one of these call boundaries.
3. Missing `CUR120.CPY` — `CUP100` consumes ~25 named `CUR120-*` fields with no supplied layout;
   unaffected by the `CUP120.CBL` addition since that file turned out to be an empty shell.
4. Unresolved consumer of `SACIDASN.CPY` inside this upload set.
5. The `OMGPR` record-length discrepancy (1,807 computed vs. 1,789 hardcoded) — needs mainframe-side
   confirmation of the live COMMAREA size before any migrated model can be validated against
   production data.
6. **New:** a structurally similar length discrepancy noticed in `A6O012U`
   (`WS-LENGTH-OF-COM-AREA VALUE 24000` vs. actual `COMMAREA PIC X(23367)`, a 633-byte gap) — lower
   apparent risk since the constant doesn't appear to be used downstream, but recorded per §1.8 for
   completeness and consistency with item 5.
7. **New:** missing `OMGPK.CPY` (raw explosion record between `A6O012U` and `A6O015U`) and missing
   `SYR000`/`SYR208`/`SYH208` (shared utility copybooks used by `A6O012U`, one of which —
   `SYH208` — is invoked as inline procedural logic, an unusual copybook usage pattern worth
   confirming with whoever maintains the shared-utility library before assuming it's "just"
   validation.

---

*T002 (and later tasks) should treat every paragraph/table catalogued here as the entry point for
decision-table extraction — this document intentionally stops at inventory level and does not
state calculation order, rounding stage, or rule priority except where already evident from
paragraph-naming (§1.4) or cursor ordering (§4.3).*

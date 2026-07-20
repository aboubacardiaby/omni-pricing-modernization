# DB2 Table Inventory

**What this document is:** a per-table rollup of every DB2 table/DCLGEN referenced
anywhere in `upload/`'s nine COBOL programs, aggregated from
`docs/cobol-analysis/sql-query-inventory.csv` (the companion, row-per-SQL-block artifact for this
same task). This document answers "who queries this table, how, and why" per table; the CSV
answers "what does this specific SQL block do" per block.

**Method:** every `EXEC SQL ... END-EXEC` block across all nine programs was extracted
mechanically (not read one-by-one) via a script that: (1) tokenizes each block, classifying it as
`INCLUDE` (DCLGEN pull-in), `SELECT`, `DECLARE_CURSOR`, `OPEN_CURSOR`, `FETCH_CURSOR`, or
`CLOSE_CURSOR`; (2) for query-shaped blocks, extracts the `FROM` table list, `WHERE` clause text,
`ORDER BY`, and every host variable (`:name`) referenced; (3) locates the `SQLCODE`
handling that governs each block -- which, in this codebase's dominant idiom, usually does **not**
sit inside the same small "9xxx-SQL-SELECT-010"-style helper paragraph as the `SELECT` itself, but
in the *calling* paragraph immediately after the `PERFORM` that invokes it. The script follows that
call site (locating every `PERFORM <paragraph-name>` reference) to find the real `EVALUATE
SQLCODE`/`IF SQLCODE` block. This table-level document then groups the same underlying data by
table rather than by SQL block.

**Coverage achieved (CONFIRMED, self-reported by the extraction script):** 417 total `EXEC SQL`
blocks found across the 9 programs (180 `INCLUDE`, 187 `SELECT`, 13 `DECLARE CURSOR`, 12 each of
`OPEN`/`FETCH`/`CLOSE CURSOR`, 1 unclassified). Of the 237 blocks that are actual
queries/cursor-operations (excluding `INCLUDE`, which needs no `SQLCODE` handling), **208 (88%)**
have their governing `SQLCODE` check mechanically located and captured, including the specific
`WHEN`/`IF` values tested and whether a fatal-abend path exists. The remaining 12% (mostly
`DECLARE CURSOR` entries, which are compile-time metadata with no runtime `SQLCODE` of their own,
plus a handful of `OPEN`/`CLOSE` sites whose handling paragraph could not be resolved by this
script's call-site heuristic) are marked with an empty `SQLCodeChecked` column in the CSV rather
than a guessed value -- **not CONFIRMED, and not asserted either way.**

**Two parsing bugs worth knowing about, since they affect how much to trust "no handling found"
and the `Paragraph` attribution throughout this document and its companion CSV:**
1. This codebase's dominant idiom is `PERFORM <paragraph> THRU <paragraph>-EXIT`, frequently split
   across two source lines with the `THRU` keyword ending the first line and the bare
   `<paragraph>-EXIT.` label starting the second. An early version of this extraction script
   misread that continuation label as a brand-new paragraph header and stopped scanning right
   there, which suppressed real `SQLCODE` handling in roughly two-thirds of cases before the bug
   was found (a directly-verified example, `9065-SQL-OPEN-MIN-010`'s `SQLCODE` check, was being
   cut off entirely -- coverage went from 88/237 to 202/237 once fixed).
2. A standalone `END-EXEC.` line (common after a multi-line `EXEC SQL INCLUDE ... END-EXEC`
   statement) also matches the same all-caps-hyphenated pattern used to detect paragraph headers,
   since `END-EXEC` is syntactically indistinguishable from a paragraph name by shape alone. This
   was misattributing the paragraph context of everything following such a line until a reserved-
   word exclusion list (`END-EXEC`, `END-IF`, `END-EVALUATE`, etc.) was added.

Both fixes are applied throughout this document and the companion CSV; flagged here so the
discovery process itself is part of the record, per this project's evidence-tracing conventions.

**DCLGEN status column:** `SUPPLIED` means a copybook for this table exists in `upload/` (cross-
referenced against `program-inventory.md`'s file list); `BLOCKED` means no copybook was supplied,
matching `program-inventory.md` §4.2's already-established list -- this document does not
re-litigate that finding, only restates it per table for convenience alongside the query evidence.

**A copybook-labeling error found while doing that cross-reference:** `HCOVDGRP.CPY` and
`HCOVDGPD.CPY` each carry a header comment claiming to be "COPYBOOK FOR TABLE HC_OVRD_PROD_GROUP"
-- the **same** claimed table name for both files. Matching each copybook's actual field prefixes
against the host variables used in `A6U01`'s real SQL (`HC-GROUP-*` vs. `HC-GROUP-OVRD-*`) shows
they are DCLGENs for two different real tables: `HCOVDGRP.CPY` (`01 HC-OVD-GROUP`) is actually
`HC_OVRD_GROUP_HEADER`, and `HCOVDGPD.CPY` (`01 HC-OVD-GROUP-DETAIL`) is actually
`HC_OVRD_GROUP_DETAIL`. Neither is `HC_OVRD_PROD_GROUP` (a table name that does not appear
anywhere in the SQL text read across all nine programs). This document uses the SQL-confirmed real
names; the mismatch is flagged here rather than silently corrected, since it is a genuine error in
the supplied source material worth confirming with whoever maintains these copybooks.

**Confidence key:** `CONFIRMED` = table name, consumer program/paragraph, and `WHERE`/`SQLCODE`
text mechanically extracted from source. `INFERRED` = the one-line "purpose" description per table,
which is derived from column names visible in the `SELECT`/`WHERE` text and (where available)
cross-referenced against this project's existing `docs/cobol-analysis/decision-tables/*.md` rule
extractions -- a reasonable reading, not a DCLGEN-confirmed fact, since no DCLGEN exists for the
large majority of these tables. `BLOCKED` = no DCLGEN supplied, column-level layout unconfirmable.

---

## Summary


- **187** distinct tables/DCLGEN targets referenced across all nine programs (158 referenced in a `FROM` clause this script could parse; 155 pulled in via `EXEC SQL INCLUDE`; most tables appear in both).
- **8** have a supplied DCLGEN/copybook in `upload/`.
- **179** have no supplied DCLGEN -- BLOCKED for column-level layout, consistent with `program-inventory.md` §4.2.

## 0. Copybook-confirmed business records (DCLGEN supplied)

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CUTFEE_PRICE` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 4000-SQL-SELECT-CUTFEE-PRICE, 9436-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUTPRICE_COMPONENT` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* 5000-SQL-SELECT-PRICE-COMP | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUVFEEP` | SUPPLIED (CUVFEEP.CPY) | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CID_FREIGHT_FLAG` | SUPPLIED (CUCIDFRT.CPY) | CUP100 (1).CBL<br/>*paragraphs:* A450-READ-CID-FREIGHT-FLAG | Y | CID-level freight/exemption flags -- CUP100 A450 fallback. DCLGEN supplied (`CUCIDFRT.CPY`). |
| `HC_OVRD_GROUP_ACCOUNT` | SUPPLIED (HCOVDACT.CPY) | A6U01.CBL<br/>*paragraphs:* 9945-CHECK-HC-COST-ACCOUNT, 9960-CHECK-HC-SELL-ACCOUNT | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `HC_OVRD_GROUP_CUSTOMER` | SUPPLIED (HCOVDCID.CPY) | A6U01.CBL<br/>*paragraphs:* 9950-CHECK-HC-COST-CID, 9965-CHECK-HC-SELL-CID | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `HC_OVRD_GROUP_DETAIL` | SUPPLIED (HCOVDGPD.CPY (copybook header comment wrongly says "HC_OVRD_PROD_GROUP")) | A6U01.CBL<br/>*paragraphs:* 9945-CHECK-HC-COST-ACCOUNT, 9950-CHECK-HC-COST-CID | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `HC_OVRD_GROUP_HEADER` | SUPPLIED (HCOVDGRP.CPY (copybook header comment wrongly says "HC_OVRD_PROD_GROUP")) | A6U01.CBL<br/>*paragraphs:* 9945-CHECK-HC-COST-ACCOUNT, 9950-CHECK-HC-COST-CID, 9960-CHECK-HC-SELL-ACCOUNT, 9965-CHECK-HC-SELL-CID | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `HC_OVRD_PRODUCT` | SUPPLIED (HCOVDPRD.CPY) | A6U01.CBL<br/>*paragraphs:* 9945-CHECK-HC-COST-ACCOUNT, 9950-CHECK-HC-COST-CID, 9960-CHECK-HC-SELL-ACCOUNT, 9965-CHECK-HC-SELL-CID | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `OMGPR` | SUPPLIED (OMGPR.CPY) | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* COMPUTATIONAL-3 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 1. CCG* -- Contract family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CCG01` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9040-SQL-SELECT-010, 9045-SQL-SELECT-010, 9285-SQL-SELECT-010, 9290-SQL-SELECT-010, 9645-SQL-SELECT-010 | Y | Contract header -- central to `docs/rules/cost-selection-rules.md` candidates R-COST-001/002/004/005 (individual/group contract resolution, cost-entry-method dispatch). |
| `CCG03` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9037-SQL-ROW-CNT-010, 9040-SQL-SELECT-010, 9045-SQL-SELECT-010, 9283-SQL-ROW-CNT-010, 9285-SQL-SELECT-010, 9290-SQL-SELECT-010... | Y | Contract line (cost/UOM/JIT-exempt/FRT-exempt flags) -- same cost-selection rule set as CCG01. |
| `CCG04` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9040-SQL-SELECT-010, 9045-SQL-SELECT-010 | Y | Contract division scope -- joined in individual-contract candidate search. |
| `CCG05` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9285-SQL-SELECT-010, 9290-SQL-SELECT-010, 9645-SQL-SELECT-010 | Y | Group-contract buy-group linkage, incl. F_GRP_CNT_FEES/F_CNT_PRIORITY -- group-contract duplicate resolution. |
| `CCG06` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9040-SQL-SELECT-010, 9045-SQL-SELECT-010 | Y | Customer-to-contract assignment (individual path) -- individual-contract candidate search key. |
| `CCG07` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9635-SQL-SELECT-010 | Y | Sanctioned/non-sanctioned rating -- freight-fee-exemption cascade. |
| `CCG09` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9040-SQL-SELECT-010, 9045-SQL-SELECT-010 | Y | Individual vendor-contract number + report-group -- individual-contract resolution output fields. |
| `CCG10` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9285-SQL-SELECT-010, 9290-SQL-SELECT-010, 9510-SQL-SELECT-010 | Y | Group-vendor-contract linkage -- group-contract MIN_GRP/MIN_GRP_CONT cursors. |
| `CCG11` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9640-SQL-SELECT-010 | Y | Group contract number -- group-contract resolution output fields. |
| `CCG13` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9005-SQL-SELECT-010 |  | Suggested sell w/ brokerage % -- cost-entry-method code 02. |
| `CCG14` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9010-SQL-SELECT-010 |  | Suggested sell w/ cost-plus % -- cost-entry-method code 03. |
| `CCG15` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9015-SQL-SELECT-010 |  | Suggested sell w/ stated cost -- cost-entry-method code 04. |
| `CCG16` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9020-SQL-SELECT-010 |  | Suggested sell w/ cost-discount % -- cost-entry-method code 05. |
| `CCG21` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9025-SQL-SELECT-010 |  | Suggested sell w/ fixed rebate -- cost-entry-method code 08. |
| `CCG25` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 1000-VERFIFY-CONTRACT-EXCL, 9655-SELECT-CONT-EXCL, COMPUTATIONAL-3 | Y | Contract exclusion list -- individual/group duplicate-contract exclusion filter. |
| `CCG27` | BLOCKED | A6U01.CBL |  | Contract priority flag (individual path, CC_CNT_PRT_FLG) -- duplicate-contract tie-break. |

## 10. Miscellaneous / single-table families

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `A9G36` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BG_FRT_VAR_GRP` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9670-SELECT-AS-OF-GRP | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BG_FRT_VAR_PROD` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9660-SELECT-AS-OF-PROD | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BG_FRT_VAR_VEND` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9665-SELECT-AS-OF-VEND | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CC_CNT_PRT_FLG` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9040-SQL-SELECT-010, 9045-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUR120` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* COMPUTATIONAL-3 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUTADR` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 7707-CHECK-PANDAC-ACCT-FLAG | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUTMST` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* A350-PRO-CUTMST-INFO |  | Customer master (CUST_TYPE) -- CUP100 A350, feeds `OMGPR-ECOMMERCE-PRICING`/NDP gate. |
| `CUVMST` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* COMPUTATIONAL-3 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `DFHRESP` | BLOCKED | A6O010U .CBL; A6O011U.CBL; A6O012U.CBL; A6O013U.CBL; A6X01.CBL<br/>*paragraphs:* COMP-3 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `DVG01` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `ING01` | BLOCKED | A6O016U.CBL; A6U01.CBL<br/>*paragraphs:* 9030-SELECT-ING01, 9195-SQL-SELECT-010 |  | Division inventory (stock item/inv class/freight) -- A6O016U, cost-adjustment. |
| `INSHED` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9695-SQL-SELECT | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `IN_NON_OMSELECT_PART` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9695-SQL-SELECT | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `IN_PROD_SPCL_SRVC_CODE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9982-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `IN_SPCL_SRVC_CODE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9982-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SA_CID_SELL_ASSN` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 7733-FND-CUST-BASE-SELL-ARR | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SA_SELL_ARR_SSC` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9982-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SA_SSC_COST_PLUS` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9987-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SA_SSC_GROSS_MRGN` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9985-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SA_SSC_VENDOR_PUB` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9990-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SQLCA` | BLOCKED | A6O010U .CBL; A6O011U.CBL; A6O012U.CBL; A6O013U.CBL; A6O016U.CBL; A6U01.CBL; A6X01.CBL; CUP100 (1).CBL<br/>*paragraphs:* COMP-3, COMPUTATIONAL-3 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SYMSCSW.CPY` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SY_MISC_SWITCHES` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9810-CHECK-NDP-ON-OFF-SWITCH |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_AC_PC_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9700-GET-VNG35-ROW | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_BG_PC_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9740-GET-VNG34-ROW | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CO_PC_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9730-GET-VNG32-ROW | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_DV_PC_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9720-GET-VNG33-ROW | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_VEND_PARENT` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9910-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 2. CUG* -- Customer family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CUG02` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* A310-SEL-ACTIVE-CUST, COMPUTATIONAL-3 |  | Active customer status/business type -- CUP100 A310. |
| `CUG03` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9420-SQL-SELECT-010, A200-SEL-ACCOUNT, COMPUTATIONAL-3 |  | Account header (customer/rounding/price-method) -- CUP100 A200. |
| `CUG06` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9570-SQL-SELECT-010, A820-SEL-CUST-PRIORITY, COMPUTATIONAL-3 | Y | Customer buy-group priority header -- group-contract cascade level 6. |
| `CUG07` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9575-SQL-SELECT-010, 9610-SQL-SELECT-010, A820-SEL-CUST-PRIORITY, COMPUTATIONAL-3 | Y | Customer buy-group priority detail -- group-contract cascade level 6; also sell-arrangement tier resolution. |
| `CUG08` | BLOCKED | A6U01.CBL | Y | Customer x vendor buy-group override -- group-contract cascade level 5. |
| `CUG09` | BLOCKED | A6U01.CBL | Y | Customer x product-category buy-group override -- group-contract cascade level 4. |
| `CUG10` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9560-SQL-SELECT-010, A810-SEL-ACCT-PRIORITY, COMPUTATIONAL-3 | Y | Account buy-group priority header -- group-contract cascade level 3. |
| `CUG11` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9565-SQL-SELECT-010, 9605-SQL-SELECT-010, A810-SEL-ACCT-PRIORITY, COMPUTATIONAL-3 | Y | Account buy-group priority detail -- group-contract cascade level 3. |
| `CUG12` | BLOCKED | A6U01.CBL | Y | Account x vendor buy-group override -- group-contract cascade level 2. |
| `CUG13` | BLOCKED | A6U01.CBL | Y | Account x product-category buy-group override -- group-contract cascade level 1. |
| `CUG17` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* 0510-SELECT-CUG17, COMPUTATIONAL-3 |  | Ship-to JIT customer/label flags -- CUP100 A500/0510 (ship-to JIT lookup). |
| `CUG18` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9430-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG19` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9240-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG20` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9470-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG21` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9245-SQL-SELECT-010 | Y | Inventory-class adjustment -- sell/cost adjustment. |
| `CUG22` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9520-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG23` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9460-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG24` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9450-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG25` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9035-SQL-SELECT-010, 9515-SQL-SELECT-010 | Y | Ship-to delivery adjustment. |
| `CUG26` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9455-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG27` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9465-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG29` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9030-SQL-SELECT-010, 9435-SQL-SELECT-010 | Y | PANDAC adjustment. |
| `CUG31` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9475-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG33` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9235-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG34` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9230-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG40` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* COMPUTATIONAL-3 |  | JIT account-adjustment-type cursor driver -- CUP100. |
| `CUG41` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* COMPUTATIONAL-3 | Y | JIT account adjustment detail (fee %/type) -- CUP100 A400 dependency (via CUR120/CUP120). |
| `CUG53` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* A425-PRO-CUG53, COMPUTATIONAL-3 | Y | Account freight/exemption flags -- CUP100 A425. |
| `CUG55` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9660-SQL-SELECT-CUG55 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG56` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9650-SQL-SELECT-CUG56 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG57` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9655-SQL-SELECT-CUG57 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG60` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG61` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG62` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG63` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG64` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CUG65` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 3. BGG* -- Buy-group family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `BGG01` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9290-SQL-SELECT-010, 9445-SQL-SELECT-010 | Y | Buy-group header (division, short name). |
| `BGG02` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9325-SQL-SELECT-010, 9480-SQL-SELECT-010 | Y | Buy-group membership. |
| `BGG03` | BLOCKED | A6U01.CBL; CUP100 (1).CBL<br/>*paragraphs:* 9295-SQL-SELECT-010, 9320-SQL-SELECT-010, 9625-SQL-SELECT-010, A850-SEL-PARENTS, COMPUTATIONAL-3 | Y | Buy-group parent chain -- parent-group walk (cost and sell sides). |
| `BGG10` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9250-SQL-SELECT-010, 9615-SQL-SELECT-010, 9680-SQL-SELECT, 9681-SQL-SELECT, 9685-SQL-SELECT, 9686-SQL-SELECT | Y | Group-vendor-contract linkage -- group-contract cursors. |
| `BGG11` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BGG19` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9480-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BGG20` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9525-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `BGG23` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* A830-SEL-LOW-UOM, COMPUTATIONAL-3 | Y | Buy-group low-UOM percentage/vendor-exclusion -- CUP100 A830, `low-uom-break-bulk.md`. |
| `BGG24` | BLOCKED | CUP100 (1).CBL<br/>*paragraphs:* A840-SEL-LOW-UOM-EXCL, COMPUTATIONAL-3 | Y | Buy-group low-UOM customer exclusion -- CUP100 A840. |
| `BGG25` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9595-SQL-SELECT-010 | Y | Buy-group low-UOM vendor exclusion -- `low-uom-break-bulk.md` R-LUOM-001. |

## 4. SAG* -- Sell-assignment family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `SAG01` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 7733-FND-CUST-BASE-SELL-ARR, 9425-SQL-SELECT-010, 9440-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG02` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9315-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG03` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9415-SQL-SELECT-010, 9565-SQL-SELECT-010, 9575-SQL-SELECT-010, 9605-SQL-SELECT-010, 9610-SQL-SELECT-010 | Y | Buy-group tier definition -- buy-group priority/tier resolution (cost and sell). |
| `SAG04` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9210-SQL-SELECT-010 | Y | Sell arrangement, vendor level -- `sell-arrangement-resolution.md` R-SELL-002. |
| `SAG05` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 7733-FND-CUST-BASE-SELL-ARR, 9315-SQL-SELECT-010, 9425-SQL-SELECT-010, 9440-SQL-SELECT-010 | Y | Sell arrangement header (method/percentage) -- central sell-arrangement row. |
| `SAG06` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9215-SQL-SELECT-010 | Y | Sell arrangement, product-category level. |
| `SAG07` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9220-SQL-SELECT-010 | Y | Sell arrangement, contract level. |
| `SAG08` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9225-SQL-SELECT-010 | Y | Sell arrangement, product level. |
| `SAG09` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9425-SQL-SELECT-010, 9440-SQL-SELECT-010 | Y | Sell assignment expiry. |
| `SAG10` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9205-SQL-SELECT-010 | Y | Sell arrangement, default level. |
| `SAG11` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9335-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG12` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9340-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG13` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9345-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG14` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9350-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG15` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9355-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG16` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9360-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG17` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9365-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG18` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9370-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG19` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9395-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG20` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9400-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG21` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9410-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG22` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9405-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG23` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9375-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG24` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9380-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG25` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9385-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SAG26` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9390-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 5. VNG* -- Vendor family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `VNG01` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9500-SQL-SELECT-010 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG02` | BLOCKED | A6O011U.CBL; A6O016U.CBL; A6U01.CBL<br/>*paragraphs:* 9015-SELECT-VNG02, 9490-SQL-SELECT-010, 9490-SQL-SELECT-VNG02, COMP-3 |  | Vendor product master (type/base UOM/custom flag/PANDAC item flag) -- `program-inventory.md` A6O016U. |
| `VNG03` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9200-SQL-SELECT-010, 9300-SQL-SELECT-010, 9305-SQL-SELECT-010, 9310-SQL-SELECT-010, 9675-SELECT-AS-OF-COST, 9676-SELECT-AS-OF-COST... | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG04` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 0217-CHK-PROD-CATE-DEFLT-VEND | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG05` | BLOCKED | A6O011U.CBL; A6O016U.CBL; A6U01.CBL<br/>*paragraphs:* 9050-SELECT-VNG05, 9330-SQL-SELECT-010, 9495-SELECT-VNG05, COMP-3 |  | Vendor product alternate UOM/conversion factor -- `low-uom-break-bulk.md` R-LUOM-002. |
| `VNG06` | BLOCKED | A6O016U.CBL; A6U01.CBL<br/>*paragraphs:* 9025-SELECT-VNG06, 9485-SQL-SELECT-010 | Y | Vendor product category -- A6O016U. |
| `VNG07` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9495-SQL-SELECT-010 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG14` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9535-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG15` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9540-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG16` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9545-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG19` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9530-SQL-SELECT-010, 9690-SQL-SELECT, 9691-SQL-SELECT | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG20` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9600-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG21` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9620-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG22` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9555-SQL-SELECT-010 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG23` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9550-SQL-SELECT-010 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG24` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9630-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG31` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9650-PRO-ACT-VEND-FREIGHT-CHG | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG32` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG33` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG34` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG35` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VNG36` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 6. VN_CID_*/CID_* -- 2021 Phase 2 adjustment family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CID_DELIV_ADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9036-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CID_FIN_CHRG_ADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9457-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CID_NON_CONT_ADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9462-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CID_OVRHD_CHRG_ADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9472-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CID_PREPAY_DEDUCT` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9467-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CID_RISK_PREM_ADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9452-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_ADJ_FEE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9661-SQL-SELECT-CUG73 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_ADJ_FEE_MEDC` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9651-SQL-SELECT-CUG74 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_ADJ_FEE_PCAT` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9656-SQL-SELECT-CUG75 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_PC_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9710-GET-VNG37-ROW | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_VN_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9552-SQL-SELECT-010 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_VN_FREIGHT` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9652-PRO-CID-VEND-FREIGHT-CHG | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `VN_CID_VN_SURCHARGE` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9547-SQL-SELECT-010 | Y | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 7. CU_*_NONSANC_RATING -- 2021 Phase 2 non-sanctioned rating family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CU_AC_CONT_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9923-GET-CUG82 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_AC_PROD_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9921-GET-CUG86 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_AC_VEND_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9922-GET-CUG78 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_BG_CONT_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9929-GET-CUG80 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_BG_PROD_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9927-GET-CUG84 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_BG_VEND_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9928-GET-CUG76 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CID_CONT_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9926-GET-CUG81 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CID_PROD_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9924-GET-CUG85 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CID_VEND_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9925-GET-CUG77 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CO_CONT_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9932-GET-CUG79 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CO_PROD_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9930-GET-CUG83 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CO_VEND_NONSANC_RATING` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9931-GET-CUG68 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 8. CU_*_COSTADJ -- cost-adjustment family

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `CU_AC_CONT_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9850-GET-CUG60 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_AC_VEND_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9860-GET-CUG61 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_BG_CONT_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9870-GET-CUG62 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_BG_VEND_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9880-GET-CUG63 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CO_CONT_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9890-GET-CUG64 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `CU_CO_VEND_COSTADJ` | BLOCKED | A6U01.CBL<br/>*paragraphs:* 9900-GET-CUG65 |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

## 9. SASSC*/INPRDSSC/INSSC -- special-service-code family (2022)

| Table | DCLGEN | Consumers (file: paragraphs) | Date-filtered? | Purpose (INFERRED unless noted) |
|---|---|---|---|---|
| `INPRDSSC` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `INSSC` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SASSC` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SASSCCP` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SASSCGM` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |
| `SASSCVP` | BLOCKED | A6U01.CBL |  | (not cross-referenced to a decision-table doc in this pass -- see raw WHERE/columns in the CSV) |

---

## Cross-reference index (tables already load-bearing in a decision-table document)

The following tables have confirmed business-rule context beyond this document, in
`docs/cobol-analysis/decision-tables/*.md`:


- `BGG01` -- Buy-group header (division, short name).
- `BGG02` -- Buy-group membership.
- `BGG03` -- Buy-group parent chain -- parent-group walk (cost and sell sides).
- `BGG10` -- Group-vendor-contract linkage -- group-contract cursors.
- `BGG23` -- Buy-group low-UOM percentage/vendor-exclusion -- CUP100 A830, `low-uom-break-bulk.md`.
- `BGG24` -- Buy-group low-UOM customer exclusion -- CUP100 A840.
- `BGG25` -- Buy-group low-UOM vendor exclusion -- `low-uom-break-bulk.md` R-LUOM-001.
- `CCG01` -- Contract header -- central to `docs/rules/cost-selection-rules.md` candidates R-COST-001/002/004/005 (individual/group contract resolution, cost-entry-method dispatch).
- `CCG03` -- Contract line (cost/UOM/JIT-exempt/FRT-exempt flags) -- same cost-selection rule set as CCG01.
- `CCG04` -- Contract division scope -- joined in individual-contract candidate search.
- `CCG05` -- Group-contract buy-group linkage, incl. F_GRP_CNT_FEES/F_CNT_PRIORITY -- group-contract duplicate resolution.
- `CCG06` -- Customer-to-contract assignment (individual path) -- individual-contract candidate search key.
- `CCG07` -- Sanctioned/non-sanctioned rating -- freight-fee-exemption cascade.
- `CCG09` -- Individual vendor-contract number + report-group -- individual-contract resolution output fields.
- `CCG10` -- Group-vendor-contract linkage -- group-contract MIN_GRP/MIN_GRP_CONT cursors.
- `CCG11` -- Group contract number -- group-contract resolution output fields.
- `CCG13` -- Suggested sell w/ brokerage % -- cost-entry-method code 02.
- `CCG14` -- Suggested sell w/ cost-plus % -- cost-entry-method code 03.
- `CCG15` -- Suggested sell w/ stated cost -- cost-entry-method code 04.
- `CCG16` -- Suggested sell w/ cost-discount % -- cost-entry-method code 05.
- `CCG21` -- Suggested sell w/ fixed rebate -- cost-entry-method code 08.
- `CCG25` -- Contract exclusion list -- individual/group duplicate-contract exclusion filter.
- `CCG27` -- Contract priority flag (individual path, CC_CNT_PRT_FLG) -- duplicate-contract tie-break.
- `CUG02` -- Active customer status/business type -- CUP100 A310.
- `CUG03` -- Account header (customer/rounding/price-method) -- CUP100 A200.
- `CUG06` -- Customer buy-group priority header -- group-contract cascade level 6.
- `CUG07` -- Customer buy-group priority detail -- group-contract cascade level 6; also sell-arrangement tier resolution.
- `CUG08` -- Customer x vendor buy-group override -- group-contract cascade level 5.
- `CUG09` -- Customer x product-category buy-group override -- group-contract cascade level 4.
- `CUG10` -- Account buy-group priority header -- group-contract cascade level 3.
- `CUG11` -- Account buy-group priority detail -- group-contract cascade level 3.
- `CUG12` -- Account x vendor buy-group override -- group-contract cascade level 2.
- `CUG13` -- Account x product-category buy-group override -- group-contract cascade level 1.
- `CUG17` -- Ship-to JIT customer/label flags -- CUP100 A500/0510 (ship-to JIT lookup).
- `CUG21` -- Inventory-class adjustment -- sell/cost adjustment.
- `CUG25` -- Ship-to delivery adjustment.
- `CUG29` -- PANDAC adjustment.
- `CUG40` -- JIT account-adjustment-type cursor driver -- CUP100.
- `CUG41` -- JIT account adjustment detail (fee %/type) -- CUP100 A400 dependency (via CUR120/CUP120).
- `CUG53` -- Account freight/exemption flags -- CUP100 A425.
- `CUTMST` -- Customer master (CUST_TYPE) -- CUP100 A350, feeds `OMGPR-ECOMMERCE-PRICING`/NDP gate.
- `CU_CID_FREIGHT_FLAG` -- CID-level freight/exemption flags -- CUP100 A450 fallback. DCLGEN supplied (`CUCIDFRT.CPY`).
- `ING01` -- Division inventory (stock item/inv class/freight) -- A6O016U, cost-adjustment.
- `SAG03` -- Buy-group tier definition -- buy-group priority/tier resolution (cost and sell).
- `SAG04` -- Sell arrangement, vendor level -- `sell-arrangement-resolution.md` R-SELL-002.
- `SAG05` -- Sell arrangement header (method/percentage) -- central sell-arrangement row.
- `SAG06` -- Sell arrangement, product-category level.
- `SAG07` -- Sell arrangement, contract level.
- `SAG08` -- Sell arrangement, product level.
- `SAG09` -- Sell assignment expiry.
- `SAG10` -- Sell arrangement, default level.
- `VNG02` -- Vendor product master (type/base UOM/custom flag/PANDAC item flag) -- `program-inventory.md` A6O016U.
- `VNG05` -- Vendor product alternate UOM/conversion factor -- `low-uom-break-bulk.md` R-LUOM-002.
- `VNG06` -- Vendor product category -- A6O016U.


---

## Companion file

Row-per-SQL-block detail (417 rows: paragraph, exact `WHERE` text, host variables, `ORDER BY`,
cursor lifecycle role, and the mechanically-located `SQLCODE` handling with `WHEN`/`IF` values and
fatal-path flag) is at `docs/cobol-analysis/sql-query-inventory.csv`.

## Assumptions

1. `PrimaryTables` in the CSV is the first token of each comma-separated `FROM`-clause entry --
   for an aliased join like `CC_CNT_PRT_FLG CCG27`, the *base table name* (`CC_CNT_PRT_FLG`) is
   captured, but this document also cross-references the alias (`CCG27`) where it is the name used
   elsewhere in this project's documents, to avoid a naming mismatch.
2. The `SQLCODE`-handling call-site heuristic (finding a `PERFORM <paragraph>` reference and
   scanning forward from there) takes the **first** call site found in the file when a helper
   paragraph is called from multiple places. If a helper is genuinely called from more than one
   place with *different* error handling, only one of those is captured here -- flagged as a
   possible source of incompleteness, not verified against every call site for every paragraph.
3. Business "Purpose" descriptions for tables not already covered by an existing decision-table
   document are inferred from column names and `WHERE`-clause shape only, not from any DCLGEN
   (since none is supplied for ~85% of these tables) -- treat these as a starting point for the
   `docs/rules/*.md` tasks, not a final word.

## Open items

1. 15% of query/cursor blocks (mostly `DECLARE CURSOR`, some `OPEN`/`CLOSE`) have no mechanically
   -located `SQLCODE` handling -- either because none exists in this codebase's idiom for that
   block type (expected for `DECLARE`) or because the call-site heuristic didn't resolve it.
2. Purpose descriptions for the majority of tables outside the cost/sell/JIT/low-UOM rule areas
   already covered are a first-pass inference from column/paragraph naming, not a deep read of
   each paragraph's logic -- appropriate depth for a SQL/table *inventory* (this task), not a
   substitute for the `docs/rules/*.md` decision-table tasks that will read these paragraphs in
   full.

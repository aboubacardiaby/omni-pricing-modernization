# A6U01 — JIT Fee Adjustment: Characterization Scenarios (T007)

**Scope:** Characterization scenarios and expected decision paths for
`docs/cobol-analysis/decision-tables/jit-adjustment.md` (rules `R-JIT-001`–`007`). Same format,
method, and caveats as the two prior characterization-scenario documents (T003, T005) — see
`characterization-scenarios/contract-cost-method.md`'s "How to read a scenario" section if this is
the first of the three you're opening. Not repeated in full here.

**What this document is NOT:** numeric golden-value tests — structural characterization only (no
DB2/CICS/COBOL execution access in this environment).

**Source of truth:** `docs/cobol-analysis/decision-tables/jit-adjustment.md` (cited as `R-JIT-00x`)
and `upload/A6U01.CBL` directly. Same confidence key as prior documents.

---

## Group A — Service-fee type gate (`R-JIT-001`)

### SCN-JIT-001 — Unrecognized or blank service-fee code → entire JIT module skipped
- **Given:** `OMGPR-JIT-SERVICE-FEE` holds a value other than `A`/`C`/`R`/`P` (spaces, or any other
  character).
- **Decision path:** `7225-PRO-JIT-ADJ-010`'s single outer `IF` (lines 13044–13047) is false → the
  entire paragraph body is skipped, no `ELSE`. **Note this gate is also re-checked by the caller,
  `7195-PRO-ADJ-SELL-010`, before even performing `7225`** (per `R-JIT-001` item 3) — so in
  practice this scenario is more often "the caller never calls `7225` at all" than "`7225` runs and
  does nothing," but the effect is identical either way.
- **Expected outcome:** every `OMGPR-A-JIT-*` field remains at whatever value it held before this
  line was processed (not re-initialized by `7225` itself — its own `INITIALIZE` site, if any, was
  not traced in T006). No error, no diagnostic — the third occurrence of this codebase's
  "unrecognized code → silent no-op" idiom, per `R-JIT-001` item 9.
- **Confidence:** CONFIRMED. Citation: `R-JIT-001` items 5, 9.

### SCN-JIT-002 — The four valid codes, side by side
- **Given:** four otherwise-identical lines, differing only in `OMGPR-JIT-SERVICE-FEE` = `A`, `C`,
  `R`, or `P`, each with nonzero vendor JIT percentages configured.
- **Decision path:** all four enter `7225`'s body; category calcs (`R-JIT-002`) route to cost-based
  or sell-based formulas per the `A`/`C` vs. `R`/`P` split; the total (`R-JIT-006`) routes to
  `OMGPR-A-JIT-COST` (`A`/`C`) or `OMGPR-A-JIT-SELL` (`R`/`P`).
- **Expected outcome (per `R-JIT-001` item 5's table):**

  | Code | Calc base | Embedded in line item? | `OMGPR-F-JIT-REMOVED` |
  |---|---|---|---|
  | `A` | Cost | No | `'Y'` only if `OMGPR-A-JIT-COST > 0` |
  | `C` | Cost | Yes | never set by this rule |
  | `R` | Sell | No | `'Y'` unconditionally |
  | `P` | Sell | Yes | never set by this rule |

  "Embedded in line item" is not itself enforced inside `7225` — it is the *meaning* the header
  comment assigns to each code, realized elsewhere (out of this task's traced scope, per
  `R-JIT-001` item 3's note that `7195` re-checks the same codes) — this scenario documents what
  `7225` itself confirms (the calc base and the removed-flag), not the line-item-embedding
  mechanism, which lives in a different paragraph.
- **Confidence:** CONFIRMED for calc-base and removed-flag columns. INFERRED for the
  "embedded in line item" column (stated in the header comment, not independently re-derived from
  a second, separate mechanism in this task).

---

## Group B — Fee-category computation (`R-JIT-002`)

### SCN-JIT-003 — Print-label fee, percent-based, cost-based line, standard product
- **Given:** `OMGPR-JIT-ITEM-LABEL` true; `OMGPR-PCT-PER-LABEL` true; `OMGPR-JIT-SERVICE-FEE = 'C'`;
  `OMGPR-C-PRIVATE-LBL NOT = 'O'`; `WS-VND-JIT-LB-FEE > 0`.
- **Decision path:** `7225` → label section → item gate true → `7227-CHK-FOR-OVERRIDES` (BLOCKED,
  may adjust `WS-VND-JIT-LB-FEE` — not traced) → `WS-VND-JIT-LB-FEE > 0` → `OMGPR-PCT-PER-LABEL`
  true → `(OMGPR-JIT-SERVICE-FEE = 'C') AND (PRIVATE-LBL NOT = 'O')` both true → cost-based formula.
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ROUNDED = OMGPR-A-TOTAL-COST * WS-VND-JIT-LB-FEE`
  (post any `7227` override, unknown to this task).
- **Confidence:** CONFIRMED for the control flow and formula shape; the actual `WS-VND-JIT-LB-FEE`
  value is contingent on the un-traced `7227` cascade.

### SCN-JIT-004 — Same fee, sell-based line (code `R`/`P`, or private-label `'O'` override)
- **Given:** either `OMGPR-JIT-SERVICE-FEE` is `R`/`P`, **or** it is `A`/`C` but
  `OMGPR-C-PRIVATE-LBL = 'O'` (invoking `R-JIT-005`).
- **Decision path:** same as SCN-JIT-003 up to the cost-vs-sell test, which now takes the `ELSE`
  branch.
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ROUNDED = WS-A-SELL-BEFORE-ADJ * WS-VND-JIT-LB-FEE`.
  **This one scenario covers two different Given conditions producing the identical decision path**
  — deliberately, to make explicit that R-JIT-005's private-label carve-out and a genuinely
  sell-based service-fee code are indistinguishable from this formula's point of view; only
  `OMGPR-A-JIT-COST` vs. `OMGPR-A-JIT-SELL` at the R-JIT-006 total step (not this category's own
  output field) would differ between the two.
- **Confidence:** CONFIRMED.

### SCN-JIT-005 — Rate-based label fee, full UOM match
- **Given:** `OMGPR-RATE-PER-LABEL` true; `OMGPR-LABEL-UM-FOUND` and `OMGPR-ALT-ORD-UM-FOUND` both
  true, and `OMGPR-ALT-ORD-UM = OMGPR-C-ORD-LIN-CUST-UOM`.
- **Decision path:** `7225` → label section → `EVALUATE TRUE`'s first `WHEN` (line 13159–13165).
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ROUNDED = (WS-VND-JIT-LB-FEE *
  OMGPR-ALT-ORD-CONV-FACTOR * OMGPR-JIT-ITEM-LABEL-QTY) / OMGPR-CUST-LABEL-CONV-FACTOR`.
- **Confidence:** CONFIRMED. Citation: `R-JIT-002` item 5; `A6U01.CBL` 13158–13170.

### SCN-JIT-006 — Rate-based label fee, label UOM found but alt-order UOM not applicable
- **Given:** `OMGPR-RATE-PER-LABEL` true; `OMGPR-LABEL-UM-FOUND` true; **either**
  `OMGPR-ALT-ORD-UM-FOUND` is false **or** the alt-order UOM doesn't equal the customer's order UOM.
- **Decision path:** `EVALUATE TRUE`'s second `WHEN` (line 13171).
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ROUNDED = (WS-VND-JIT-LB-FEE *
  OMGPR-JIT-ITEM-LABEL-QTY) / OMGPR-CUST-LABEL-CONV-FACTOR` — no `ALT-ORD-CONV-FACTOR` term.
- **Confidence:** CONFIRMED.

### SCN-JIT-007 — Rate-based label fee, no label UOM resolved at all
- **Given:** `OMGPR-RATE-PER-LABEL` true; `OMGPR-LABEL-UM-FOUND` false.
- **Decision path:** `EVALUATE TRUE`'s `WHEN OTHER` (line 13181).
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ROUNDED = WS-VND-JIT-LB-FEE *
  OMGPR-JIT-ITEM-LABEL-QTY` — no conversion factor applied at all. **Three genuinely different
  numeric results for the same nominal rate and quantity, selected purely by which UOM-resolution
  flags happened to be true** — a migration must replicate this exact 3-way selection, not
  approximate it with a single "best available" conversion.
- **Confidence:** CONFIRMED.

### SCN-JIT-008 — Flat per-item label rate
- **Given:** `OMGPR-LABEL-RATE-PER-ITEM` true (a third, mutually-exclusive charge-type value,
  distinct from percent and rate).
- **Decision path:** line 13187–13190, independent of the `EVALUATE` above (this is a separate
  `IF`, not another `WHEN` in the same `EVALUATE` — CONFIRMED, so in principle both the rate
  `EVALUATE` and this flat-rate `IF` could both apply if their guarding 88-levels were not mutually
  exclusive by value; they are, per `OMGPR-JIT-LABEL-CHRG-TYPE`'s three 88-levels).
- **Expected outcome:** `OMGPR-A-JIT-LABEL-FEE ← WS-VND-JIT-LB-FEE` directly — no quantity
  multiplication, no conversion. A flat per-item fee is not scaled by `OMGPR-JIT-ITEM-LABEL-QTY` at
  all, unlike every rate-based variant in SCN-JIT-005–007.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13187–13190.

### SCN-JIT-009 — Apply-label fee mirrors label fee's percent/rate/flat structure exactly
- **Given:** any of SCN-JIT-003–008's conditions, but for apply-label instead of label
  (`OMGPR-JIT-ITEM-APPLY-LAB` in place of `OMGPR-JIT-ITEM-LABEL`, `OMGPR-PCT-PER-APPLY`/
  `OMGPR-RATE-PER-APPLY`/`OMGPR-APPLY-RATE-PER-ITEM` in place of the label equivalents), **and**
  the 2022 fee-component fast path (`R-JIT-004`) does *not* apply (see SCN-JIT-014 for when it
  does).
- **Decision path / outcome:** identical shape to SCN-JIT-003–008, writing `OMGPR-A-JIT-APPLY-FEE`
  instead of `OMGPR-A-JIT-LABEL-FEE`, and reusing the **same** `OMGPR-CUST-LABEL-CONV-FACTOR` and
  `OMGPR-JIT-ITEM-LABEL-QTY` fields as the label calculation (CONFIRMED — apply-label's rate
  formula, lines 13233–13242, uses the identically-named quantity/conversion fields as label's,
  not a separate apply-label-specific quantity; this is either intentional field reuse or a
  copy-paste artifact that was never given its own fields — this task cannot distinguish the two
  from the code alone).
- **Confidence:** CONFIRMED for the structural mirroring. Flagged (not resolved) that apply-label
  has no `OMGPR-JIT-ITEM-APPLY-LABEL-QTY`-style field of its own distinct from label's quantity.

### SCN-JIT-010 — Service fee, LUM fee, and extra-delivery fee computed together, cost-based
- **Given:** `OMGPR-JIT-SERVICE-FEE = 'A'` (or `'C'`), `OMGPR-C-PRIVATE-LBL NOT = 'O'`,
  `WS-VND-JIT-SF-FEE > 0`.
- **Decision path:** `7225` → service-fee section → `7227-CHK-FOR-OVERRIDES` → `WS-VND-JIT-SF-FEE >
  ZEROS` → cost-based branch, `WS-VND-JIT-SF-FEE > ZEROES` sub-guard → all three `COMPUTE`s fire
  together (lines 13278–13286).
- **Expected outcome:** `OMGPR-A-JIT-SERVICE-FEE`, `OMGPR-A-JIT-LUM-FEE`, and
  `OMGPR-A-JIT-EXTRA-DELIV-FEE` are all set from `OMGPR-A-TOTAL-COST * <their respective vendor
  percent>` **in the same pass** — there is no way to get one without the other two in this
  branch (unless the individual vendor percentages are themselves zero, in which case `COMPUTE`
  still runs but produces zero).
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13277–13287.

### SCN-JIT-011 — Non-OM-select fee folds into the service-fee total, with its own extra gate
- **Given:** `WS-VND-JIT-PF-FEE > 0` **and** `WS-NON-OM-SELECT-ITEM` true (a per-line-item
  condition distinct from everything else gating this paragraph — its own source not traced, per
  `program-inventory.md`'s `IN_NON_OMSELECT_PART` reference).
- **Decision path:** within either the cost-based or sell-based service-fee branch, the additional
  `IF WS-VND-JIT-PF-FEE > ZEROES AND WS-NON-OM-SELECT-ITEM` (lines 13288–13289 or 13308–13309)
  fires independently of the main service-fee `IF`.
- **Expected outcome:** `OMGPR-A-JIT-NON-OM-SLCT-FEE` is computed **and immediately added into**
  `OMGPR-A-JIT-SERVICE-FEE` (`ADD OMGPR-A-JIT-NON-OM-SLCT-FEE TO OMGPR-A-JIT-SERVICE-FEE`) — it is
  not a separate line item in the final total (`R-JIT-006`'s sum formula only references
  `OMGPR-A-JIT-SERVICE-FEE`, not `-NON-OM-SLCT-FEE` by name) but its value is fully absorbed into
  that field. **A non-OM-select item with `WS-VND-JIT-PF-FEE = 0` gets nothing added, even if the
  main service fee also fires** — the two gates are independent.
- **Confidence:** CONFIRMED. Citation: `R-JIT-002` item 5; `A6U01.CBL` 13288–13295, 13308–13315.

### SCN-JIT-012 — Item not flagged for a given service → that category contributes nothing
- **Given:** `OMGPR-JIT-ITEM-LABEL` (or `-APPLY-LAB`, or `-BREAK-BULK`) is false for this specific
  order line, regardless of what the vendor's JIT fee configuration otherwise allows.
- **Decision path:** the corresponding section's outer `IF` (line 13133, 13194, or the — now
  entirely dead per R-JIT-003 — break-bulk section) is false; nothing inside runs;
  `7227-CHK-FOR-OVERRIDES` is not even called for that category.
- **Expected outcome:** the category's `OMGPR-A-JIT-*-FEE` field is whatever it was before this
  paragraph ran (not re-set) — contributes zero to `R-JIT-006`'s total only if it was already zero
  going in; this task did not trace whether these fields are re-initialized per line elsewhere.
- **Confidence:** CONFIRMED for the control flow; INFERRED (not verified) that the fields are
  zero-initialized per line by the time this matters.

---

## Group C — Break-bulk dead code / LUOM fallback (`R-JIT-003`)

### SCN-JIT-013 — LUOM-eligible stock order → LUOM fallback fires (the practical every-time case)
- **Given:** `OMGPR-F-LUOM-ELIG-SW = 'Y'`; `OMGPR-ORDER-TYPE-STOCK` true (order type `'S'` or
  space); (break-bulk fee is `0`, which per `R-JIT-003` is true unconditionally as the code
  currently stands).
- **Decision path:** `7225` → `IF OMGPR-A-JIT-BREAK-FEE = 0 AND OMGPR-F-LUOM-ELIG-SW = 'Y' AND
  OMGPR-ORDER-TYPE-STOCK` (true) → `0245-PRO-BG-LOW-UOM-010` (not traced — BLOCKED).
- **Expected outcome:** whatever `0245` computes for the buy-group low-UOM charge; **this is, in
  practice, the only way this line's "break-bulk-or-LUOM" concept produces a nonzero charge today**,
  per R-JIT-003's finding — there is no live code path where the JIT break-bulk percent/rate
  formula itself produces a nonzero `OMGPR-A-JIT-BREAK-FEE`.
- **Confidence:** CONFIRMED for the gate; BLOCKED for `0245`'s output.

### SCN-JIT-014 — Non-stock order, or LUOM-ineligible → neither break-bulk nor LUOM charges
- **Given:** either `OMGPR-F-LUOM-ELIG-SW NOT = 'Y'` (the account is not LUOM-eligible — per
  `R-JIT-003` item 9, a determination made in the still-BLOCKED `CUS120`) or
  `OMGPR-ORDER-TYPE-STOCK` is false (a non-stock order type).
- **Decision path:** the `R-JIT-003` gate is false; `0245` is never called; and, per `R-JIT-003`,
  the break-bulk percent/rate formula that would otherwise be the alternative is commented out —
  so **neither** mechanism produces a charge.
- **Expected outcome:** `OMGPR-A-JIT-BREAK-FEE` remains `0` for this line, unconditionally, in this
  combination of conditions. This is worth stating explicitly as its own scenario because it is the
  case a reader might assume "must" produce *some* break-bulk-related charge (the line item was
  presumably flagged `OMGPR-JIT-ITEM-BREAK-BULK` for a reason) but, per the current code, does not.
- **Confidence:** CONFIRMED.

---

## Group D — 2022 fee-component interaction (`R-JIT-004`)

### SCN-JIT-015 — 2022 break-bulk fee-component row exists, percent-typed → legacy amount unchanged
- **Given:** `OMGPR-FEE-SHRT-CODE-BB > SPACES`; `OMGPR-FEE-PCT-SELL-BB` or `OMGPR-FEE-PCT-COST-BB`
  true.
- **Decision path:** `7225`'s opening field-setup block (lines 13049–13060) → the `IF
  OMGPR-FEE-SHRT-CODE-BB > SPACES` branch, inner `IF` true → `WS-VND-JIT-BB-FEE ←
  OMGPR-JIT-BREAK-CHRG-AMT` (unchanged from the legacy source).
- **Expected outcome:** no observable difference from the case where no 2022 row exists at all —
  this scenario exists to make explicit that the 2022 system's mere *presence* doesn't change
  anything by itself; only its *type* (percent vs. not) matters, per SCN-JIT-016.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13050–13056.

### SCN-JIT-016 — 2022 break-bulk fee-component row exists, flat-typed → legacy amount suppressed
- **Given:** `OMGPR-FEE-SHRT-CODE-BB > SPACES`; neither `OMGPR-FEE-PCT-SELL-BB` nor
  `OMGPR-FEE-PCT-COST-BB` (i.e., the fee type is per-line/per-order/per-qty).
- **Decision path:** same entry point as SCN-JIT-015, `ELSE` branch → `WS-VND-JIT-BB-FEE ← ZERO`.
- **Expected outcome:** the legacy JIT break-bulk working value is forced to zero — currently moot
  in combination with R-JIT-003 (it was already effectively always producing zero either way), but
  this is the specific mechanism that would matter immediately if the commented-out break-bulk
  percent/rate formula were ever restored, since restoring it would then be silently neutralized
  whenever a flat-typed 2022 fee-component row exists for this account's break-bulk fee.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13054–13056.

### SCN-JIT-017 — 2022 apply-label fee-component row, flat-typed → fast path bypasses the entire legacy cascade
- **Given:** `OMGPR-FEE-SHRT-CODE-AL > SPACES` and (`OMGPR-FEE-PER-LINE-AL` or
  `OMGPR-FEE-PER-ORDER-AL` or `OMGPR-FEE-PER-QTY-AL`) true.
- **Decision path:** `7225` → apply-label section → the `SJ1022` fast-path `IF` (line 13195) is
  true → `OMGPR-A-JIT-APPLY-FEE ← OMGPR-A-APPLY-LAB-AL` directly → **`7227-CHK-FOR-OVERRIDES` and
  the entire percent/rate/flat sub-dispatch (lines 13201–13260) never execute for this line.**
- **Expected outcome:** the apply-label fee is whatever `CUP100`'s 2022 fee-component lookup
  already computed, full stop — any vendor-level JIT override that `7227`'s cascade might otherwise
  have applied is **not consulted at all** in this case. This is the sharpest version of R-JIT-004's
  finding: the 2022 system doesn't just adjust an input to the legacy calculation here, it
  **replaces the legacy calculation's entire code path**, override cascade included.
- **Confidence:** CONFIRMED. Citation: `R-JIT-004` item 3 (apply-label); `A6U01.CBL` 13195–13200.

### SCN-JIT-018 — 2022 apply-label row exists but is percent-typed → legacy cascade still runs
- **Given:** `OMGPR-FEE-SHRT-CODE-AL > SPACES` but the fee type is percent-of-sell or
  percent-of-cost (not per-line/order/qty).
- **Decision path:** the SJ1022 fast-path condition (`OMGPR-FEE-PER-LINE-AL OR -ORDER-AL OR
  -QTY-AL`) is false → falls to the `ELSE` at line 13201 → full legacy cascade (`7227` +
  percent/rate/flat, per SCN-JIT-003–009) runs as if no 2022 row existed.
- **Expected outcome:** identical control flow to SCN-JIT-009 (apply-label mirrors label). The
  2022 fee-component row's *existence* was checked but its data was never actually read in this
  branch — only its fee-type mattered, to decide whether to skip the legacy path.
- **Confidence:** CONFIRMED.

---

## Group E — Private-label `'O'` exception (`R-JIT-005`)

### SCN-JIT-019 — Cost-based service-fee type, but private-label `'O'` product → routes to sell-based formula anyway
- **Given:** `OMGPR-JIT-SERVICE-FEE = 'C'`; `OMGPR-C-PRIVATE-LBL = 'O'`.
- **Decision path:** every cost-vs-sell test in `7225` (label, apply-label, service-fee, and the
  final total) evaluates its `AND OMGPR-C-PRIVATE-LBL NOT = 'O'` clause as false → all four take
  their `ELSE` (sell-based) branch, **despite the service-fee code being `'C'`**.
- **Expected outcome:** every JIT fee category for this line computes from
  `WS-A-SELL-BEFORE-ADJ`, not `OMGPR-A-TOTAL-COST`; the total is written to `OMGPR-A-JIT-SELL`, not
  `OMGPR-A-JIT-COST` — **even though `R-JIT-001`'s table says `'C'` is a cost-based code.** This is
  the scenario that most directly demonstrates why R-JIT-005 was worth extracting as its own rule
  rather than folding into R-JIT-001: reading only the header comment's A/C/R/P table would predict
  the wrong calc base for any private-label-`'O'` product on a `'C'`-type account.
- **Confidence:** CONFIRMED. Citation: `R-JIT-005`; `A6U01.CBL` 13146, 13213, 13276, 13322.

---

## Group F — JIT-removed flag (`R-JIT-006`)

### SCN-JIT-020 — Code `'A'` with positive cost-based JIT total → flagged removed
- **Given:** `OMGPR-JIT-SERVICE-FEE = 'A'`; the four category fees sum to `OMGPR-A-JIT-COST > 0`.
- **Decision path:** `R-JIT-006`'s final check, first disjunct true
  (`OMGPR-JIT-SERVICE-FEE = 'A' AND OMGPR-A-JIT-COST > 0`).
- **Expected outcome:** `OMGPR-F-JIT-REMOVED = 'Y'`.
- **Confidence:** CONFIRMED.

### SCN-JIT-021 — Code `'A'` with zero JIT total → not flagged removed
- **Given:** `OMGPR-JIT-SERVICE-FEE = 'A'`; every category gate happened to be false or every
  vendor fee happened to be zero, so `OMGPR-A-JIT-COST = 0`.
- **Decision path:** first disjunct's second condition (`OMGPR-A-JIT-COST > 0`) is false; second
  disjunct (`= 'R'`) is also false (code is `'A'`, not `'R'`) → the whole `IF` is false.
- **Expected outcome:** `OMGPR-F-JIT-REMOVED` is **not** set by this rule (retains whatever value
  it held before — not traced to its initialization site in this pass).
- **Confidence:** CONFIRMED.

### SCN-JIT-022 — Code `'R'` → always flagged removed, regardless of the sell-based total
- **Given:** `OMGPR-JIT-SERVICE-FEE = 'R'`, any value of `OMGPR-A-JIT-SELL` including zero.
- **Decision path:** second disjunct true unconditionally (no `> 0` guard on this side, unlike the
  `'A'` case) → `OMGPR-F-JIT-REMOVED = 'Y'`.
- **Expected outcome:** flagged removed even when the computed JIT-sell total is zero — the
  asymmetry with `'A'` (which requires `> 0`) flagged in `R-JIT-006` item 9 is real and observable
  exactly here: a `'R'`-type line with zero JIT ends up flagged the same as one with a substantial
  JIT amount, while an equivalent zero-JIT `'A'`-type line does not get flagged at all.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13336–13340.

---

## Group G — Freight-fee exemption (`R-JIT-007`, adjacent cost-side rule)

### SCN-JIT-023 — Custom account, explicit non-exempt flag → not exempt
- **Given:** `VNG02-C-CUSTOM-IND = 'Y'`; `OMGPR-F-EXEMPT-CUSTOM-FLAG NOT = 'N'` (i.e. `'Y'`, meaning
  "exemption does not apply" per the inverted flag sense already flagged in `R-JIT-007` item 5).
- **Decision path:** `7226-CHK-FRT-FEE-EXEMPT` → custom branch, condition false →
  `WS-EXEMPT-FRT-CHARGES` stays `'N'` (its `MOVE 'N'` default at line 13350). **None of the
  group-sanctioned/division/individual levels are even reached** — custom-account status
  short-circuits the whole cascade, exempt or not.
- **Expected outcome:** `WS-EXEMPT-FRT-CHARGES = 'N'` → the "not exempt" `OMGPR-C-INFRT-TYPE`
  remapping table applies (sell-based or variance-based sub-table, per `WS-APPLY-FRT-CST-SELL-SW`).
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13352–13357.

### SCN-JIT-024 — Non-custom, cost contract found, group-sanctioned → exempt
- **Given:** `VNG02-C-CUSTOM-IND NOT = 'Y'`; `WS-COST-CONT-FND` true; `CCG09-I-RPT-GRP > 0` (or
  `OMGPR-F-GRP-CNT-FEES = 'Y'`); `OMGPR-F-EXEMPT-SANC-FLAG = 'N'`.
- **Decision path:** custom branch false → `WS-COST-CONT-FND` branch → group-sanctioned sub-branch
  true → flag condition true → `WS-EXEMPT-FRT-CHARGES = 'Y'`.
- **Expected outcome:** the "exempt" `OMGPR-C-INFRT-TYPE` remapping table applies (the first
  `EVALUATE`, lines 13387–13402) — different target letters than the not-exempt tables, per
  `R-JIT-007` item 7.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13359–13363.

### SCN-JIT-025 — No cost contract at all → falls to the non-contract exemption check
- **Given:** `VNG02-C-CUSTOM-IND NOT = 'Y'`; `WS-COST-CONT-FND` false (per `R-COST-001`'s
  price-list-fallback outcome).
- **Decision path:** custom branch false → `WS-COST-CONT-FND` branch false → `ELSE` at line
  13379–13382 → exempt iff `OMGPR-F-EXEMPT-NON-CONT-FLAG = 'N'`. **This is another concrete
  cross-link to the cost-side documents**: whether a line even reaches this specific exemption
  check (vs. the group-sanctioned/division/individual checks) depends on `R-COST-001`'s outcome,
  exactly as `R-SELL-001` depended on it for the sell-arrangement cascade choice.
- **Expected outcome:** exempt or not per `OMGPR-F-EXEMPT-NON-CONT-FLAG`, then the corresponding
  `OMGPR-C-INFRT-TYPE` remap.
- **Confidence:** CONFIRMED. Citation: `A6U01.CBL` 13379–13382.

---

## Coverage summary

| Rule | Scenarios |
|---|---|
| R-JIT-001 (service-fee gate) | SCN-JIT-001–002 |
| R-JIT-002 (fee categories) | SCN-JIT-003–012 |
| R-JIT-003 (break-bulk/LUOM) | SCN-JIT-013–014 |
| R-JIT-004 (2022 fee-component interaction) | SCN-JIT-015–018 |
| R-JIT-005 (private-label `'O'`) | SCN-JIT-019 |
| R-JIT-006 (JIT-removed flag) | SCN-JIT-020–022 |
| R-JIT-007 (freight exemption) | SCN-JIT-023–025 |

25 scenarios across 7 rules — more than the prior two documents because this rule area has more
independent sub-mechanisms (five fee categories × three charge-type variants, plus a live
20-year-apart subsystem interaction) packed into one paragraph.

## Assumptions

Same standing assumptions as the prior two characterization documents (upstream validation passed;
un-read helper paragraphs marked INFERRED where depended on; no numeric examples invented) apply
here without repetition.

## Blockers

1. `7227`'s six override-check paragraphs — every scenario in Group B that mentions "post any
   `7227` override, unknown to this task" is bounded by this gap.
2. `0245-PRO-BG-LOW-UOM-010`'s actual output (SCN-JIT-013).
3. `WS-NON-OM-SELECT-ITEM`'s own source (SCN-JIT-011) — referenced, not traced to its origin.
4. `OMGPR-C-INFRT-TYPE`'s letter-code domain (Group G) — same blocker as the decision-table
   document; scenarios there confirm *which remapping table* applies but not what any individual
   code letter means to whatever consumes `OMGPR-C-INFRT-TYPE` downstream.
5. No DB2/CICS/COBOL execution environment — standing blocker across all three
   characterization-scenario documents produced so far.

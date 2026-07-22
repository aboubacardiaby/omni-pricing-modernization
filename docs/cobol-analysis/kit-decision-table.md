# Kit decision table — T048 finalization

Status: finalized from supplied `A6O011U.CBL`, `A6O012U.CBL`, and `A6O013U.CBL` on 2026-07-21.
The supplied programs resolve the former A6O012U/A6O013U evidence gap. Behavior owned by the
unsupplied A6O015U/OMGPK interface remains explicitly blocked and is not inferred.

## Confirmed call sequence

| Step | Program/paragraph | Confirmed behavior |
|---|---|---|
| 1 | A6O011U `0100-GET-CATEGORY` | Calls A6O013U for category, base UOM, conversion factor, inventory class, and product-category dates. |
| 2 | A6O013U `1000-VALIDATE-INPUT` | Rejects blank vendor and product; no kit explosion occurs here. |
| 3 | A6O013U `2000-READ-PRODUCT-DATA` | Calls A6O016U with requirement `C` and passes its product facts back unchanged. |
| 4 | A6O011U `0200-EXPLODE` | Calls A6O012U with request type `C`, pricing date, blank rollup switch, and the concatenated vendor/product pack number. |
| 5 | A6O012U `0015-VALIDATE` | Validates/defaults the explosion request, then calls A6O015U. |
| 6 | A6O012U `3000-MOVE-TO-OMGEXPL` | Shapes raw A6O015U rows into structure, level-one, or component-only output. |
| 7 | A6O011U `2000-PROCESS-PRICE` | Prices each returned component through A6U01, stops on the first component error, rolls up fields, prices kit sell, merges expiration dates, and applies kit alternate-UOM conversion. |

## A6O012U request validation

Checks are ordered; the first failure prevents later processing.

| Priority | Input | Accepted/default | Failure |
|---:|---|---|---|
| 1 | Pack product number | Required nonblank value | 61201, response `E` |
| 2 | Effective date | Blank defaults to current DB2 date; nonblank is validated by SYH208/SYU208 | 61202, response `E` |
| 3 | Rollup-cost switch | LOW-VALUES, blank, `Y`, or `N` | 61204, response `E` |
| 4 | Request type | `S`, `L`, or `C`; blank defaults to `C` | 61203, response `E` |

The initialization query failure is 61210 with response `A`. A6O015U CICS-link failures set
response `E` and message text but no numeric error. A6O012U treats response `A` and `E` alike:
both prevent explosion processing.

## Response-shaping decision table

Raw A6O015U types are `S` (sub-pack), `1` (level-one component), and `2` (level-two component).

| Request | Raw `S` | Raw `1` | Raw `2` |
|---|---|---|---|
| `S` structure | Remember parent slot/product/quantity; do not emit | Emit component, level 1, parent = top pack | Find previously seen parent slot; emit level 2 with parent product and multiplied quantity |
| `L` level one | Emit sub-pack (`S`) beneath top pack | Emit component, level 1, beneath top pack | Skip |
| `C` components (default and A6O011U mode) | Remember parent slot/product/quantity; do not emit | Emit component, level 1, beneath top pack | Find previously seen parent; emit level 2 with blank parent field and multiplied quantity |

Unknown raw row types are silently skipped despite the COBOL comment saying they are errors.
A level-two row whose parent slot cannot be found is also silently skipped.

## Nested quantity and ordering

A6O012U `4000-FIND-PARENT-PROD` matches the child row's numeric parent-slot reference against a
previously recorded sub-pack slot. For a match:

`emitted quantity = child quantity within one sub-pack * parent sub-pack quantity`

This is the NK1209 fix for incident 1667882 and must not be replaced with a flat quantity copy.
The single forward pass proves that A6O012U requires a parent `S` row to precede its level-two
children. Whether A6O015U guarantees that order is BLOCKED because its source is absent.

## Capacity behavior

- `PERFORM VARYING SUB FROM 1 ... UNTIL SUB >= 700` uses COBOL's default test-before behavior,
  so raw occurrences 1 through 699 can be processed; occurrence 700 is not processed. There is
  no overflow error or continuation marker for additional raw rows.
- The remembered sub-pack array has 300 occurrences, but the `S`-row population path increments
  and writes `SUB-SUBPACK` without checking it against 300. The behavior of a 301st sub-pack
  depends on compiler/runtime subscript checking and is therefore BLOCKED; it must not be
  described as safe truncation.
- Parent lookup itself searches no more than 300 remembered entries and stops at the first blank
  product slot.

## A6O011U component pricing and rollup

- A6O011U requests A6O012U's `C` component-only view.
- Each component is initialized, alternate-UOM facts are resolved, and A6U01 is called through
  `9000-LINK-TO-PRICER`. The first pricer error terminates the component loop.
- Component quantity multiplies total cost, dealer/acquisition cost, total adjusted cost,
  overhead, inbound freight, JIT buckets, vendor cost adjustment, rebate buckets, protected
  acquisition cost, and contract-line unit cost in `3000-COMPUTE-COST`.
- `3100-MOVE-COSTS` adds pack overhead plus third-party cost and sell fees to total cost. The
  third-party sell fee is removed from the cost basis used for sell pricing, then added directly
  to total sell, customer-UOM sell price, and unrounded total sell in `4000-COMPUTE-SELL`.
- Kit sell is obtained through A6U01 sell-only mode using UOM `KT`. JIT-on-cost may be called
  separately for A/C service modes and qualifying customers.
- Kit output suppresses selected component-level cost-adjustment display fields while retaining
  dedicated overhead/third-party output fields.
- A6O011U `4200-PROCESS-EXP-DATE` chooses the earliest of the component-derived expiration and
  the overhead, third-party-cost, and third-party-sell expiration dates.
- If ordered UOM differs from base UOM, `4300-HANDLE-ALT-UOM-KIT` multiplies the confirmed kit
  totals and itemized cost/rebate/JIT fields by the product conversion factor.

## Explicit blockers

| Blocked behavior | Missing evidence | Required implementation posture |
|---|---|---|
| How component rows are discovered and ordered | A6O015U source and OMGPK.CPY | T049 must initially wrap the legacy explosion dependency. |
| Meaning and production of `COMPLETE-EXPLODE-SW`, product type/status, and continuation fields | A6O015U/OMGPK interface | Preserve as opaque legacy response data; do not invent semantics. |
| Actual effect of the rollup-cost switch inside the explosion engine | A6O015U source | Forward the switch unchanged; perform no inferred explosion-time rollup. |
| Guarantee that parents precede children | A6O015U source or captured output | Preserve returned order and surface the dependency; do not reorder without an approved decision. |
| Runtime result of writing sub-pack occurrence 301 | Compiler options/runtime capture | Enforce a modern explicit limit rather than claiming a COBOL result not evidenced by source. |
| Exact SYH208/SYU208 date validation | Utility source | Use an adapter or separately approved date contract. |

All prior inferred kit-explosion behavior is now either confirmed above or explicitly blocked in
this table. No A6O015U behavior is treated as confirmed.

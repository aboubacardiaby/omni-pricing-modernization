# G4 Kit Parity Review

**Date:** 2026-07-21  
**Owner:** Codex  
**Reviewer:** Claude  
**Decision:** NOT APPROVED

## Gate criterion

Compare approved component and rollup cases and require exact COBOL/C# agreement for critical
fields and decision paths.

## Evidence inspected

- `upload/A6O011U.CBL`, especially `0200-EXPLODE`, `2000-PROCESS-PRICE`,
  `3000-COMPUTE-COST`, `3100-MOVE-COSTS`, `4000-COMPUTE-SELL`,
  `4020-GET-SELL`, `4200-PROCESS-EXP-DATE`, and `7000-INIT-FOR-COMP-COST`.
- `docs/cobol-analysis/kit-decision-table.md`.
- T048 through T052 implementation and focused unit tests.
- `tests/Pricing.ParityTests/BaselineTests.cs` and
  `tests/Pricing.CharacterizationTests/BaselineTests.cs`.
- Current T055 and T056 task state.

## Findings

1. **Critical — no executable kit parity evidence exists.** The parity project contains one
   assembly-availability smoke test. It has no approved kit fixtures, COBOL adapter/capture,
   component/rollup comparison, or decision-path assertion. T055 and T056 remain incomplete,
   so the number of actual COBOL/C# kit comparisons is zero. Self-authored unit expectations
   are not independent COBOL parity evidence.
2. **Critical — the C# sell path does not model the COBOL pack-sell call.** A6O011U
   `3100-MOVE-COSTS` derives `WS-PACK-CST-FOR-SELL`, and `4020-GET-SELL` calls A6U01 in sell-only
   mode with that rolled cost and kit UOM before adding the third-party sell fee. The C#
   `KitRollupCalculator` instead totals each component's regular-item `SellPrice` multiplied by
   component quantity and uses that sum plus the third-party fee as `TotalSell`. No separate
   pack-level sell-only pricing decision is executed. This can change the selected arrangement,
   method, hierarchy, adjustment path, and final sell.
3. **High — component quantity is applied at a different stage and can be applied twice.**
   A6O011U retains the caller's ordered quantity while pricing each component and applies
   `OMGEXPL-COMP-QTY` afterward in `3000-COMPUTE-COST`. The C# component service places the
   effective explosion quantity into each component `PricingRequest`, then the rollup calculator
   multiplies the returned amounts by that effective quantity again. Quantity-sensitive fees
   and decisions therefore cannot be assumed equivalent.
4. **High — expiration aggregation differs from the observed COBOL state flow.** A6O011U
   initializes the output fields on every component call, leaves the final component's
   `OMGPR-D-EXPIRATION`, and compares that value with the three root fee expirations in `4200`.
   The C# finalizer collects every component expiration and chooses the earliest together with
   the fee expirations. No approved decision documents this as an intentional divergence.

The T048 source analysis and T049-T052 unit tests demonstrate useful implementation coverage,
but the gate requires independent COBOL/C# comparisons. The three structural differences above
also need resolution or explicit approval before exact parity can be claimed.

## Verification executed

```powershell
$env:DOTNET_ROLL_FORWARD='Major'
dotnet test tests/Pricing.ParityTests/Pricing.ParityTests.csproj --no-restore --nologo
```

Result: 1 smoke test passed, 0 failed, and 0 COBOL/C# kit comparisons executed.

## Required evidence for re-review

1. Complete T055 with approved, sanitized kit cases containing authoritative COBOL inputs,
   component outputs, pack outputs, error outcomes, and decision paths.
2. Complete T056 with executable exact comparisons for component order/quantity, selected cost
   fields, pack cost basis, pack sell arrangement/method, fees, UOM conversion, expiration,
   errors, and provenance-equivalent decision paths.
3. Correct the pack-level sell and quantity staging differences, or record an approved
   intentional-divergence decision with corresponding expectations.
4. Resolve the component-expiration behavior against an authoritative runtime capture or an
   approved modernization decision.
5. Record per-scenario results and obtain Claude's independent fidelity review.

Until these requirements are satisfied, COBOL remains authoritative and G4 is not approved.

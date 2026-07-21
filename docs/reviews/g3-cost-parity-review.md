# G3 Cost Parity Review

**Date:** 2026-07-21  
**Owner:** Codex  
**Reviewer:** Claude  
**Decision:** NOT APPROVED

## Gate criterion

Run approved cost scenarios and require exact COBOL/C# agreement for critical values and the
decision path before sell implementation is declared complete.

## Evidence inspected

- A6U01 cost implementation and tests for T025 through T031.
- `tests/Pricing.ParityTests/BaselineTests.cs`.
- `tests/Pricing.CharacterizationTests/BaselineTests.cs`.
- T010 `docs/characterization/scenario-catalog.md` from discovery commit `9030fd3`.
- T007 cost-selection and T009 rebate/adjustment decision tables from discovery commit `9030fd3`.
- Current task state, including uncompleted T055 sanitized COBOL characterization fixtures.

## Findings

1. The parity project has one assembly-availability smoke test. It does not invoke COBOL or C#
   cost pricing and compares no critical fields or decision paths.
2. The characterization project likewise has one assembly-availability smoke test and contains
   no executable cost scenarios.
3. T010's scenario catalog explicitly excludes a comprehensive cost-selection matrix and says
   its expected outcomes were derived by static analysis rather than COBOL execution.
4. No sanitized COBOL cost fixtures with authoritative inputs, outputs, and decision paths are
   present. T055, which produces those fixtures, remains uncompleted.
5. No runnable COBOL/DB2/CICS adapter or captured COBOL outputs are available in this worktree.
6. Exact rounding parity cannot be asserted at tie boundaries because the discovery evidence
   still labels the platform rounding mode as inferred pending runtime confirmation.

The C# unit suite provides strong implementation-level coverage, but self-authored expected
values are not independent COBOL parity evidence. Consequently, no approved scenario can be
reported as an exact COBOL/C# match yet.

## Verification executed

```powershell
dotnet restore Pricing.sln --nologo
dotnet build Pricing.sln --no-restore --nologo
$env:DOTNET_ROLL_FORWARD='Major'
dotnet test Pricing.sln --no-build --no-restore --nologo
```

Results:

- Restore: succeeded.
- Build: succeeded with 0 warnings and 0 errors.
- Unit tests: 189 passed.
- Integration tests: 8 passed.
- Characterization tests: 1 smoke test passed.
- Parity tests: 1 smoke test passed.
- Total: 199 passed, 0 failed, but 0 executable COBOL/C# cost comparisons.

## Required evidence for re-review

1. Produce an approved, sanitized cost fixture set containing COBOL inputs, critical outputs,
   and expected decision paths for individual, account/customer, group/parent, special,
   healthcare, acquisition fallback, rebate, and vendor-adjustment cases.
2. Confirm the COBOL runtime rounding mode for parity-sensitive boundaries.
3. Add executable characterization/parity tests that run or consume authoritative COBOL output
   and compare contract, hierarchy/rule type, unit cost, UOM, rebate, price protection, vendor
   adjustment, total cost, expiration, exemptions, and legacy errors exactly.
4. Record per-scenario results and obtain Claude's independent fidelity review.

Until these items exist, COBOL remains authoritative and G3 remains not approved.

# Pricing Pipeline Infrastructure Gap

**Status**: CONFIRMED (verified directly against `src/` on `feature/legacy-price-operation-json`, 2026-07-28)
**Trigger**: `/api/v1/prices/calculate` and the legacy `PriceOperation` projection return `"NOT CONTRACTED"` with blank `contractType`/`sellType`/`effectiveDate` for nearly every product, even when a real contract should apply.

## Root cause

`Program.cs` registers `Pricing.Infrastructure.SqlServer.SqlServerPricingCalculationService` as `IPricingCalculationService`. That class is an intentionally narrow stand-in — its own doc comment says *"Prices the confirmed regular-item VNG03 fallback... CCG27 is intentionally excluded."* It runs one query against `VNG02`/`VNG03` (acquisition/dealer cost) and hardcodes `LegacyPricingDetails.VendorContractNumber = "NOT CONTRACTED"` for every row (`SqlServerPricingCalculationService.cs:137`). It never sets `ContractSelection` or `SellArrangementSelection`.

The full rule-based pipeline exists and is unit-tested end-to-end: `Pricing.Application.Orchestration.PricingCalculationService` → `PricingOrchestrator` → five ordered stages (context, cost, rebate, sell, fees), each backed by the ordered `ICostRule`/`ISellArrangementRule` chains built in Phases 5–7 (T024–T044, all marked `[x]`). **None of those stages, and 16 of the 17 repository interfaces they depend on, have a concrete production implementation in `Pricing.Infrastructure`.** Only `IKitExplosionRepository` does (`LegacyKitExplosionRepository`, and that wraps a legacy transport call, not SQL). This exact gap is already flagged as a blocker in `tasks.md` T060: *"the pricing repository contracts currently have no concrete DB2 query adapters in Pricing.Infrastructure."* T060 was written against DB2; the DB2→SQL Server migration in this branch did not close the gap — it only migrated the generic `ISqlServerQueryExecutor`/connection plumbing, not per-domain repositories.

**Consequence confirmed by grep**: registering `PricingCalculationService` in DI today throws at construction time — nothing satisfies `IPricingContextStage`, `ICostPricingStage`, `IRebatePricingStage`, `ISellPricingStage`, or `IFeePricingStage`.

## Stage-by-stage breakdown

| Stage interface | Wraps (existing, tested Application code) | Repository interfaces needed | SQL tables (from task evidence / COBOL refs) | Reusable from `SqlServerPricingCalculationService`? |
|---|---|---|---|---|
| `IPricingContextStage` | `ProductClassificationService`, `ProductInformationService`, `CustomerPricingContextService` (T020–T022) | `IProductClassificationRepository`, `IProductInformationRepository`, `ICustomerPricingContextRepository` | VNG02, VNG06, ING01, VNG05 (product); CUG03, CUG02/06/07/10/11/17/40/41/53, BGG03/23/24, CCG25 (account/customer/buying-group hierarchy, per T022 evidence) | Partial — `PriceQuery`'s VNG02/ING01/CUG03 joins cover a slice of product/account lookup, but buying-group membership, exclusions, and fee config (CUP100 A800–A850) are untouched. |
| `ICostPricingStage` | `CostRuleEvaluator` over `IndividualCustomerCostContractRule`, `BuyingGroupCostContractRule`, `SpecialContractCostRule`, `HealthcareCostOverrideRule`, `AcquisitionDealerCostFallbackRule` (T024–T029), then `VendorCostAdjustmentService` (T031) | `IIndividualCostContractRepository` (also used by `SpecialContractCostRule`), `IBuyingGroupCostContractRepository`, `IHealthcareCostOverrideRepository` (HCOVD* tables), `IAcquisitionCostRepository`, `IVendorCostAdjustmentRepository` | VNG03 (acquisition/dealer + individual/group contract rows), HCOVDACT/HCOVDCID/HCOVDGPD/HCOVDGRP/HCOVDPRD (healthcare override) | **Yes, for `IAcquisitionCostRepository` only.** `PriceQuery`'s VNG03 date-filtered, level-ordered lookup (A6U01 7105/7575) is essentially the fallback rule already. Rules 1–4 (the ones that actually populate `ContractSelection` and fix the "NOT CONTRACTED" symptom) have no query today. |
| `IRebatePricingStage` | `RebateCalculator` (T030) — static, operates on already-selected cost data | none (no new repository) | n/a | n/a |
| `ISellPricingStage` | Ordered `ISellArrangementRule`s (T033–T035), sell calculators (T036), `PriceLockService` (T037), sell-adjustment composition (T038) | `IAccountCustomerSellArrangementRepository`, `IBuyingGroupSellArrangementRepository`, `ICorporateSellArrangementRepository`, `IPriceLockRepository` | CUG* sell-arrangement/contract tables, CUG31 (price lock) | No — `characterizedCostPlus` in the current service is a single hardcoded characterization vector (one account/product pair), not a queryable rule; `HospitalListPrice` (VNG03 `A_VND_PRC_LST_HOSP`) is the only real reusable column, confirmed as the T036 list-price fallback source. |
| `IFeePricingStage` | `FreightEngine`, `JitFeeEngine` (static), `LowUomBreakBulkEngine`, `PandacAndSurgiTrakEngines`, `SurchargeAndMarkupEngines`, `AncillaryJitFeeComposer` (static) (T039–T044) | `IFreightRepository`, `ILowUomRepository`, `IPandacRepository`, `ISurchargeRepository` | Freight/JIT/PANDAC/SurgiTrak/surcharge tables per T039–T044 evidence (not yet inventoried against live table names here) | No — current service hardcodes all JIT/label/apply fees to `0.0000`. |

## Full repository interface inventory (17 total; 1 implemented)

| Interface | File | Concrete implementation | Notes |
|---|---|---|---|
| `IKitExplosionRepository` | `Kits/IKitExplosionRepository.cs` | ✅ `LegacyKitExplosionRepository` | Wraps legacy A6O012U transport, not SQL — precedent for "wrap legacy first" if a SQL path is blocked. |
| `IProductClassificationRepository` | `ProductClassification/` | ❌ | |
| `IProductInformationRepository` | `ProductInformation/` | ❌ | |
| `ICustomerPricingContextRepository` | `CustomerPricingContext/` | ❌ | Largest single query — full CUP100 hierarchy. |
| `IIndividualCostContractRepository` | `CostSelection/` | ❌ | Also consumed by `SpecialContractCostRule`. Highest-priority fix for the reported symptom. |
| `IBuyingGroupCostContractRepository` | `CostSelection/` | ❌ | |
| `IHealthcareCostOverrideRepository` | `CostSelection/` | ❌ | |
| `IAcquisitionCostRepository` | `CostSelection/` | ❌ | Query already exists in `SqlServerPricingCalculationService`; extract and adapt. |
| `IVendorCostAdjustmentRepository` | `CostAdjustments/` | ❌ | |
| `IAccountCustomerSellArrangementRepository` | `SellSelection/` | ❌ | |
| `IBuyingGroupSellArrangementRepository` | `SellSelection/` | ❌ | |
| `ICorporateSellArrangementRepository` | `SellSelection/` | ❌ | |
| `IPriceLockRepository` | `SellSelection/PriceLockService.cs` | ❌ | |
| `IFreightRepository` | `Fees/FreightEngine.cs` | ❌ | |
| `ILowUomRepository` | `Fees/LowUomBreakBulkEngine.cs` | ❌ | |
| `IPandacRepository` | `Fees/PandacAndSurgiTrakEngines.cs` | ❌ | |
| `ISurchargeRepository` | `Fees/SurchargeAndMarkupEngines.cs` | ❌ | |

## Sequencing recommendation

To fix the reported symptom (contract fields blank) with the smallest slice, the minimum viable path is:

1. `IPricingContextStage` (needs all three context repositories — can't select a cost rule without `PricingContext`).
2. `ICostPricingStage` with just `IIndividualCostContractRepository` + `IBuyingGroupCostContractRepository` + `IAcquisitionCostRepository` wired (healthcare/special can trail).
3. Register `PricingCalculationService` with these two stages real and `ISellPricingStage`/`IFeePricingStage` as thin pass-through stubs that carry cost forward unchanged — this alone would populate `ContractSelection` and stop the false `"NOT CONTRACTED"` everywhere, without claiming sell/fee parity is done.

Steps 4–5 (full sell/fee wiring) are a separate, larger effort and should not block step 3 from shipping.

## Draft task entries (for review before adding to `tasks.md`)

```markdown
## Phase 12 — Production Repository Adapters

- [ ] T064 [CODEX] Implement IProductClassificationRepository, IProductInformationRepository, and
      ICustomerPricingContextRepository against SQL Server and a concrete IPricingContextStage.
  - Depends on: T022, current SqlServer infra migration
  - Acceptance: PricingContext populated from real queries; existing T020-T022 unit tests still pass
    against the new adapters via integration tests; no behavior change to already-approved rule logic.

- [ ] T065 [CODEX] Implement IIndividualCostContractRepository, IBuyingGroupCostContractRepository,
      IHealthcareCostOverrideRepository, and IAcquisitionCostRepository against SQL Server; extract
      SqlServerPricingCalculationService's VNG03 query into IAcquisitionCostRepository.
  - Depends on: T025-T029, T064
  - Acceptance: ICostPricingStage implemented and wired; ContractSelection is populated end-to-end for
    a known-contracted characterization case (no more universal "NOT CONTRACTED").

- [ ] T066 [CODEX] Implement IVendorCostAdjustmentRepository and IRebatePricingStage.
  - Depends on: T030, T031, T065

- [ ] T067 [CODEX] Implement IAccountCustomerSellArrangementRepository, IBuyingGroupSellArrangementRepository,
      ICorporateSellArrangementRepository, IPriceLockRepository, and ISellPricingStage.
  - Depends on: T033-T038, T065

- [ ] T068 [CODEX] Implement IFreightRepository, ILowUomRepository, IPandacRepository, ISurchargeRepository,
      and IFeePricingStage.
  - Depends on: T039-T044, T067

- [ ] T069 [CODEX] Register Pricing.Application.Orchestration.PricingCalculationService as
      IPricingCalculationService in Program.cs, replacing SqlServerPricingCalculationService; retire or
      repurpose SqlServerPricingCalculationService once parity is confirmed.
  - Depends on: T064-T068
  - Acceptance: full solution test suite passes; G3/G4 parity gates re-run against the newly wired service.
```

## Open questions

- Live table names for freight/PANDAC/surcharge repositories (T068) were not independently re-verified against `upload/*.CPY` in this pass — confirm against T039–T044 evidence notes before implementation.
- Whether `SqlServerPricingCalculationService` should be deleted or kept as an explicit degraded-mode fallback (e.g., when DI-composition fails) is a product decision, not inferred here.

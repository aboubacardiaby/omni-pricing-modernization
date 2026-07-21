# Feature Specification: COBOL Pricing Modernization

**Feature branch**: `001-cobol-pricing-modernization`  
**Created**: 2026-07-18  
**Status**: Draft for execution  
**Input**: Modernize the supplied OMNI CICS/DB2 COBOL pricing subsystem to an explainable ASP.NET Core C# service while preserving behavior.

## User Scenarios and Testing

### User Story 1 — Price a regular product (P1)

As an order-processing consumer, I submit division, account, vendor, product, quantity, UOM, ship-to, bill-to, pricing date, and request type and receive the same cost, sell price, contract provenance, fees, and expiration as COBOL.

**Independent test**: Run an individual-contract case through COBOL and C# and compare all critical output fields.

**Acceptance scenarios**:

1. Given an effective individual contract, when a full-price request is submitted, then that contract is selected and cost/sell/expiration match COBOL.
2. Given no applicable contract, when pricing is requested, then acquisition/dealer cost fallback matches COBOL.
3. Given a cost-only, sell-only, or JIT request, then only the corresponding processing path executes.

### User Story 2 — Apply customer hierarchy and adjustments (P1)

As a pricing analyst, I need account, customer, buying-group, parent-group, exclusions, rebates, freight, JIT, and fee rules to execute in the same priority as COBOL.

**Independent test**: Execute representative cases for each hierarchy and adjustment, including a competing-rule case.

### User Story 3 — Price a kit or pack (P1)

As an order consumer, I submit a kit product and receive a component-based rollup with correct quantities, UOM conversion, overhead, third-party fees, errors, and earliest expiration.

**Independent test**: Price a known multi-component kit and compare each component plus final rollup.

### User Story 4 — Explain a price (P2)

As support or audit staff, I can see which cost rule, sell rule, contract, buying group, fee records, and dates produced a result without exposing sensitive data.

### User Story 5 — Compare COBOL and C# safely (P1)

As a migration operator, I can execute both engines, classify differences, measure parity, and keep COBOL authoritative until cutover approval.

### User Story 6 — Consume legacy OMGPR records (P2)

As an existing integration, I can exchange the 1,789-byte OMGPR format with correct alphanumeric, COMP, COMP-3, sign, scale, spaces, low values, and configured byte order.

## Functional Requirements

- **FR-001**: The system MUST validate required division, account, vendor, product, and pricing date values.
- **FR-002**: The system MUST default ship-to and bill-to suffixes exactly as verified from COBOL.
- **FR-003**: The system MUST classify regular versus kit products before pricing.
- **FR-004**: The system MUST build customer/account pricing context, including buying-group hierarchy, priorities, exclusions, fee configuration, and low-UOM eligibility.
- **FR-005**: The system MUST select cost rules in verified COBOL priority order.
- **FR-006**: The system MUST support individual, account/customer, group, parent-group, special, healthcare override, and acquisition-cost fallback paths where confirmed.
- **FR-007**: The system MUST calculate rebates and cost adjustments with COBOL-equivalent eligibility and rounding.
- **FR-008**: The system MUST select and calculate sell arrangements for fixed/stated, cost-plus, discount, margin, suggested, and fallback types where confirmed.
- **FR-009**: The system MUST implement price-lock behavior.
- **FR-010**: The system MUST calculate freight, JIT, PANDAC, SurgiTrak, low-UOM, break-bulk, label/application, extra-delivery, category surcharge, distribution, and markup rules where confirmed.
- **FR-011**: The system MUST collect applicable expiration dates and return the closest valid expiration.
- **FR-012**: The system MUST price kit components through the regular pricing path and roll up results.
- **FR-013**: The system MUST return typed modern errors while retaining mapped legacy error codes.
- **FR-014**: The system MUST retain rule and data provenance for each price component.
- **FR-015**: The system MUST encode and decode OMGPR without losing field fidelity.
- **FR-016**: The system MUST compare COBOL and C# results and classify differences.
- **FR-017**: The system MUST support shadow execution and controlled, reversible cutover.

## Non-Functional Requirements

- **NFR-001**: Money and percentage calculations use `decimal` only.
- **NFR-002**: API operations propagate cancellation and use asynchronous I/O.
- **NFR-003**: Logs are structured, correlated, and safe for customer data.
- **NFR-004**: The service exposes health, metrics, traces, and OpenAPI metadata.
- **NFR-005**: Performance targets are established from a measured COBOL baseline before cutover.
- **NFR-006**: Repository and rule tests run deterministically without production dependencies.
- **NFR-007**: A rollback switch can restore COBOL authority for every cutover scope.

## Key Entities

- PricingRequest
- PricingContext
- ProductInformation
- CustomerPricingContext
- CostSelection
- SellArrangementSelection
- PriceComponent
- PricingResult
- Contract
- BuyingGroupMembership
- FeeConfiguration
- HealthcareOverride
- KitComponent
- LegacyOmgprRecord
- PricingDifference

## Edge Cases

- Zero or negative rebates and adjustments
- Null, expired, same-day, leap-day, and end-of-month dates
- Alternative UOM and conversion-factor precision
- Multiple contracts with competing priorities
- Same final price from a different rule path
- Missing product or missing kit component
- More components than supported by legacy layout
- Partial kit failure and nested/sub-pack recursion
- Invalid packed decimal or wrong record length
- Database timeout, cancellation, and missing called-program data

## Success Criteria

- **SC-001**: 100% of supplied program/copybook fields and visible SQL operations are inventoried or explicitly blocked.
- **SC-002**: OMGPR round-trip test vectors preserve all confirmed values and produce exactly 1,789 bytes.
- **SC-003**: Critical parity fields match exactly for the approved characterization suite.
- **SC-004**: Every implemented pricing rule has source traceability and required rule tests.
- **SC-005**: Shadow execution reports parity by scenario, field, rule, customer scope, and product scope.
- **SC-006**: A controlled rollout can be enabled and rolled back without redeployment.

## Out of Scope

- Replacing or redesigning upstream order applications
- Changing business pricing policy
- Production DB2 schema redesign
- Removing COBOL before shadow and cutover gates pass
- Implementing behavior from unavailable dependencies without evidence

## Open Questions

1. What are the complete source and interfaces for A6O012U, A6O013U, A6W016U, and other missing called programs?
2. What mainframe character encoding and binary byte order apply to OMGPR?
3. What account rounding configurations exist in production?
4. What production performance and parity thresholds are required for cutover?
5. Which division/customer/product scope should be the first pilot?


# OMNI Pricing Modernization Constitution

## Core Principles

### I. COBOL Is the Behavioral Source of Truth
The supplied COBOL programs, copybooks, DB2 statements, and verified production examples define current pricing behavior. Implementations MUST NOT invent, simplify, or reorder pricing rules without an approved decision record. Every migrated rule MUST cite its source program, paragraph, copybook, or SQL operation.

### II. Parity Before Replacement
No C# capability may become authoritative until it passes characterization and COBOL/C# parity testing for its scope. Exact agreement is required for selected contract, buying group, rule type, error code, expiration date, and monetary values after COBOL-equivalent rounding. A matching final price does not excuse a different decision path.

### III. Decimal, Date, and Layout Fidelity
Money and percentages MUST use `decimal`, never floating-point types. COMP, COMP-3, signs, scale, byte ordering, fixed lengths, spaces, and low values MUST be handled explicitly. Effective-date boundaries, null expirations, closest-expiration selection, and rounding stage MUST match COBOL.

### IV. Modular Domain Design
`A6U01` MUST NOT become a monolithic C# translation. The solution MUST separate product classification, customer context, cost selection, rebates, sell arrangements, fees, overrides, expiration calculation, and kit pricing. Infrastructure dependencies MUST point inward through interfaces.

### V. Explainable Pricing
Every calculated price MUST retain provenance: rule name, source contract/record, hierarchy level, effective/expiration dates, and itemized adjustments. Production diagnostics MUST explain why a rule applied or was skipped without exposing sensitive information.

### VI. Test-First Rule Migration
Each migrated rule requires tests for: applies, does not apply, excluded, expired, zero value, higher-priority competitor, lower-priority competitor, and fallback. Boundary and failure tests are mandatory. Missing legacy dependencies MUST be recorded as blockers rather than guessed.

### VII. Incremental and Reversible Delivery
Migration proceeds through compatibility, shadow execution, measured parity, and controlled cutover. COBOL remains authoritative until an approved gate changes that status. Every cutover unit MUST have an immediate rollback path.

### VIII. Agent Isolation and Review
Claude owns COBOL discovery and behavioral specifications unless a task states otherwise. Codex owns C# implementation and automated tests unless a task states otherwise. Agents MUST work only on claimed tasks, avoid overlapping files, and hand off through committed artifacts. The non-owner reviews rule fidelity or implementation quality as specified in `tasks.md`.

## Technology and Quality Constraints

- Target runtime: .NET 8 or later approved LTS.
- API: ASP.NET Core with OpenAPI, ProblemDetails, health checks, structured logging, and OpenTelemetry.
- Data access: Dapper by default for parity-sensitive legacy SQL; EF Core only for justified aggregate persistence or CRUD.
- Tests: unit, integration, characterization, parity, performance, and failure tests.
- Security: authenticated API, authorization, input limits, audit correlation, and sensitive-data-safe logging.
- CI MUST run formatting, build, tests, analyzers, and dependency/security checks.
- No production database schema changes are implied by this specification.

## Delivery Gates

1. **Discovery gate**: program, call, copybook, SQL, field, and missing-dependency inventories reviewed.
2. **Compatibility gate**: OMGPR decode/encode test vectors pass and the 1,789-byte contract is reconciled.
3. **Cost gate**: representative cost scenarios reach approved parity.
4. **Sell/fee gate**: selling and fee scenarios reach approved parity.
5. **Kit gate**: component rollup, UOM, errors, and expiration reach approved parity.
6. **Shadow gate**: production-like dual execution meets agreed correctness and performance thresholds.
7. **Cutover gate**: scoped rollout, monitoring, fallback, and approval are ready.

## Governance

This constitution overrides informal implementation preferences. Amendments require: rationale, affected specs/tasks, parity risk, migration impact, and explicit approval. Pull requests MUST include COBOL traceability, assumptions, tests, comparison results, and rollback impact.

**Version**: 1.0.0  
**Ratified**: 2026-07-18  
**Last amended**: 2026-07-18


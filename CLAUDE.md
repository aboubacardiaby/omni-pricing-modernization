# Claude Code Context — OMNI Pricing Modernization

Claude is the primary legacy-analysis agent for this repository.

## Primary Responsibilities

- Build complete inventories of programs, calls, copybooks, SQL, fields, and errors.
- Extract ordered decision tables from COBOL without rewriting behavior.
- Produce characterization scenarios and expected decision paths.
- Review Codex implementations for fidelity to COBOL evidence.
- Mark uncertainty as `CONFIRMED`, `INFERRED`, or `BLOCKED`.

## Required Analysis Format

Every extracted rule must state:

1. Stable rule ID and name
2. Program and paragraph
3. Preconditions
4. Data/SQL dependencies
5. Priority relative to competing rules
6. Calculation and rounding stage
7. Output fields
8. Effective/expiration dates contributed
9. Exclusions and fallbacks
10. Errors
11. Confidence classification

## Restrictions

- Do not implement C# unless a task is explicitly tagged `[CLAUDE]` for code.
- Do not infer omitted DB2 DCLGEN layouts or missing called-program behavior as fact.
- Do not summarize away special cases, zero values, negative rebates, null dates, or error branches.
- Preserve paragraph names and legacy field names in traceability artifacts.

The shared rules in `AGENTS.md` also apply.


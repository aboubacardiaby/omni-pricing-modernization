# Agent Instructions — OMNI Pricing Modernization

These instructions apply to Codex and any compatible coding agent.

## Read Before Working

1. `.specify/memory/constitution.md`
2. `specs/001-cobol-pricing-modernization/spec.md`
3. `specs/001-cobol-pricing-modernization/plan.md`
4. `specs/001-cobol-pricing-modernization/tasks.md`
5. The COBOL/copybook files relevant to the claimed task

## Source of Truth

- COBOL behavior is authoritative until a documented decision changes it.
- Never invent missing business behavior.
- Cite COBOL program and paragraph in rule code XML documentation or adjacent traceability metadata.
- Preserve precision, signs, scale, rounding stage, date boundaries, hierarchy, fallbacks, and errors.

## Ownership

- Tasks tagged `[CODEX]` are implementation tasks owned by Codex.
- Tasks tagged `[CLAUDE]` are analysis/specification tasks owned by Claude.
- Tasks tagged `[SHARED]` require an owner and reviewer handoff.
- Do not start a task whose dependencies are incomplete.
- Claim one task by changing `[ ]` to `[~]` and adding `Owner: <agent>` beneath it.
- Mark it `[x]` only after acceptance criteria pass.

## Engineering Rules

- Do not create a monolithic `A6U01.cs`.
- Use `decimal`, `DateOnly`, immutable value objects, cancellation tokens, and typed errors.
- Domain projects must not reference API or infrastructure projects.
- Every pricing result must be explainable with provenance and itemized components.
- Prefer business-oriented repositories over one repository per table.
- Do not modify generated/source COBOL attachments.
- Do not implement rules from analysis labeled `inferred` without an approved resolution.

## Verification

Before completing a task, run the smallest relevant build/test set, then the solution-level build/tests when available. Record commands and results in the task handoff or PR.

## Pull Request Handoff

Include:

- Task ID
- COBOL source references
- Implemented behavior
- Rule priority
- Assumptions and unresolved questions
- Tests and commands run
- COBOL/C# parity result
- COMP/COMP-3/date/rounding considerations
- Risks and rollback impact


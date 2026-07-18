# OMNI Pricing Modernization Spec Kit

This repository contains an execution-ready GitHub Spec Kit for coordinated Claude Code and Codex work.

## Artifacts

- Constitution: `.specify/memory/constitution.md`
- Feature specification: `specs/001-cobol-pricing-modernization/spec.md`
- Technical plan: `specs/001-cobol-pricing-modernization/plan.md`
- Executable backlog: `specs/001-cobol-pricing-modernization/tasks.md`
- Research register: `specs/001-cobol-pricing-modernization/research.md`
- Data model: `specs/001-cobol-pricing-modernization/data-model.md`
- OpenAPI contract: `specs/001-cobol-pricing-modernization/contracts/pricing-api.yaml`
- Requirements checklist: `specs/001-cobol-pricing-modernization/checklists/requirements.md`
- Shared agent rules: `AGENTS.md`
- Claude context: `CLAUDE.md`

## Agent Workflow

1. Read the constitution, spec, plan, and task list.
2. Select the next unblocked task carrying the agent's tag.
3. Claim it in `tasks.md` before changing task-owned files.
4. Work only inside the documented ownership boundary.
5. Verify acceptance criteria.
6. Use the pull-request template and request the named cross-agent review.

## Spec Kit Commands

If the repository is initialized with the official `specify` CLI, use the standard lifecycle commands for future changes:

```text
/speckit.constitution
/speckit.specify
/speckit.clarify
/speckit.plan
/speckit.checklist
/speckit.tasks
/speckit.analyze
/speckit.implement
```

The initial artifacts are already populated. Run analyze before implementation to check cross-document consistency, then begin T001/T002/T003 with Claude and T011 with Codex.


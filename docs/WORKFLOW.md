# Codex Development Workflow

## 1. Read the guide and required documents

Read `AGENTS.md`, then `ARCHITECTURE.md`, `PROJECT_STRUCTURE.md`, `CONVENTIONS.md`, and `WORKFLOW.md`. Read the task-specific database, service catalog, communication, refactor, decisions, or development document as applicable.

## 2. Inspect source

Never change code based only on documentation. Inspect relevant controllers, Contracts/DTOs, services, repositories, persistence, configuration, DI, client consumers, and tests.

- Source is authoritative for **CURRENT** behavior.
- Accepted decisions are authoritative for **TARGET** direction.
- Documentation that conflicts with source must be corrected, not used to overwrite current behavior.

## 3. Determine CURRENT and TARGET impact

Record:

- Affected applications and current files.
- Current behavior and ownership.
- Relevant target layer/dependency rule.
- Route, JSON, authorization, database, transaction, cache, provider, Dashboard, and Mobile impact.
- Whether the task explicitly authorizes code, schema, provider, package, Docker, or external-state changes.

Changing the accepted MySQL/Pomelo provider or database-first schema workflow is a major persistence change and always requires an explicit architecture decision plus separate review of data conversion, SQL dialect, provider behavior, schema synchronization, rollout, rollback, and validation.

## 4. Create a scoped plan

Plan the smallest coherent change. Do not combine a focused task with unrelated refactoring. Resolve choices that would materially alter public behavior or architecture before implementation.

## 5. Implement

- Preserve routes and JSON schemas unless explicitly authorized.
- Preserve unrelated user changes.
- Introduce abstractions only with a concrete migrated use case.
- Maintain Presentation -> BLL abstractions and DAL -> BLL abstractions; never add BLL -> DAL dependencies.
- Keep generated files generation-driven.
- Never commit secrets or local `.env` values.

## 6. Build and test

- Build affected .NET projects; build the solution for cross-project changes.
- Run relevant xUnit tests.
- For Mobile, format-check, run `flutter analyze`, and run relevant/all Flutter tests.
- Verify client consumers after Contract changes.
- For database work, use provider/integration validation in addition to EF InMemory tests.

Documentation-only changes do not require application builds unless they alter executable files or a command needs validation. They still require source verification and consistency checks.

## 7. Review compatibility and diff

- Compare public routes, HTTP methods, authorization, JSON names, and envelopes.
- Review schema, transactions, cache invalidation, and provider behavior.
- Inspect `git diff` and `git status` for unrelated files, generated noise, and secrets.
- Confirm changes stay within the authorized file types.

## 8. Synchronize documentation

Update the owning document when architecture, structure, behavior, integration, schema, configuration, or development procedure changes. Clearly label CURRENT and TARGET; do not present a recommendation or future command as implemented.

## 9. Report

Report changed files, resulting behavior, verification, API/database impact, discovered inconsistencies, risks, and future implementation work that remains. Do not silently implement follow-up work outside scope.

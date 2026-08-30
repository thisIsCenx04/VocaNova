# VocaNova Agent Guide

## Project identity

VocaNova is a vocabulary-learning and testing system with three applications:

- `src/VocaNova.API`: ASP.NET Core 8 Web API and the only backend.
- `src/VocaNova.Dashboard`: ASP.NET Core 8 MVC administration client.
- `src/VocaNova.Mobile`: Flutter client using Riverpod, Dio, go_router, secure storage, and shared preferences.
- `tests/VocaNova.Tests`: xUnit tests for API and Dashboard; Flutter tests are under `src/VocaNova.Mobile/test`.

## Status vocabulary

- **CURRENT**: verified in repository source now.
- **TARGET**: accepted future architecture; do not describe it as implemented.
- **DECISION**: accepted architecture recorded in `docs/DECISIONS.md`.
- **RECOMMENDATION**: optional idea that has not been accepted.

Never change code based only on documentation. Inspect the relevant source first. Source is authoritative for CURRENT claims; accepted decisions are authoritative for TARGET claims.

## Mandatory reading order

Every session reads:

1. `AGENTS.md`
2. `docs/ARCHITECTURE.md`
3. `docs/PROJECT_STRUCTURE.md`
4. `docs/CONVENTIONS.md`
5. `docs/WORKFLOW.md`

Then read the task-specific document:

- Database: `docs/DATABASE.md`
- API/features: `docs/SERVICE_CATALOG.md`
- Client integration: `docs/COMMUNICATION.md`
- Refactoring: `docs/REFACTOR_PLAN.md`
- Architecture decisions: `docs/DECISIONS.md`
- Setup/build/run/test: `docs/DEVELOPMENT.md`

## CURRENT baseline

- The API is a transitional modular monolith. Notifications, Progress, all Dictionary and Lists/personal-topic endpoints, Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin use corrected feature-first Presentation/BLL/DAL slices under `Features/<Feature>`.
- Persistence is EF Core 8 with MySQL 8 and Pomelo. The workflow is database-first through `scripts/scaffold-mysql.ps1`; no EF migration set exists. `WordSense` now uses the reviewed `status` column and the same active/deleted global-filter pattern as `Word` and `Topic`.
- Redis is a cache/runtime-settings fallback, not the system of record.
- Dashboard and Mobile communicate with the API only through HTTP(S)/REST and own their wire models. Dashboard API wire DTOs live under `src/VocaNova.Dashboard/Data/Dtos`; Mobile feature wire DTOs live under `src/VocaNova.Mobile/lib/features/<feature>/data/dtos`, with remote HTTP gateways in `data/services` and client domain models in `domain/models`.
- Test-only compatibility cleanup is partially complete: Auth, Dictionary, and Lists still have local test compatibility helpers/aliases, while Progress, KNN, Quiz, and SuperAdmin adapters have been retired.
- Docker foundation is present and aligned with the accepted relational target: Compose defines `mysql`, `redis`, `api`, and `dashboard`; API/Dashboard have multi-stage Dockerfiles, and the API container receives `MYSQL_CONNECTION_STRING` for `mysql:3306`.

## TARGET architecture

The accepted architecture is system-level three-layer architecture:

```text
Presentation -> BLL abstractions
DAL          -> BLL abstractions
```

- Presentation: Dashboard, Mobile, and the API HTTP entry point (feature-owned `Controllers`, `Contracts`, `Mappings`, middleware, filters, HTTP behavior, and `Program.cs`).
- BLL: use cases, business rules/models/results, service abstractions, and required persistence/cache/authentication/storage/provider abstractions.
- DAL: feature repository implementations/mappings plus shared EF Core, `VocaNovaDbContext`, entities/configurations, MySQL/Pomelo, Redis, authentication/storage, and external-provider implementations under `Infrastructure`.
- `Program.cs` is the composition root and registers feature BLL/DAL through `AddBLL()` and `AddDAL(configuration)`.

The canonical target API layout is feature-first. Each `Features/<Feature>/` owns `Controllers`, `Contracts/Requests|Responses`, `Mappings`, `BLL/Abstractions|Models|Services`, and `DAL/Repositories|Mappings`. Repository interfaces remain BLL-owned; shared persistence, Redis, authentication, storage, providers, and other global infrastructure stay consolidated outside `Features/`. The backend remains a modular monolith. Dashboard and Mobile do not receive backend BLL/DAL folders or projects.

## Strict dependency and contract rules

- Controllers depend on BLL service abstractions, never repositories, `VocaNovaDbContext`, Redis, MySQL, or external-provider implementations.
- BLL must not depend on DAL, EF Core, Pomelo, MySQL-specific APIs, ASP.NET HTTP types, Redis implementations, or external SDK implementations.
- DAL implements BLL-owned abstractions and maps persistence/provider types to BLL models.
- HTTP `*Request`/`*Response` Contracts, BLL models, and DAL entities are distinct boundaries. Never expose EF entities over HTTP.
- Preserve public routes, HTTP behavior, and JSON schemas unless the task explicitly authorizes a breaking API change.
- Do not introduce microservices, internal REST/gRPC between features, message brokers, or distributed transactions without a separate accepted decision.

## Persistence and Docker targets

- CURRENT and TARGET database: MySQL 8 with EF Core 8 and `Pomelo.EntityFrameworkCore.MySql`.
- CURRENT and TARGET schema workflow: database-first synchronization through `scripts/scaffold-mysql.ps1`; the existing MySQL schema is the source of truth.
- BLL remains provider-independent even though DAL uses MySQL/Pomelo.
- CURRENT and TARGET Docker services are exactly `mysql`, `redis`, `api`, and `dashboard`; Flutter remains outside Docker. MySQL uses the named `mysql_data` volume, Redis is non-persistent, the API connects to `mysql:3306`, and Dashboard uses `http://api:8080`.

Database/provider/schema changes and Docker implementation require separately scoped implementation tasks. Never infer authorization from target documentation.

## Change policy

Before editing:

1. Verify scope and behavior in source.
2. Identify affected applications/layers and API, database, cache, and client impact.
3. Make the smallest coherent change and preserve unrelated work.
4. Build/test affected .NET projects and run Flutter format/analyze/tests when relevant.
5. Review compatibility and `git diff`.
6. Synchronize documentation when architecture, behavior, structure, integration, schema, or development procedure changes.

Never commit secrets or local `.env` values. `.env.example` contains placeholders only. Do not hand-edit generated Flutter files such as `*.g.dart` or generated localization files.

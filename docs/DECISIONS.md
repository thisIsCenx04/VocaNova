# Architecture Decisions

Statuses use **Accepted**, **Superseded**, or **Proposed**. CURRENT implementation facts do not override an accepted TARGET decision.

## ADR-001: System-level three-layer architecture

Status: Accepted

Decision: Apply Presentation, BLL, and DAL across the system. Presentation depends on BLL abstractions, and DAL implements and depends on BLL abstractions. BLL never depends on DAL.

Consequences: The API owns backend BLL/DAL responsibilities. Existing feature-first coupling is migrated incrementally.

## ADR-002: Dashboard and Mobile are Presentation clients

Status: Accepted

Decision: Dashboard and Mobile call the API only through HTTP(S)/REST and own language-specific wire models. They may own UI workflows/state, client models, local storage, and API gateways, but do not receive backend BLL/DAL or database/cache access.

## ADR-003: REST boundaries use Contracts

Status: Accepted

Decision: Public HTTP input/output types use `*Request` and `*Response` under `Contracts`. HTTP Contract, BLL model, and DAL entity are distinct. Existing `Dto` classes remain CURRENT until safely classified; do not create new generic DTO types.

Consequences: Presentation and DAL mappings become explicit while routes and JSON schemas remain compatibility surfaces.

## ADR-004: BLL owns required abstractions

Status: Accepted

Decision: BLL owns service, persistence, caching, authentication, storage, and external-provider abstractions required by use cases. DAL implements them and maps internal representations.

Consequences: BLL must be independent of EF Core, MySQL/Pomelo APIs, ASP.NET HTTP types, Redis implementations, DAL entities, and provider SDK/transport implementations.

## ADR-005: Program.cs is the composition root

Status: Accepted

Decision: `Program.cs` may see Presentation, BLL, and DAL to compose the API. The target registration form is conceptually `AddBLL()` and `AddDAL(configuration)`.

Consequences: The composition extensions now exist as CURRENT implementation after the ordered feature slices were migrated. `Program.cs` still owns HTTP framework setup, middleware ordering, authentication/authorization wiring, Swagger, and endpoint mapping, while `AddBLL()` and `AddDAL(configuration)` group feature BLL/DAL and shared infrastructure registrations.

## ADR-006: Backend remains a modular monolith

Status: Accepted

Decision: Keep one backend deployment and in-process feature calls. Do not introduce microservices, internal REST/gRPC, message brokers, or distributed transactions without a separate decision.

## ADR-007: Clients do not share API source assemblies

Status: Accepted

Decision: Dashboard and Mobile keep client-local wire models and communicate only over REST. API, Dashboard, and Mobile can evolve/build independently while honoring public contracts.

## ADR-008: Canonical target API physical layout

Status: Superseded

Decision: The target API uses top-level `Controllers`, `Contracts`, `Mappings`, `BLL`, and `DAL`. Feature grouping exists inside each layer, for example `Controllers/Auth`, `BLL/Services/Auth`, and `DAL/Persistence/Repositories/Auth`.

Consequences: Current `Features/` and `Infrastructure/` remain documented as CURRENT but are not target roots. The former ambiguity with feature roots containing controllers/services/repositories is resolved.

Superseded by: ADR-018. The system-level dependency direction remains accepted, but the physical layout changes to corrected feature-first slices.

## ADR-009: OpenAPI as a generated client source

Status: Proposed

Decision: None yet. Evaluate generation/contract verification after Contracts are separated and stable.

## ADR-010: MySQL database-first persistence

Status: Accepted

Decision: Treat the existing MySQL schema as the source of truth and synchronize the EF Core model with Pomelo `dbcontext scaffold` through `scripts/scaffold-mysql.ps1`.

Consequences: MySQL/Pomelo/database-first describe both CURRENT persistence and the accepted TARGET workflow. Run forced scaffolding only for explicitly authorized schema synchronization and review every generated diff. ADR-016 reaffirms this as the long-term relational direction.

## ADR-011: PostgreSQL and Npgsql are the target relational stack

Status: Superseded

Decision: Replace MySQL/Pomelo with PostgreSQL and `Npgsql.EntityFrameworkCore.PostgreSQL` on EF Core 8. DAL owns all provider-specific code; BLL remains provider independent.

Consequences: Migration requires a separately reviewed provider/schema/data task. Existing MySQL behavior remains CURRENT until cutover is verified.

Superseded by: ADR-016. No PostgreSQL/Npgsql cutover is planned.

## ADR-012: Code First with EF Core Migrations is the target schema workflow

Status: Superseded

Decision: DAL persistence entities, configurations, `VocaNovaDbContext`, and EF migrations become the schema definition/source of truth. Do not retain database-first as a simultaneous target workflow.

Consequences: Establish an intentional baseline/initial migration and data migration plan before retiring `scaffold-mysql.ps1`.

Superseded by: ADR-016. This decision depended on the abandoned PostgreSQL migration; database-first remains the accepted workflow on MySQL.

## ADR-013: Redis remains cache infrastructure

Status: Accepted

Decision: Redis implementations belong to DAL behind BLL-owned cache abstractions. Redis is not the system of record; cache failure does not redefine business truth.

## ADR-014: Docker Compose is the target backend environment

Status: Accepted

Decision: Target Compose services are exactly `mysql`, `redis`, `api`, and `dashboard`. Containers use service DNS names. MySQL uses the named `mysql_data` volume. External providers remain HTTPS dependencies.

Consequences: The CURRENT Compose file now matches this service decision: `mysql`, `redis`, `api`, and `dashboard`. The API receives the existing MySQL settings for `mysql:3306`, and MySQL data is stored in `mysql_data`. Compose does not change the database-first schema workflow or create EF migrations.

## ADR-015: Flutter Mobile remains outside Docker

Status: Accepted

Decision: Mobile runs through normal Flutter tooling on emulators, simulators, or physical devices and calls the host-exposed API endpoint. It is not a Compose service.

## ADR-016: MySQL, Pomelo, and database-first are the long-term relational stack

Status: Accepted

Decision: Keep MySQL 8 with EF Core 8 and `Pomelo.EntityFrameworkCore.MySql` as VocaNova's relational stack. The existing MySQL schema remains the source of truth, and `scripts/scaffold-mysql.ps1` remains the accepted database-first synchronization workflow. This is the long-term TARGET, not a transitional or historical state.

Consequences: PostgreSQL/Npgsql and Code First migrations are no longer planned. DAL continues to isolate provider-specific persistence from BLL. Docker's CURRENT and TARGET relational service is `mysql`; schema creation remains database-first and outside EF migrations.

Supersedes: ADR-011 and ADR-012. Reaffirms ADR-010 and updates the relational part of ADR-014.

## ADR-017: BLL-owned explicit transaction lifecycle for cross-repository use cases

Status: Accepted

Decision: BLL owns `IApplicationTransactionManager` and `IApplicationTransaction`. A BLL use case explicitly begins, saves, commits, or rolls back a provider-neutral transaction; the transaction is asynchronously disposable and rolls back when disposed before a successful commit. DAL implements the abstractions with an EF Core transaction and `SaveChangesAsync` on the same scoped `VocaNovaDbContext` used by the participating repositories. Participating repositories query and stage changes but do not own transaction completion.

Consequences: Auth registration, new Google-account creation, refresh-token rotation, password reset, and account deletion retain an explicit atomic boundary through BLL-owned transaction abstractions. SuperAdmin mutations use the same boundary when account/auth/profile/role/token state changes together. External provider calls occur outside database transactions, and cache invalidation occurs only after commit. Lists is outside this decision: Unit 2 preserves random-add and personal-topic get-or-create as independent-save sequences with their existing partial-write risk. Any future Lists atomicity change requires a separate accepted decision.

## ADR-018: Corrected feature-first API physical layout

Status: Accepted

Decision: Organize feature-owned API code under `Features/<Feature>` while preserving the system-level Presentation/BLL/DAL dependency direction from ADR-001 and the BLL ownership rule from ADR-004. Each feature uses this canonical shape as needed:

```text
Features/<Feature>/
|-- Controllers/
|-- Contracts/
|   |-- Requests/
|   `-- Responses/
|-- Mappings/
|-- BLL/
|   |-- Models/
|   `-- Services/
`-- DAL/
    |-- Repositories/
    |   `-- Interfaces/
    `-- Mappings/
```

Public HTTP types use `*Request` and `*Response`; no new `DTOs` folder or generic `Dto` suffix is introduced. Request validators remain Presentation concerns and may live beside the feature's Request Contracts. Business result/error types remain inside the feature BLL model boundary. Mapping is explicit/manual by default; Mapster or AutoMapper requires a case-specific reason and is not mandated.

Repository and other use-case-required interfaces belong logically to BLL abstraction namespaces. CURRENT source stores those interface files physically under `Features/<Feature>/DAL/Repositories/Interfaces` while declaring `Features.<Feature>.BLL.Abstractions`; only concrete implementations belong to `Features/<Feature>/DAL/Repositories`. DAL mapping belongs to the feature DAL slice. Controllers depend on BLL service abstractions, never DAL implementations.

Shared persistence and infrastructure remain consolidated outside feature folders. `VocaNovaDbContext`, scaffolded entities, EF configurations, database-first synchronization, migrations if ever introduced by a separate decision, and transaction coordination stay under shared `Infrastructure/Persistence` or an equivalent shared DAL location. Redis, authentication, storage, auditing, runtime configuration, rate limiting, OTP, SMS, and external-provider implementations also remain shared infrastructure. Cross-feature, layer-safe primitives remain under `Common`.

Consequences: The ordered feature migration units now use corrected feature-first slices without changing public behavior. Physical feature grouping does not permit BLL-to-DAL dependencies; repository/cache/provider interface files sit in `DAL/Repositories/Interfaces` but keep BLL abstraction namespaces and BLL-only signatures. Shared persistence/provider implementations remain consolidated under `Infrastructure`, and `AddBLL()`/`AddDAL(configuration)` are the CURRENT grouped registration surface.

Supersedes: ADR-008. ADR-001 through ADR-007, ADR-010, ADR-013 through ADR-017 remain in force.

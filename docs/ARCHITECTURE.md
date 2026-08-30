# Architecture

## Status vocabulary

- **CURRENT**: verified in source now.
- **TARGET**: accepted future architecture, not yet implemented.
- **DECISION**: accepted architecture recorded in `DECISIONS.md`.
- **RECOMMENDATION**: optional future idea, not accepted architecture.

## System overview (CURRENT)

```text
ASP.NET Core MVC Dashboard ---- HTTP(S)/JSON ----+
                                                  |
Flutter Mobile --------------- HTTP(S)/JSON ----> VocaNova.API
                                                  |
                                  +---------------+----------------+
                                  |               |                |
                                MySQL           Redis       External providers
                                                              Google, Gemini,
                                                           Cloudinary, SpeedSMS
```

- `VocaNova.API` is the only backend and the only application that accesses MySQL, Redis, and backend providers.
- `VocaNova.Dashboard` is a server-rendered administration Presentation client with no API project reference or database access. Dashboard-owned API wire DTOs live in `Data/Dtos`, while Razor view/support models stay under `Models`.
- `VocaNova.Mobile` is a Flutter Presentation client using Riverpod and Dio. Feature HTTP access lives in `data/services/*ApiService`, feature wire models live in `data/dtos/*Dto`, and services map DTOs to `domain/models` before state/UI consumption.
- The API is a transitional modular monolith. Notifications, Progress, all Dictionary and Lists/personal-topic endpoints, Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin use corrected feature-first Presentation/BLL/DAL slices under `Features/<Feature>`.
- Docker Compose and API/Dashboard Dockerfiles are present. Compose provisions MySQL, Redis, API, and Dashboard; the API container uses MySQL/Pomelo through `MYSQL_CONNECTION_STRING=Server=mysql;Port=3306;...`.
- There is no microservice transport, gRPC, distributed message broker, or distributed transaction design.

Current API flow:

```text
Client -> middleware -> feature Controller -> feature Service
       -> feature Repository / Infrastructure -> MySQL, Redis, or provider
       -> Result<T> -> HTTP response envelope -> Client
```

Controllers inject service abstractions. Migrated feature BLL services depend on BLL-owned ports rather than EF entities, `VocaNovaDbContext`, or DAL implementations. Shared infrastructure implementations, provider clients, authentication helpers, Redis caches, and EF persistence remain consolidated under `Infrastructure`, and `Program.cs` composes feature BLL/DAL registration through `AddBLL()` and `AddDAL(builder.Configuration)`.

### Notifications three-layer pilot (CURRENT)

Notifications is the first implemented end-to-end slice of the target architecture:

```text
Controllers + Contracts + Mappings (Presentation-owned)
    -> BLL service abstraction, models, result, and persistence port
    <- DAL EF repository and persistence mapping
```

- `GET /api/notifications`, Bearer authorization, the `user_id` claim, response envelope, pagination metadata, and JSON field names are unchanged.
- BLL owns the derived-notification rule and is independent of ASP.NET Core, EF Core, `Infrastructure`, and DAL.
- DAL owns the EF query and maps `Word` persistence rows to a BLL model. It temporarily uses the existing MySQL/Pomelo `VocaNovaDbContext` under `Infrastructure/Persistence`; moving shared persistence types is a later phase.
- `Program.cs` composes the BLL interfaces with the DAL implementation through the shared `AddBLL()` and `AddDAL(builder.Configuration)` registration extensions.
- Architecture tests enforce the pilot dependency direction and scan BLL source for forbidden outer-layer/framework references.

### Progress three-layer slice (CURRENT)

Progress is the second migrated end-to-end feature. It uses Presentation-owned controllers/Contracts/mappings, framework-neutral BLL services/models/results and persistence/cache ports, plus DAL EF and Redis implementations.

- All five `/api/progress/**` GET endpoints, Bearer authorization, the `user_id` claim, envelopes, status behavior, query fields, route constraints, and snake_case JSON fields are unchanged.
- BLL owns streak, seven-day accuracy, chart period/bucket, mastery-level completion, weakest-word accuracy, validation, and not-found rules.
- DAL owns EF queries/projections over `TestSession`, `TestAnswer`, `UserWordProgress`, and `Word`, while continuing to use the current MySQL/Pomelo `VocaNovaDbContext`.
- The BLL-owned summary cache port is implemented by DAL Redis. The cache key, 15-minute TTL, snake_case payload, unavailable-Redis fallback, and Quiz invalidation timing are preserved.
- Progress is read-only and introduces no transaction. The migrated Quiz slice preserves its single-save answer/session/SRS persistence semantics and SM-2 behavior.

### Public Dictionary read-side slice (CURRENT)

The five anonymous public word/topic GET endpoints now use Presentation-owned request/response Contracts and mappings, framework-neutral BLL read services/models/results and persistence/cache ports, and DAL EF/Redis implementations.

- Routes, query names/defaults, status behavior, messages, snake_case response fields, and the existing paged object inside `data` are unchanged.
- BLL owns search/filter normalization and validation, daily-word selection, not-found behavior, cache-key composition, and cache orchestration.
- DAL owns MySQL/Pomelo EF queries and entity mapping for active words/topics, details, daily candidates, counts, ordering, filtering, and pagination.
- DAL Redis implementations preserve the existing word-search/detail/topic keys, 5/30/60/10-minute TTLs, snake_case payloads, unavailable-Redis fallback, and topic invalidation behavior. Dictionary administration uses the same BLL-owned cache ports for invalidation.
- This slice remains read-only and introduces no transaction. Dictionary administration is now implemented by the companion feature-first write slice below.

### Dictionary administration slice (CURRENT)

Dictionary word/topic administration now uses Presentation-owned request/response Contracts and mappings, framework-neutral BLL admin services/models/results and repository/storage/cache ports, plus DAL EF repositories/mappings and shared Cloudinary/Redis implementations.

- All existing `/api/admin/words/**` and `/api/admin/topics/**` routes, Admin/SuperAdmin policies, messages, envelopes, pagination placement, JSON/form fields, CSV row outcomes, Cloudinary ordering, and Dashboard-owned DTO parsing are unchanged.
- Word writes still invalidate word-detail and affected user-list entries without clearing the word-search cache. Topic invalidation remains operation-specific. Existing independent-save ordering for CSV, examples, topic links, and provider side effects remains unchanged.
- MySQL `word_senses.status` is `varchar(20) NOT NULL DEFAULT 'active'`, indexed by `idx_senses_status`. EF applies the active/deleted global filter; sense delete/restore verifies word ownership, saves once, and invalidates the word-detail cache.
- Architecture, controller-contract, cache-invalidation, Dashboard compatibility, feature behavior, and real-MySQL rollback tests enforce the slice boundaries and compatibility.

### Lists and personal-topic slice (CURRENT)

All authenticated Lists/personal-topic read and mutation endpoints now use Presentation-owned request/response Contracts and mappings, framework-neutral BLL query/mutation services, models, results, and persistence/cache ports, and DAL EF/Redis implementations.

- Routes, Bearer authorization, the `user_id` claim, query names/defaults, envelopes, messages, snake_case response fields, and paged objects inside `data` are unchanged.
- BLL-owned lookup results preserve `404 List not found.` for zero, missing, deleted, or reserved lists, `403 You do not have access to this list.` for foreign active ordinary lists, and `404 Word not found.` for an invalid personal-topic word filter without exposing HTTP types below Presentation.
- DAL owns MySQL/Pomelo EF ownership checks, word/topic projections, ordering, filtering, and pagination. The DAL Redis implementation preserves `user-lists:v2:{userId}`, its 10-minute TTL, snake_case payload, and unavailable-Redis fallback.
- Mutation ownership reuses the same explicit lookup result, so missing/deleted/reserved lists remain 404 and foreign active ordinary lists remain 403. Successful route, envelope, JSON, validation, soft-delete, and cache behavior is unchanged.
- Unit 2 is a structural migration only. Random-add still saves and invalidates once per successfully added word; personal-topic get-or-create can save the reserved list before its membership. Neither sequence has an encompassing transaction, and induced later failures can leave earlier writes committed. Note-only updates retain the CURRENT behavior of not invalidating the list-summary cache.

### Quiz and AI-grading slices (CURRENT)

Quiz and AI grading now use feature-owned Presentation Contracts/controllers/mappings, framework-neutral BLL services/models/results and repository/cache/provider ports, feature DAL EF repositories/mappings, and shared Redis/Gemini/runtime-configuration adapters.

- All seven authenticated Quiz routes and four Admin-authorized AI-grading settings routes retain their messages, envelopes, query defaults, and snake_case JSON fields. Mobile Quiz and Dashboard AI-settings wire models remain unchanged.
- Quiz answer, session, and SM-2/SRS changes are staged behind a BLL-owned aggregate and applied by DAL through one relational save. AI-cache hit accounting or a new Gemini-result write can still save independently before that later Quiz save; ADR-017's ordering remains unchanged and no encompassing transaction was added.
- Redis quiz pools retain `${prefix}quiz-pool:{sessionId}:{listId|all}`, the two-hour TTL, payload/removal behavior, and unavailable-Redis fallback. Successful session creation/answer persistence retains Progress-summary invalidation timing.
- Gemini-produced grades retain the SHA-256-keyed seven-day MySQL cache, hit/miss accounting, retries, model fallback, per-attempt timeout, threshold behavior, and non-cached normalized exact-match degradation when Gemini is unavailable.
- Architecture, controller-contract, transaction-order, provider/cache behavior, Mobile Quiz, and Dashboard client compatibility tests enforce these boundaries.

### Admin and SuperAdmin slices (CURRENT)

Admin reporting/user-management and SuperAdmin account/role endpoints now use feature-owned Presentation Contracts/controllers/mappings, framework-neutral BLL services/models/repository ports, feature DAL EF repositories, and shared authentication/auditing/profile-cache infrastructure.

- All existing Admin and SuperAdmin routes, policies, messages, envelopes, pagination placement, query names, and JSON fields are unchanged.
- Admin status changes stage account status and refresh-token revocation through BLL-owned repository ports, save once, and invalidate the profile cache after the save.
- SuperAdmin account and role mutations use BLL-owned repository ports and the shared transaction manager for multi-entity mutations, then invalidate affected profile cache entries after commit.
- Architecture tests now enforce Admin/SuperAdmin controller boundaries, DAL-to-BLL port implementation, and BLL source independence from ASP.NET Core, EF Core, `VocaNovaDbContext`, feature DAL, and shared Infrastructure implementations.

## System-level three-layer architecture (TARGET)

VocaNova remains one backend modular monolith with two independent clients.

```text
Presentation -> BLL abstractions
DAL          -> BLL abstractions
```

The arrow from DAL points toward BLL because DAL implements interfaces owned by BLL. BLL must never depend on DAL.

### Presentation

Presentation consists of:

- `VocaNova.Dashboard`.
- `VocaNova.Mobile`.
- The API HTTP entry point: feature-owned `Controllers`, `Contracts`, Presentation `Mappings`, middleware, filters, HTTP-specific behavior, and `Program.cs`.

Presentation validates transport input, maps Request Contracts to BLL models/commands, invokes BLL service abstractions, maps BLL results to Response Contracts, and selects HTTP/UI responses. Dashboard and Mobile remain REST clients and do not receive backend BLL or DAL projects/folders.

### Client DTO Boundaries

Dashboard and Mobile each own their client-side DTOs for API JSON parsing. Those DTOs mirror the API's public JSON contract but are not shared source files with the backend:

```text
API Contracts/Responses -> JSON -> Dashboard Data/Dtos -> Dashboard Models/ViewModels
API Contracts/Responses -> JSON -> Mobile data/dtos -> Mobile domain/models -> application state
Dashboard form/view input -> Dashboard Data/Dtos -> JSON -> API Contracts/Requests
Mobile application input -> Mobile data/dtos -> JSON -> API Contracts/Requests
```

The API keeps `Contracts/Requests|Responses` because those files are the backend's public HTTP boundary. Dashboard and Mobile use `Dtos` because they are client-owned transport/cache payload shapes.

### BLL

BLL owns use cases, business rules, business models, business results/errors, service abstractions, and the persistence, cache, authentication, storage, and external-provider abstractions required by those use cases.

BLL is framework/provider independent. It must not use DAL, EF Core, `VocaNovaDbContext`, DAL entities, Pomelo, MySQL-specific APIs, ASP.NET HTTP types, StackExchange.Redis implementations, Cloudinary/Gemini/Google/SpeedSMS SDK or transport implementations.

### DAL

DAL owns feature-specific repository implementations/mappings plus shared infrastructure. Shared EF Core, `VocaNovaDbContext`, persistence entities/configurations, MySQL/Pomelo integration, Redis, authentication, storage, Gemini, Google, Cloudinary, SMS, and other provider implementations remain consolidated under `Infrastructure` rather than being duplicated inside features.

## Canonical API physical layout (TARGET)

ADR-018 accepts corrected feature-first slices while retaining the same dependency direction:

```text
src/VocaNova.API/
|-- Features/
|   `-- <Feature>/
|       |-- Controllers/
|       |-- Contracts/
|       |   |-- Requests/
|       |   `-- Responses/
|       |-- Mappings/
|       |-- BLL/
|       |   |-- Abstractions/
|       |   |-- Models/
|       |   `-- Services/
|       `-- DAL/
|           |-- Repositories/
|           `-- Mappings/
|-- Infrastructure/
|   |-- Persistence/{Configurations,Entities,Transactions}/
|   |-- Caching/
|   |-- Authentication/
|   |-- Auditing/
|   |-- Configuration/
|   |-- Otp/
|   |-- RateLimiting/
|   |-- Sms/
|   `-- Storage/
|-- Common/
|-- Middleware/
|-- Filters/
|-- DependencyInjection/
`-- Program.cs
```

Feature Contracts never use a `DTOs` folder or new `Dto` suffix. Request validators are Presentation concerns and may live beside Request Contracts. Repository/cache/provider interfaces required by a use case remain under its BLL abstractions; repository implementations alone live in the feature DAL. Mapping is explicit/manual by default and no mapping library is mandated.

Notifications, Progress, all Dictionary endpoints, Lists/personal-topic endpoints, Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin now use this physical layout. Shared `VocaNovaDbContext`, entities/configurations, Redis, authentication, storage, and providers remain outside `Features`.

## Dependency rules (TARGET)

Allowed:

```text
Feature Controller          -> same-feature BLL service abstraction
Feature BLL service         -> BLL-owned repository/provider/cache abstraction
Feature DAL implementation  -> same-feature BLL abstraction
Program.cs       -> Presentation + BLL + DAL registration
```

Forbidden:

- BLL -> DAL, EF Core, Pomelo/MySQL-specific APIs, Redis implementations, ASP.NET HTTP types, or external SDK implementations.
- Controller -> repository, DbContext, Redis, MySQL, or external-provider implementation.
- Direct serialization of EF entities over HTTP.

`Program.cs` is the composition root. CURRENT registration uses `AddBLL()` and `AddDAL(builder.Configuration)` extension methods, with framework bootstrapping and middleware ordering still visible in `Program.cs`.

### Transaction boundary (TARGET)

For cross-repository Auth and SuperAdmin use cases, BLL owns the provider-neutral `IApplicationTransactionManager`/`IApplicationTransaction` lifecycle accepted by ADR-017. DAL implements its save/commit/rollback operations with an EF transaction on the same scoped `VocaNovaDbContext` as participating repositories. BLL explicitly saves and commits only after all relational steps succeed; disposal before commit rolls back, external provider calls remain outside the transaction, and cache invalidation occurs after commit. Lists random-add and personal-topic get-or-create are not part of that accepted design: their CURRENT independent-save behavior is preserved, and any future atomicity change requires a separate decision.

## Model and mapping boundaries (TARGET)

```text
Request Contract -> Presentation mapping -> BLL model/command -> BLL use case
  -> BLL abstraction -> DAL implementation -> DAL mapping -> entity/provider model

entity/provider result -> DAL mapping -> BLL model/result
  -> Presentation mapping -> Response Contract -> JSON
```

An HTTP Contract is not a BLL model, and a BLL model is not a DAL entity. Public HTTP types use `*Request` and `*Response`, not new generic `Dto` types. Explicit/manual mapping is the default; AutoMapper is not mandated.

## Persistence and deployment status

- CURRENT and TARGET relational runtime is MySQL/Pomelo/database-first. The existing MySQL schema is the source of truth and `scripts/scaffold-mysql.ps1` is the accepted synchronization workflow.
- CURRENT Docker foundation defines exactly `mysql`, `redis`, `api`, and `dashboard`, uses health-based dependencies, stores MySQL data in `mysql_data`, disables Redis persistence, and keeps Flutter outside Docker.
- CURRENT Dashboard container calls `http://api:8080`; exposed host defaults are API 5013 and Dashboard 5236.
- TARGET Compose services are the same: `mysql`, `redis`, `api`, and `dashboard`; MySQL uses the named `mysql_data` volume and the API connects through the existing MySQL/Pomelo configuration.
- Redis remains cache/infrastructure and never becomes business truth.
- Compose starts a MySQL database container but does not create VocaNova tables; the database-first schema still has to be provisioned from an existing compatible schema outside EF migrations.
- Gemini, Cloudinary, Google, and SpeedSMS remain external HTTPS providers, not containers.

Docker deployment shape:

```text
Flutter Mobile (outside Docker)
          |
          v exposed host API port
+----------------------------------------+
| Docker Compose                         |
| dashboard -> api -> mysql              |
|                  `-> redis             |
+----------------------------------------+
```

See `DATABASE.md`, `COMMUNICATION.md`, and `DEVELOPMENT.md` for persistence and runtime details.

## Cross-cutting behavior (CURRENT)

- `ExceptionMiddleware` logs unhandled exceptions and returns the shared 500 envelope; Development includes exception details.
- `AuditLogMiddleware` audits admin/SuperAdmin write requests, redacts password/token/secret fields, and queues records to an in-process background service.
- JWT Bearer authentication defines Admin and SuperAdmin policies.
- FluentValidation auto-validates controller inputs.
- Redis cache implementations degrade to uncached behavior when unavailable.
- KNN rebuilding supports manual and hosted scheduled execution. The rebuild service is singleton and creates scopes for scoped learning work; the hosted interval is captured from startup options, while only vector weights use the runtime-settings `.env`/Redis path.

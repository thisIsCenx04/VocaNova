# Project Structure

Generated outputs (`bin`, `obj`, `.dart_tool`, Flutter `build`), vendored browser libraries, generated Dart/localization files, images, and platform boilerplate are omitted.

## Repository tree (CURRENT)

This tree was verified from the checkout. EF migrations, `PROJECT_STRUCTURE_3_LAYER.md`, and `VocaNova_Activity_History.md` are not present.

```text
VocaNova/
|-- .env.example
|-- .dockerignore
|-- .gitignore
|-- AGENTS.md
|-- README.md
|-- VocaNova.sln
|-- docker-compose.yml
|-- docs/
|   |-- ARCHITECTURE.md
|   |-- COMMUNICATION.md
|   |-- CONVENTIONS.md
|   |-- DATABASE.md
|   |-- DECISIONS.md
|   |-- DEVELOPMENT.md
|   |-- PROJECT_STRUCTURE.md
|   |-- REFACTOR_PLAN.md
|   |-- SERVICE_CATALOG.md
|   `-- WORKFLOW.md
|-- scripts/
|   |-- add-word-sense-status.sql
|   |-- scaffold-mysql.ps1
|   |-- seed-activity-trend.sql
|   |-- seed-topic-overlaps.sql
|   `-- seed-user-learning-activity.sql
|-- src/
|   |-- VocaNova.API/
|   |   |-- Dockerfile
|   |   |-- Common/{Abstractions,Constants,Extensions,Models,Responses,Results,Routing,Security,Validation}/
|   |   |-- Features/
|   |   |   |-- Admin/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- AiGrading/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Auth/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Dictionary/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Knn/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Lists/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Notifications/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Progress/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   |-- Quiz/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |   `-- SuperAdmin/{Controllers,Contracts,Mappings,BLL,DAL}/
|   |   |-- Infrastructure/
|   |   |   |-- Auditing/
|   |   |   |-- Authentication/
|   |   |   |-- Caching/          # shared Redis implementations, including migrated feature caches
|   |   |   |-- Configuration/
|   |   |   |-- ExternalServices/Gemini/
|   |   |   |-- HostedServices/
|   |   |   |-- Otp/
|   |   |   |-- Persistence/{Configurations,Entities}/
|   |   |   |-- RateLimiting/
|   |   |   |-- Sms/
|   |   |   `-- Storage/
|   |   |-- DependencyInjection/
|   |   |-- Middleware/
|   |   |-- Program.cs
|   |   `-- VocaNova.API.csproj
|   |-- VocaNova.Dashboard/
|   |   |-- Dockerfile
|   |   |-- Controllers/
|   |   |-- Models/{AdminAccounts,Api,Auth,Dashboard,Knn,Profile,Roles,Statistics,Topics,Users,Vocabulary}/
|   |   |-- Routing/
|   |   |-- Services/{Api,Auth,Localization}/
|   |   |-- Views/
|   |   |-- wwwroot/
|   |   |-- src/VocaNova.Dashboard/   # anomalous nested duplicate assets/view tree
|   |   |-- Program.cs
|   |   `-- VocaNova.Dashboard.csproj
|   `-- VocaNova.Mobile/
|       |-- lib/
|       |   |-- app/{router,settings,theme}/
|       |   |-- core/{connectivity,network,storage,widgets}/
|       |   |-- features/{auth,dictionary,home,lists,notifications,progress,quiz,settings,shared}/
|       |   |   `-- application, data, domain, presentation as used by each feature
|       |   |-- l10n/
|       |   `-- main.dart
|       |-- test/
|       |-- android/ ios/ web/
|       `-- pubspec.yaml
`-- tests/
    `-- VocaNova.Tests/
        |-- Admin/ AiGrading/ Architecture/ Auth/ Dashboard/ Dictionary/
        |-- Knn/ Lists/ Notifications/ Progress/ Quiz/ Shared/ SuperAdmin/ Support/
        `-- VocaNova.Tests.csproj
```

### Current ownership

| Path | Verified responsibility |
|---|---|
| `API/Features` | Corrected feature-first Presentation/BLL/DAL slices for Notifications, Progress, all Dictionary and Lists/personal-topic behavior, Quiz, AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin. |
| `API/Infrastructure` | Shared EF/MySQL persistence, Redis implementations, authentication, auditing, runtime configuration, rate limiting, OTP, SMS, and Cloudinary storage. Core persistence and providers are deliberately not fragmented across features. |
| `API/DependencyInjection` | API composition extensions. `AddBLL()` groups feature BLL service registrations and `AddDAL(configuration)` groups feature DAL/shared infrastructure registrations while `Program.cs` keeps HTTP/middleware ordering. |
| `API/Common` | Shared envelopes/results, helpers, routing, security, and validation; currently includes some EF/ASP.NET-coupled code. |
| `Dashboard/Services/Api` | HTTP transport, Bearer injection, refresh/retry, endpoint operations, and envelope parsing. |
| `Dashboard/Models` | Dashboard-owned wire models and Razor view/support models. |
| `Mobile/lib/features/*/data` | Dio-backed API gateways currently named repositories and JSON/domain mapping. |
| `Mobile/lib/features/*/application` | Riverpod state, providers, notifiers, caching, and UI workflows. |
| `tests/VocaNova.Tests` | API/Dashboard xUnit tests using mocks, HTTP handlers, EF InMemory, and a rolled-back MySQL integration test for WordSense soft delete. |
| `Mobile/test` | Flutter unit, networking, storage, provider, repository, router, and widget tests. |
| `docker-compose.yml` | Four-service Docker foundation: MySQL, non-persistent Redis, API, and Dashboard with health-based dependencies. |
| API/Dashboard `Dockerfile` | .NET 8 multi-stage restore/publish/runtime images exposing container HTTP port 8080. |

## Repository tree (TARGET)

ADR-018 accepts corrected feature-first slices. The tree below is the canonical physical layout; a feature creates only the files it needs, but it does not collapse HTTP Contracts, BLL models, and persistence entities into one type.

```text
VocaNova/
|-- src/
|   |-- VocaNova.API/
|   |   |-- Features/
|   |   |   |-- Admin/
|   |   |   |-- AiGrading/
|   |   |   |-- Auth/
|   |   |   |-- Dictionary/
|   |   |   |-- Knn/
|   |   |   |-- Lists/
|   |   |   |-- Notifications/
|   |   |   |-- Progress/
|   |   |   |-- Quiz/
|   |   |   `-- SuperAdmin/
|   |   |       # every feature uses the same boundary shape as needed:
|   |   |       |-- Controllers/
|   |   |       |-- Contracts/{Requests,Responses}/
|   |   |       |-- Mappings/
|   |   |       |-- BLL/{Abstractions,Models,Services}/
|   |   |       `-- DAL/{Repositories,Mappings}/
|   |   |-- Infrastructure/
|   |   |   |-- Persistence/{Configurations,Entities,Transactions}/
|   |   |   |-- Caching/
|   |   |   |-- Authentication/
|   |   |   |-- Auditing/
|   |   |   |-- Configuration/
|   |   |   |-- Otp/
|   |   |   |-- RateLimiting/
|   |   |   |-- Sms/
|   |   |   `-- Storage/
|   |   |-- Common/
|   |   |-- DependencyInjection/
|   |   |-- Middleware/
|   |   |-- Filters/
|   |   `-- Program.cs
|   |-- VocaNova.Dashboard/      # REST-only Presentation client
|   `-- VocaNova.Mobile/         # REST-only Presentation client; outside Docker
|-- tests/VocaNova.Tests/
|-- scripts/
`-- docs/
```

The literal feature boundary is:

```text
Features/<Feature>/
|-- Controllers/
|-- Contracts/
|   |-- Requests/
|   `-- Responses/
|-- Mappings/                    # Contract <-> BLL model
|-- BLL/
|   |-- Abstractions/            # repository/cache/provider ports owned by BLL
|   |-- Models/                  # commands, queries, results/errors, business models
|   `-- Services/
`-- DAL/
    |-- Repositories/            # implementations only
    `-- Mappings/                # BLL model <-> persistence representation
```

Request validators are Presentation concerns and may live beside the feature's Request Contracts. No `DTOs` folder or new `Dto` suffix is part of the target. Mapping is explicit/manual by default; AutoMapper is not mandated.

Shared/global infrastructure remains consolidated. `VocaNovaDbContext`, scaffolded entities, EF configurations, database-first synchronization, transaction coordination, Redis implementations, authentication, storage, providers, auditing, and runtime configuration stay outside `Features`. Cross-feature framework-neutral primitives such as `Common/Models/PagedCollection.cs` stay under `Common` rather than being duplicated.

### Migrated feature files (CURRENT)

```text
Features/Notifications/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Progress/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Dictionary/{Controllers,Contracts,Mappings,BLL,DAL}/  # public reads and administration
Features/Lists/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Quiz/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/AiGrading/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Knn/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Auth/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/Admin/{Controllers,Contracts,Mappings,BLL,DAL}/
Features/SuperAdmin/{Controllers,Contracts,Mappings,BLL,DAL}/
Infrastructure/Caching/{Auth,Dictionary,Knn,Lists,Progress,Quiz}/ # shared cache implementations
DependencyInjection/VocaNovaServiceCollectionExtensions.cs        # AddBLL/AddDAL composition
Common/Models/PagedCollection.cs
```

## Important migration gaps

- The ordered feature-first refactor units are implemented in source. Admin/SuperAdmin cleanup removed the remaining Admin BLL EF-entity leak and corrected Admin repository namespaces to match their physical BLL/DAL folders.
- Test-only compatibility cleanup is partially complete. Progress, KNN, Quiz, and SuperAdmin compatibility adapters have been retired; remaining test-local compatibility surfaces are `tests/VocaNova.Tests/Auth/CompatAuthRepository.cs`, `tests/VocaNova.Tests/Dictionary/DictionaryCompatibilityAdapters.cs`, `tests/VocaNova.Tests/Lists/ListsCompatibilityAdapters.cs`, and broad aliases in `tests/VocaNova.Tests/GlobalUsings.cs`.
- Shared entities/configurations, `VocaNovaDbContext`, Redis, and provider implementations remain consolidated under `Infrastructure`.
- Feature BLL/DAL registrations are grouped behind `AddBLL()` and `AddDAL(configuration)`; `Program.cs` still owns HTTP framework setup and middleware order.
- Dashboard/Mobile remain REST clients; their client-side layers are not backend BLL/DAL.
- Dockerfiles and Compose health checks/container DNS are implemented for `mysql`, `redis`, `api`, and `dashboard`; Compose uses the named `mysql_data` volume and wires the API's existing MySQL configuration to `mysql:3306`.

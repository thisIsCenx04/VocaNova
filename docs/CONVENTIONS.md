# Conventions

CURRENT names are documented accurately even when they differ from TARGET conventions. Do not mechanically rename source because documentation prefers a target name.

## Status and terminology

- **CURRENT**: verified in source now.
- **TARGET**: accepted future state, not yet implemented.
- **DECISION**: accepted in `DECISIONS.md`.
- **RECOMMENDATION**: optional and not accepted.
- Use “Contracts” only for public HTTP request/response shapes.
- Do not use “DTO” as an ambiguous synonym for Contract, BLL model, entity, cache payload, or provider model.

## Naming

| Boundary | CURRENT | TARGET convention |
|---|---|---|
| API HTTP | `*Request`, `*Response`, many `*Dto`, query types | `*Request` and `*Response` under `Features/<Feature>/Contracts`; no new generic `Dto` types. |
| Controllers | `*Controller` in mixed or migrated feature folders | `*Controller` under `Features/<Feature>/Controllers`; HTTP Presentation only. |
| BLL | `I*Service`/`*Service`, models mixed with DTOs/entities | Business models/results, `I*Service`, and `*Service` under `Features/<Feature>/BLL`; framework neutral. |
| BLL persistence ports | Production feature repository interfaces are BLL-owned; some test-only compatibility helpers still exist. | BLL-owned `I*Repository` under `Features/<Feature>/BLL/Abstractions`, using BLL models/commands. |
| DAL | `*Repository`, scaffolded POCO entity names, configurations | Feature implementations under `Features/<Feature>/DAL`; shared entities, EF configurations, Redis, and providers under consolidated `Infrastructure`. |
| Dashboard | `Data/Dtos`, mostly `*ViewModel`, `VocaNovaApiClient` | `*ViewModel`; Dashboard-owned Requests/Responses; `ApiClient` and UI workflow services. |
| Mobile | `*State`, Notifier/Provider, `data/services/*ApiService`, feature-owned `data/dtos/*Dto` wire models, `domain/models` domain models | Riverpod State/Notifier/Provider; client service/API gateway for remote HTTP; explicit local-store names for device persistence. |

## Ownership and dependencies (TARGET)

- Presentation maps HTTP Contracts to/from BLL models and depends on BLL service abstractions.
- BLL owns use cases, business rules/models/results, service abstractions, and required repository/cache/auth/storage/provider abstractions.
- DAL owns EF Core/MySQL/Pomelo, Redis, persistence and external-provider implementations, and BLL/entity/provider mapping.
- Controllers never depend on repositories, DbContext, Redis, database providers, or external implementations.
- BLL never depends on DAL, EF Core, Pomelo, MySQL-specific APIs, ASP.NET HTTP types, Redis implementations, or external SDK implementations.
- `Program.cs` is the composition root and may see Presentation, BLL, and DAL registrations.
- Dashboard and Mobile remain Presentation clients. Do not create backend BLL/DAL layers for them.
- `Common` contains only proven reusable, layer-safe code. EF-specific helpers belong to DAL; HTTP-specific helpers belong to Presentation.

## Contracts and mappings

- HTTP Contract != BLL model != DAL entity.
- Never serialize an EF entity directly as an API response.
- Map Request Contract -> BLL command/model at Presentation.
- Map BLL model <-> persistence/provider model at DAL.
- Map BLL result -> Response Contract at Presentation.
- Prefer explicit/manual mapping. Do not mandate AutoMapper without a later decision.
- Public routes, methods, status behavior, response envelopes, and JSON property names are compatibility surfaces.

CURRENT production feature slices separate HTTP Contracts, BLL models/results, and DAL entity/provider/cache representations across Notifications, Progress, Dictionary, Lists/personal topics, Quiz/AI grading, KNN/runtime configuration, Auth, Admin, and SuperAdmin. Remaining `Dto` names or compatibility helpers are legacy/test-local surfaces only; do not introduce new ambiguous DTOs.

CURRENT Dashboard API payloads use Dashboard-owned DTOs under `src/VocaNova.Dashboard/Data/Dtos`. Razor/UI models remain under `src/VocaNova.Dashboard/Models`, and `Services/Api` owns HTTP transport/envelope parsing.

CURRENT Mobile REST payloads use explicit feature-owned DTOs under `src/VocaNova.Mobile/lib/features/<feature>/data/dtos`. Remote HTTP calls are named `*ApiService` under `data/services`, cache workflows serialize through DTOs, and application/presentation layers consume domain models from `domain/models` rather than DTOs.

## Async, validation, and errors

- I/O methods use `async`/`await`, end with `Async`, accept cancellation where practical, and pass cancellation through layers.
- Use asynchronous EF and HTTP APIs. Avoid `async void` outside framework event signatures.
- Feature controllers use FluentValidation and a shared `Result<T>`/response envelope where applicable. Feature BLL results remain framework-neutral and map to HTTP status/envelope behavior in Presentation; future BLL results must remain independent of ASP.NET status constants.
- Dashboard uses DataAnnotations/ModelState and maps API failures to UI state.
- Mobile maps Dio failures to `AppException`; backend validation remains authoritative.
- Cache failure degrades cache behavior and must not redefine business truth.

## Logging, security, and generated files

- Use injected `ILogger<T>` and structured placeholders.
- Never log or commit secrets, tokens, passwords, OTPs in exposed environments, or local `.env` values.
- `.env.example` contains placeholders only. Dockerfiles and Compose files must never hard-code secrets.
- Admin/SuperAdmin write auditing redacts fields containing password/token/secret.
- Do not hand-edit Riverpod `*.g.dart` or generated files under `lib/l10n/gen`.
- C# nullable and implicit usings are enabled; Dart uses `flutter_lints` and `dart format`.

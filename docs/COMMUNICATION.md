# Communication and Integration

This document owns runtime boundaries. Database internals are in `DATABASE.md`; endpoint inventory is in `SERVICE_CATALOG.md`.

## Client -> API boundary (CURRENT)

Dashboard and Mobile call the single `VocaNova.API` process using JSON over HTTP(S). Most API responses use:

```json
{
  "success": true,
  "data": {},
  "message": "Success.",
  "errors": [],
  "pagination": null
}
```

Many feature DTO properties explicitly use snake_case; shared envelope/pagination properties follow ASP.NET web JSON defaults. Multipart form data is used for avatar, word image/audio, and CSV uploads. Neither client uses a generated OpenAPI client, so public routes, methods, authorization, envelopes, and JSON property names are manual compatibility surfaces.

## Dashboard -> API (CURRENT)

```text
Browser -> MVC Controller -> Dashboard workflow/API client -> HttpClient -> VocaNova.API
```

- `VocaNovaApi:BaseUrl` selects the API endpoint (`http://localhost:5013` in Dashboard Development configuration).
- `DashboardAuthService` handles login/logout/profile admission. Admin and SuperAdmin roles are admitted.
- Cookie authentication stores API access/refresh tokens in authentication properties.
- `BearerTokenHandler` attaches the access token, performs one refresh on 401 through a separate client, updates the cookie, clones the request, and retries once.
- Most MVC controllers call `IVocaNovaApiClient`; they translate normalized API results into views, ModelState, TempData, redirects, or status responses.
- Dashboard directly consumes Dictionary administration, KNN/runtime settings, AI-grading settings, Admin users/statistics, and SuperAdmin account/role contracts through feature controllers and manually maintained API models.
- Dashboard has no reference to the API project and no DbContext/MySQL/Redis access.

## Mobile -> API (CURRENT)

```text
Screen -> Riverpod Provider/Notifier -> feature data *Repository -> Dio -> VocaNova.API
```

- Android emulator default base URL is `http://10.0.2.2:5013`; `API_BASE_URL` can override it with `--dart-define`.
- `AuthInterceptor` reads secure tokens, attaches Bearer auth, serializes refresh attempts, rotates stored tokens, and retries once.
- `ErrorInterceptor` maps Dio/backend failures to `AppException`.
- Feature `data/*_repository.dart` classes are REST gateways despite their current names; JSON is generally mapped into client domain models.
- Mobile directly consumes the audited Auth, Lists/personal-topic, Quiz, and KNN recommendation/onboarding contracts. Their route strings and manually mapped request/response fields are part of each refactor slice's compatibility check.
- `SharedPreferences` stores UI settings and TTL-based client caches. `flutter_secure_storage` stores access/refresh tokens.
- Mobile never connects to MySQL or Redis and never references backend source.

## API -> infrastructure (CURRENT)

| Boundary | Transport | Verified role |
|---|---|---|
| API -> MySQL | EF Core 8 + Pomelo | Primary relational system of record. |
| API -> Redis | StackExchange.Redis | Cache and runtime-settings fallback; failures degrade to uncached behavior. |
| API -> Gemini | HTTPS `HttpClient` | Typing-answer grading with retry/model fallback and exact-match fallback. |
| API -> Cloudinary | Cloudinary .NET SDK over HTTPS | Word images/audio and avatars. |
| API -> SpeedSMS | HTTPS `HttpClient` | Optional OTP delivery; disabled configuration uses a console provider. |
| API -> Google | Google authentication library | Mobile ID-token validation. |

The backend is one process. Feature calls are in-process C# calls; there is no internal REST, gRPC, event bus, or distributed message broker. The audit `Channel<T>` is process-local.

## Boundaries (TARGET)

Dashboard and Mobile remain Presentation clients:

```text
Browser -> MVC Controller -> Dashboard workflow/service -> API Client -> REST -> API
Screen  -> Riverpod Provider/Notifier -> client service/API gateway -> Dio -> REST -> API
```

They own language-specific wire models and must not access MySQL/Redis or reference backend source. Client-side application/domain/data concepts do not create backend BLL/DAL layers.

Within the API:

```text
HTTP Presentation -> BLL service abstraction
BLL use case       -> BLL-owned persistence/cache/provider abstraction
DAL implementation -> MySQL / Redis / external HTTPS provider
```

The MySQL/Pomelo boundary remains the accepted TARGET relational boundary. Redis remains infrastructure and not business truth.

## Docker networking (CURRENT/TARGET)

```text
Flutter Mobile (outside Docker)
          |
          v exposed host API port
+------------------------------------------------+
| Docker Compose                                 |
| Dashboard ---------------------> api           |
|                                    |           |
|                              +-----+-----+     |
|                              v           v     |
|                         mysql          redis |
+------------------------------------------------+
```

- Compose services are exactly `mysql`, `redis`, `api`, and `dashboard`.
- Container-to-container configuration uses `mysql:3306`, `redis:6379`, and `api:8080`, never `localhost`.
- Host-based development outside Docker may use `localhost`.
- Gemini, Cloudinary, Google, and SpeedSMS remain external HTTPS services, not containers.
- Flutter runs on an emulator, simulator, physical device, or normal Flutter development host and calls the exposed API endpoint.
- Dashboard is configured with `VocaNovaApi__BaseUrl=http://api:8080` and has no MySQL/Redis configuration.
- CURRENT Compose wires the API container with `MYSQL_CONNECTION_STRING=Server=mysql;Port=3306;...` and keeps Dashboard configured with `VocaNovaApi__BaseUrl=http://api:8080`. It does not create VocaNova tables; database-first schema provisioning remains a separate operational step.

## Contract synchronization

Until an independently accepted OpenAPI-generation decision is implemented, changes require coordinated verification of API Contracts/tests, Dashboard wire models/client calls, and Mobile parsing/repository tests. Architecture refactoring must preserve existing routes and JSON schemas unless a task explicitly authorizes an API change.

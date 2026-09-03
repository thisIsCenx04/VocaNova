# VocaNova

VocaNova is a vocabulary-learning and testing system with one ASP.NET Core backend, an MVC administration dashboard, and a Flutter mobile client.

| Application | Role |
|---|---|
| `src/VocaNova.API` | ASP.NET Core 8 Web API and backend modular monolith. |
| `src/VocaNova.Dashboard` | ASP.NET Core 8 MVC Presentation client that calls the API over HTTP(S). |
| `src/VocaNova.Mobile` | Flutter Presentation client using Riverpod and Dio. |
| `tests/VocaNova.Tests` | xUnit tests for API and Dashboard; Flutter tests are in the Mobile package. |

## Architecture status

**CURRENT:** The API is in an incremental architecture transition: Notifications, Progress, all Dictionary and Lists/personal-topic endpoints, Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin use corrected feature-first Presentation/BLL/DAL slices under `Features/<Feature>`, with feature BLL/DAL registrations grouped through `AddBLL()` and `AddDAL(configuration)`. EF Core 8 uses MySQL/Pomelo and database-first scaffolding; Dictionary senses now have active/deleted soft-delete state. Dashboard and Mobile maintain their own API wire models. Some Auth/Dictionary/Lists test compatibility helpers remain local to the test project. The Docker foundation defines MySQL, Redis, API, and Dashboard containers aligned with the accepted relational target.

**TARGET:** VocaNova remains a modular monolith and adopts a system-level three-layer architecture. Each API `Features/<Feature>` folder owns its Presentation Contracts/controllers/mappings, framework-neutral BLL models/services/ports, and feature-specific DAL repositories/mappings. Repository interfaces belong to BLL; shared EF/Redis/authentication/storage/provider infrastructure stays consolidated outside feature folders. MySQL 8, Pomelo, and database-first synchronization through `scripts/scaffold-mysql.ps1` remain the long-term relational stack and workflow. Docker Compose uses exactly `mysql`, `redis`, `api`, and `dashboard`; Flutter remains outside Docker.

## Current technology stack

- .NET 8, ASP.NET Core Web API/MVC, EF Core 8, Pomelo MySQL provider
- MySQL 8, Redis/StackExchange.Redis
- JWT and Google authentication, Cloudinary, Gemini, SpeedSMS
- Flutter/Dart, Riverpod, Dio, go_router, secure storage, shared preferences
- xUnit, Moq, FluentAssertions, EF Core InMemory, Flutter test

## Current quick start

Prerequisites are .NET SDK 8, MySQL 8 with an existing compatible `vocanova` schema, Redis, and Flutter for Mobile.

```powershell
Copy-Item .env.example .env
dotnet restore VocaNova.sln
dotnet build VocaNova.sln
dotnet test VocaNova.sln
```

Then run the API and Dashboard in separate terminals:

```powershell
dotnet run --project src/VocaNova.API
dotnet run --project src/VocaNova.Dashboard
```

Configure the placeholder values in the uncommitted `.env`; see `docs/DEVELOPMENT.md`. Run Mobile separately:

```powershell
Set-Location src/VocaNova.Mobile
flutter pub get
flutter run
```

The current schema synchronization command is `scripts/scaffold-mysql.ps1`. It is database-first and destructive to scaffolded source; use it only for an explicitly reviewed current-schema task.

The CURRENT Compose file provisions MySQL, Redis, API, and Dashboard. The API container is wired to `mysql:3306` through `MYSQL_CONNECTION_STRING`, and Dashboard-to-API container routing remains `http://api:8080`. Because the project is database-first and has no migrations, the MySQL container creates the database name only; load an existing compatible schema before using database-backed endpoints.

## Documentation

- `AGENTS.md`: authoritative contributor/agent rules
- `docs/ARCHITECTURE.md`: current and target architecture
- `docs/PROJECT_STRUCTURE.md`: verified current and accepted target trees
- `docs/SERVICE_CATALOG.md`: current routes and feature ownership
- `docs/DATABASE.md`: current and target MySQL/Pomelo database-first persistence
- `docs/COMMUNICATION.md`: client, database, cache, provider, and Docker boundaries
- `docs/DECISIONS.md`: accepted and superseded architecture decisions
- `docs/REFACTOR_PLAN.md`: incremental migration plan
- `docs/DEVELOPMENT.md`: host setup, database scaffolding, and current/target Docker status
- `docs/CONVENTIONS.md` and `docs/WORKFLOW.md`: naming and delivery rules

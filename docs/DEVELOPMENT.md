# Development

## Development setup (CURRENT)

### Prerequisites

- .NET SDK 8 or newer; all .NET projects target `net8.0`.
- MySQL 8.x with an existing compatible `vocanova` schema.
- Redis for cache/runtime settings; many API paths degrade to uncached operation if unavailable.
- Flutter compatible with Dart `^3.10.7` for Mobile.
- `dotnet ef` only for explicitly authorized current database scaffolding.
- Credentials only for exercised flows: JWT, Google, Gemini, Cloudinary, Pexels media suggestions, and optionally SpeedSMS.

### Configuration

```powershell
Copy-Item .env.example .env
```

Keep `.env` local and uncommitted. `.env.example` contains placeholders. Current API startup requires `MYSQL_CONNECTION_STRING`, `MYSQL_SERVER_VERSION`, and valid JWT settings. Redis and provider settings use ASP.NET double-underscore environment keys. Configure `Pexels__ApiKey` before using admin image/video suggestions. Dashboard reads `VocaNovaApi:BaseUrl` from its appsettings.

### Restore, build, and test

```powershell
dotnet restore VocaNova.sln
dotnet build VocaNova.sln
dotnet test VocaNova.sln

Set-Location src/VocaNova.Mobile
flutter pub get
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test --concurrency=1
```

Flutter is not a solution project and must be verified separately.

Audio regression checks: the .NET Dictionary tests cover UK/US replacement, cleanup/reference guards, and Dashboard multipart contracts. Mobile's `test/features/dictionary/application/audio_playback_service_test.dart` covers recording/TTS fallback and rapid taps. Open `tests/VocaNova.Tests/Dashboard/word-audio-browser-test.html` in a Chromium browser to exercise the real edit JavaScript against a labeled local fixture; its output must read `PASS`. The fixture mocks upload responses and does not replace authenticated Dashboard or device/Cloudinary testing.

### Run

```powershell
dotnet run --project src/VocaNova.API
dotnet run --project src/VocaNova.Dashboard
```

API launch profiles expose HTTP `http://localhost:5013` and HTTPS `https://localhost:7069`; health is `/health`, and Swagger is enabled in Development. Dashboard profiles expose HTTP `http://localhost:5236` and HTTPS `https://localhost:7060`; Dashboard Development configuration calls API port 5013.

Run Mobile separately:

```powershell
Set-Location src/VocaNova.Mobile
flutter run
```

Android emulator default API URL is `http://10.0.2.2:5013`. Override it for a LAN device or another environment:

```powershell
flutter run --dart-define=API_BASE_URL=http://192.168.1.10:5013
```

Google login also uses `GOOGLE_SERVER_CLIENT_ID`. iOS provider files/settings remain local and must not be committed.

### Current database workflow

The repository has no schema-creation migrations. Start MySQL, provide an existing compatible schema, and configure `.env`.

```powershell
.\scripts\scaffold-mysql.ps1
```

This is a database-first Pomelo scaffold using `--force`; it overwrites context/entities. Use it only for explicitly authorized schema synchronization and review all generated diffs. The SQL files under `scripts/` are optional MySQL data scripts, not migrations.

## Docker development status

The repository includes `docker-compose.yml`, `.dockerignore`, and multi-stage .NET 8 Dockerfiles for API and Dashboard. CURRENT Compose provisions `mysql`, `redis`, `api`, and `dashboard`, with the API container wired to `mysql:3306` through `MYSQL_CONNECTION_STRING` and `MYSQL_SERVER_VERSION`.

The MySQL image creates the configured database name, but the project has no EF migrations or automatic schema creation. Import or otherwise provide an existing compatible VocaNova schema in the MySQL container before using database-backed endpoints. Host-based MySQL remains supported for normal `dotnet run`.

### Docker configuration (CURRENT/TARGET)

Compose services are exactly `mysql`, `redis`, `api`, and `dashboard`. MySQL uses the named `mysql_data` volume, API uses `mysql:3306` through its existing environment keys, Redis uses `redis:6379`, and Dashboard uses `api:8080`. Flutter remains outside Docker, and external providers remain HTTPS dependencies.

Container MySQL configuration:

```dotenv
MYSQL_DATABASE=vocanova
MYSQL_ROOT_PASSWORD=<secret>
MYSQL_CONNECTION_STRING=Server=mysql;Port=3306;Database=vocanova;User=root;Password=<secret>;
MYSQL_SERVER_VERSION=8.0.0-mysql
```

Do not commit `.env`, real secrets, or hard-coded secrets in Dockerfiles/Compose. Normal commands remain `docker compose up --build`, `docker compose ps`, `docker compose logs`, and `docker compose down`.

Connectivity by caller:

- Docker Dashboard -> API: `http://api:8080`.
- Host browser -> Dashboard: `http://localhost:5236` by default.
- Host browser/tools -> API: `http://localhost:5013` by default.
- Android emulator -> API: `http://10.0.2.2:5013` by default.
- Physical device -> API: the development host's reachable address plus exposed API port; no machine-specific LAN address is committed.

The API and Dashboard containers listen on internal port 8080. Compose waits for MySQL and Redis health before starting API, then waits for API health before starting Dashboard.

## Troubleshooting (CURRENT)

- Missing MySQL configuration: ensure root `.env` contains `MYSQL_CONNECTION_STRING` and `MYSQL_SERVER_VERSION`.
- Redis warnings: start Redis or correct `Redis__Configuration`; cache bypass is expected while unavailable.
- Dashboard cannot reach API: align `VocaNovaApi:BaseUrl` with the active API launch profile/scheme.
- Android emulator cannot reach host `localhost`: use `10.0.2.2`; physical devices require a reachable LAN address.
- Google login errors: use the same web client ID in Mobile and API configuration.
- Provider failures: configure only the Gemini/Cloudinary/Pexels/SpeedSMS flow being exercised.
- Scaffold failure: verify `dotnet ef`, MySQL, `.env`, and the pre-existing schema.

## Word video setup and checks

Apply `scripts/add-word-video-assets.sql` to the target MySQL database before running this version. Existing `Cloudinary__CloudName`, `Cloudinary__ApiKey`, and `Cloudinary__ApiSecret` settings are reused. Video uploads use `vocanova/words/video/{wordId}/{uniqueId}`; no additional provider secret is needed. Run `flutter pub get` and `flutter gen-l10n` after pulling the Mobile change.

The .NET video lifecycle tests cover file/metadata limits, failed processing, ambiguous commits, cleanup, cache failures, and soft deletion. `WordVideoIntegrationTests` exercises the actual MySQL table inside a rolled-back transaction. Mobile `test/features/dictionary/word_video_test.dart` checks DTO cache round-trip, lazy initialization, audio stop ordering, background pause, disposal and retry using a fake native player.

Open `tests/VocaNova.Tests/Dashboard/word-video-browser-test.html` in Chromium: output must be `PASS`. This uses real Dashboard JS/CSS with simulated metadata/API responses; it does not replace authenticated end-to-end or device checks. A local Cloudinary smoke check successfully uploaded a six-second public demo fixture, produced MP4/JPG variants, fetched both with HTTP 200, and deleted the test asset. No provider credentials or fixture media are committed.

Auto-suggestion regression: `tests/VocaNova.Tests/Dashboard/media-suggestions-browser-test.html` exercises the production suggestion, preview, download and upload scripts with simulated provider responses. It checks automatic image/video searches, inline video players, no upload on selection, multipart save, old video preservation on failure, image persistence using the saved URL, allowed download hosts and the size cap. Chromium output must read `PASS`. Public Pexels image/video responses were also checked for browser CORS support.

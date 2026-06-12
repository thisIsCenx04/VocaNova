# VocaNova

VocaNova is a vocabulary learning and testing system for SEP490. The repository contains:

- `src/VocaNova.API`: ASP.NET Core Web API.
- `src/VocaNova.Dashboard`: ASP.NET Core MVC dashboard.
- `src/VocaNova.Mobile`: Flutter mobile application.
- `tests/VocaNova.Tests`: xUnit test project.

## Prerequisites

- .NET SDK 8.0 or newer.
- PostgreSQL for the main database.
- Redis for cache and rate-limit related features.
- Flutter 3.38 or newer for the mobile application.

## Local Setup

1. Restore packages:

   ```bash
   dotnet restore
   ```

2. Create a local `.env` file from the example:

   ```bash
   cp .env.example .env
   ```

3. Update local configuration:

   In `.env`:

   - `MYSQL_CONNECTION_STRING`
   - `MYSQL_SERVER_VERSION`

   In `src/VocaNova.API/appsettings.json` or user secrets:

   - `JwtSettings:SecretKey`
   - `Redis:Configuration`
   - `AiGrading:*`

4. Build the solution:

   ```bash
   dotnet build
   ```

5. Run the API:

   ```bash
   dotnet run --project src/VocaNova.API
   ```

6. Run the dashboard:

   ```bash
   dotnet run --project src/VocaNova.Dashboard
   ```

7. Run tests:

   ```bash
   dotnet test
   ```

8. Run the mobile application:

   ```powershell
   cd src/VocaNova.Mobile
   flutter pub get
   flutter run
   ```

## Database Scaffold

The database is MySQL/MariaDB from XAMPP. Connection details are read from the repository root `.env` file, which is ignored by Git.

Run the scaffold script after the XAMPP MySQL service is running and the `vocanova` schema exists:

```powershell
.\scripts\scaffold-mysql.ps1
```

## Git Workflow

- Feature branches follow `feature/{module}/{feature-name}`.
- Commits follow Conventional Commits, for example `feat(setup): add solution structure`.
- Pull requests target `dev`.

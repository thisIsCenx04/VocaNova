# VocaNova

VocaNova is a vocabulary learning and testing system for SEP490. The repository is organized for three .NET projects in the first backend/dashboard milestone:

- `src/VocaNova.API`: ASP.NET Core Web API.
- `src/VocaNova.Dashboard`: ASP.NET Core MVC dashboard.
- `tests/VocaNova.Tests`: xUnit test project.

## Prerequisites

- .NET SDK 8.0 or newer.
- PostgreSQL for the main database.
- Redis for cache and rate-limit related features.

## Local Setup

1. Restore packages:

   ```bash
   dotnet restore
   ```

2. Update local configuration in `src/VocaNova.API/appsettings.json` or user secrets:

   - `ConnectionStrings:DefaultConnection`
   - `JwtSettings:SecretKey`
   - `Redis:Configuration`
   - `AiGrading:*`

3. Build the solution:

   ```bash
   dotnet build
   ```

4. Run the API:

   ```bash
   dotnet run --project src/VocaNova.API
   ```

5. Run the dashboard:

   ```bash
   dotnet run --project src/VocaNova.Dashboard
   ```

6. Run tests:

   ```bash
   dotnet test
   ```

## Git Workflow

- Feature branches follow `feature/{module}/{feature-name}`.
- Commits follow Conventional Commits, for example `feat(setup): add solution structure`.
- Pull requests target `dev`.

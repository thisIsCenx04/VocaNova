param(
    [string] $Project = "src/VocaNova.API/VocaNova.API.csproj",
    [string] $Context = "VocaNovaDbContext",
    [string] $ContextDir = "Infrastructure/Persistence",
    [string] $OutputDir = "Infrastructure/Persistence/Entities",
    [string] $Provider = "Pomelo.EntityFrameworkCore.MySql"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$envPath = Join-Path $repoRoot ".env"

if (-not (Test-Path $envPath)) {
    throw "Missing .env at repository root. Copy .env.example to .env and update MYSQL_CONNECTION_STRING."
}

Get-Content $envPath | ForEach-Object {
    $line = $_.Trim()

    if ($line.Length -eq 0 -or $line.StartsWith("#")) {
        return
    }

    $parts = $line.Split("=", 2)

    if ($parts.Length -eq 2) {
        [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
    }
}

if ([string]::IsNullOrWhiteSpace($env:MYSQL_CONNECTION_STRING)) {
    throw "Missing MYSQL_CONNECTION_STRING in .env."
}

dotnet ef dbcontext scaffold `
    $env:MYSQL_CONNECTION_STRING `
    $Provider `
    --project $Project `
    --startup-project $Project `
    --context $Context `
    --context-dir $ContextDir `
    --output-dir $OutputDir `
    --use-database-names `
    --no-onconfiguring `
    --force

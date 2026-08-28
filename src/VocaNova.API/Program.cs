using FluentValidation;
using FluentValidation.AspNetCore;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Routing;
using VocaNova.API.DependencyInjection;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Middleware;

// Publishes .env as process environment variables — DatabaseConnection reads its connection
// string straight from the environment rather than from IConfiguration.
EnvironmentFile.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

// The environment-variable provider above is a one-time snapshot with no file watcher. Layering
// the same file in as a watched source is what lets the admin settings screens change .env and
// have the running app pick it up.
var envFilePath = EnvironmentFile.FindPath();
if (envFilePath is not null)
{
    builder.Configuration.AddEnvFile(envFilePath);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["uint"] = typeof(UIntRouteConstraint);
});
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddVocaNovaAuthorization();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddBLL();
builder.Services.AddDAL(builder.Configuration);
builder.Services.AddSwaggerWithJwtBearer();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuditLogMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(ApiResponseFormatter.Success(new { status = "ok", service = "VocaNova.API" })))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

public partial class Program;

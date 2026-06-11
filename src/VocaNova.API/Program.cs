using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Responses;
using VocaNova.API.Features.Auth.Repositories;
using VocaNova.API.Infrastructure.Authentication;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

EnvironmentFile.LoadFromRepositoryRoot();

builder.Services.AddDbContext<VocaNovaDbContext>(options =>
{
    var connectionString = DatabaseConnection.GetConnectionString();

    options.UseMySql(
        connectionString,
        DatabaseConnection.GetServerVersion());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddVocaNovaAuthorization();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IAuditLogQueue, AuditLogQueue>();
builder.Services.AddHostedService<AuditLogBackgroundService>();
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

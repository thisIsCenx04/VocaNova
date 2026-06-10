using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Configuration;
using VocaNova.API.Infrastructure.Persistence;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "VocaNova.API" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

public partial class Program;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VocaNova.API.Infrastructure.Configuration;

namespace VocaNova.API.Infrastructure.Persistence;

public sealed class DesignTimeVocaNovaDbContextFactory : IDesignTimeDbContextFactory<VocaNovaDbContext>
{
    public VocaNovaDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.LoadFromRepositoryRoot();

        var connectionString = DatabaseConnection.GetConnectionString();
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseMySql(connectionString, DatabaseConnection.GetServerVersion())
            .Options;

        return new VocaNovaDbContext(options);
    }
}


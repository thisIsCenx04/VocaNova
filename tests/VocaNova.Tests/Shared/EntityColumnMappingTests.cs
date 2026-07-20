using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.Tests.Shared;

public class EntityColumnMappingTests
{
    // Every table in the VocaNova schema uses snake_case column names. A property
    // without an explicit HasColumnName silently falls back to its PascalCase
    // name, producing "Unknown column 'X'" errors at runtime that in-memory
    // tests never catch. This guards the whole model against that.
    [Fact]
    public void All_Mapped_Columns_Should_Be_SnakeCase()
    {
        using var context = CreateRelationalContext();

        var offenders = new List<string>();
        foreach (var entity in context.Model.GetEntityTypes())
        {
            var storeObject = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);
            if (storeObject is null)
            {
                continue;
            }

            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(storeObject.Value);
                if (column is not null && column.Any(char.IsUpper))
                {
                    offenders.Add($"{entity.ClrType.Name}.{property.Name} -> {column}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "every column must be mapped to its snake_case name; offenders are missing HasColumnName");
    }

    private static VocaNovaDbContext CreateRelationalContext()
    {
        // A real relational provider is required so column-name metadata is built.
        // No connection is opened while only inspecting the model.
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseMySql(
                "Server=localhost;Database=vocanova_model_only;",
                new MySqlServerVersion(new Version(8, 0, 21)))
            .Options;

        return new VocaNovaDbContext(options);
    }
}

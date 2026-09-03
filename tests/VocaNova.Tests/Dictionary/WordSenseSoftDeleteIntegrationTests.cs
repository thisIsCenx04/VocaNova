using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;

namespace VocaNova.Tests.Dictionary;

public sealed class WordSenseSoftDeleteIntegrationTests
{
    [Fact]
    public async Task MySql_Schema_Filter_Delete_And_Restore_Should_Match_Reviewed_Design()
    {
        EnvironmentFile.LoadFromRepositoryRoot();
        var options = new DbContextOptionsBuilder<VocaNovaDbContext>()
            .UseMySql(DatabaseConnection.GetConnectionString(), DatabaseConnection.GetServerVersion())
            .Options;
        await using var dbContext = new VocaNovaDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        var metadata = await ReadSchemaMetadataAsync(dbContext.Database.GetDbConnection());
        metadata.ColumnType.Should().Be("varchar(20)");
        metadata.IsNullable.Should().Be("NO");
        metadata.DefaultValue.Should().Be("active");
        metadata.Comment.Should().Be("active/deleted");
        metadata.IndexCount.Should().Be(1);
        dbContext.Model.FindEntityType(typeof(EntityWordSense))!.GetQueryFilter().Should().NotBeNull();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var suffix = Guid.NewGuid().ToString("N")[..12];
        var word = new EntityWord
        {
            Word1 = $"unit-{suffix}",
            WordKey = $"unit-{suffix}",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        word.WordSenses.Add(new EntityWordSense
        {
            SenseOrder = 1,
            WordClass = "noun",
            EnglishDefinition = "sense to delete",
            Status = UserStatus.Active,
        });
        word.WordSenses.Add(new EntityWordSense
        {
            SenseOrder = 2,
            WordClass = "noun",
            EnglishDefinition = "unrelated visible sense",
            Status = UserStatus.Active,
        });
        dbContext.Words.Add(word);
        await dbContext.SaveChangesAsync();
        var deletedSenseId = word.WordSenses.OrderBy(sense => sense.SenseOrder).First().SenseId;

        var admin = new WordAdminRepository(dbContext);
        (await admin.SetSenseStatusAsync(word.WordId, deletedSenseId, UserStatus.Deleted)).Should().BeTrue();

        (await dbContext.WordSenses.AnyAsync(sense => sense.SenseId == deletedSenseId)).Should().BeFalse();
        (await dbContext.WordSenses.IgnoreQueryFilters()
            .SingleAsync(sense => sense.SenseId == deletedSenseId)).Status.Should().Be(UserStatus.Deleted);
        (await admin.SenseExistsAsync(word.WordId, deletedSenseId, includeDeleted: true)).Should().BeTrue();
        var detailAfterDelete = await new WordReadRepository(dbContext).FindDetailAsync(word.WordId);
        detailAfterDelete!.Senses.Select(sense => sense.EnglishDefinition)
            .Should().Equal("unrelated visible sense");

        (await admin.SetSenseStatusAsync(word.WordId, deletedSenseId, UserStatus.Active)).Should().BeTrue();
        var detailAfterRestore = await new WordReadRepository(dbContext).FindDetailAsync(word.WordId);
        detailAfterRestore!.Senses.Select(sense => sense.EnglishDefinition)
            .Should().Equal("sense to delete", "unrelated visible sense");

        await transaction.RollbackAsync();
    }

    private static async Task<SchemaMetadata> ReadSchemaMetadataAsync(DbConnection connection)
    {
        await using var columnCommand = connection.CreateCommand();
        columnCommand.CommandText = """
            SELECT COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'word_senses'
              AND COLUMN_NAME = 'status';
            """;
        await using var reader = await columnCommand.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        var columnType = reader.GetString(0);
        var nullable = reader.GetString(1);
        var defaultValue = reader.GetString(2);
        var comment = reader.GetString(3);
        await reader.DisposeAsync();

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'word_senses'
              AND INDEX_NAME = 'idx_senses_status'
              AND COLUMN_NAME = 'status';
            """;
        var indexCount = Convert.ToInt32(await indexCommand.ExecuteScalarAsync());
        return new SchemaMetadata(columnType, nullable, defaultValue, comment, indexCount);
    }

    private sealed record SchemaMetadata(
        string ColumnType,
        string IsNullable,
        string DefaultValue,
        string Comment,
        int IndexCount);
}

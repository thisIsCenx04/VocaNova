using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Extensions;

namespace VocaNova.Tests.Shared;

public class ExtensionTests
{
    [Theory]
    [InlineData("  Hello  ", "hello")]
    [InlineData("RUN", "run")]
    [InlineData(" already-normal ", "already-normal")]
    public void NormalizeWord_Should_Trim_And_Lowercase(string value, string expected)
    {
        value.NormalizeWord().Should().Be(expected);
    }

    [Theory]
    [InlineData("  Hello!  ", "hello")]
    [InlineData("Correct answer.", "correct answer")]
    [InlineData("No punctuation", "no punctuation")]
    public void NormalizeAnswer_Should_Trim_Remove_Final_Punctuation_And_Lowercase(
        string value,
        string expected)
    {
        value.NormalizeAnswer().Should().Be(expected);
    }

    [Theory]
    [InlineData("0912345690", "091****90")]
    [InlineData(" 0987654321 ", "098****21")]
    [InlineData("12345", "*****")]
    public void MaskPhone_Should_Hide_Middle_Digits(string phone, string expected)
    {
        phone.MaskPhone().Should().Be(expected);
    }

    [Fact]
    public async Task ToPagedResultAsync_Should_Return_Page_With_Correct_Offset()
    {
        await using var dbContext = new TestPagingDbContext();
        await dbContext.Items.AddRangeAsync(
            Enumerable.Range(1, 12).Select(id => new TestPagingItem { Id = id, Name = $"Word {id}" }));
        await dbContext.SaveChangesAsync();

        var result = await dbContext.Items
            .OrderBy(item => item.Id)
            .Select(item => item.Id)
            .ToPagedResultAsync(page: 2, limit: 5);

        result.Items.Should().BeEquivalentTo(new[] { 6, 7, 8, 9, 10 }, options => options.WithStrictOrdering());
        result.Page.Should().Be(2);
        result.Limit.Should().Be(5);
        result.TotalItems.Should().Be(12);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task ToPagedResultAsync_Should_Work_With_InMemory_Queryable()
    {
        var query = Enumerable.Range(1, 7).AsQueryable();

        var result = await query.ToPagedResultAsync(page: 2, limit: 3);

        result.Items.Should().BeEquivalentTo(new[] { 4, 5, 6 }, options => options.WithStrictOrdering());
        result.TotalItems.Should().Be(7);
        result.TotalPages.Should().Be(3);
    }

    private sealed class TestPagingDbContext : DbContext
    {
        public DbSet<TestPagingItem> Items => Set<TestPagingItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }
    }

    private sealed class TestPagingItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}

using FluentAssertions;
using VocaNova.API.Common.Results;

namespace VocaNova.Tests.Shared;

public class PagedResultTests
{
    [Fact]
    public void Constructor_Should_Set_Pagination_Metadata()
    {
        var items = new[] { "word-6", "word-7", "word-8", "word-9", "word-10" };

        var result = new PagedResult<string>(items, page: 2, limit: 5, totalItems: 12);

        result.Items.Should().BeEquivalentTo(items, options => options.WithStrictOrdering());
        result.Page.Should().Be(2);
        result.Limit.Should().Be(5);
        result.TotalItems.Should().Be(12);
        result.TotalPages.Should().Be(3);
    }
}

using FluentAssertions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;

namespace VocaNova.Tests.Shared;

public class ApiResponseFormatterTests
{
    [Fact]
    public void Success_Should_Return_Standard_Response_Shape()
    {
        var data = new { Word = "hello" };

        var response = ApiResponseFormatter.Success(data);

        response.Success.Should().BeTrue();
        response.Data.Should().Be(data);
        response.Message.Should().Be("Success.");
        response.Errors.Should().BeEmpty();
        response.Pagination.Should().BeNull();
    }

    [Fact]
    public void Paged_Should_Move_Items_To_Data_And_Set_Pagination()
    {
        var pagedResult = new PagedResult<string>(
            new[] { "word-1", "word-2" },
            page: 2,
            limit: 2,
            totalItems: 5);

        var response = ApiResponseFormatter.Paged(pagedResult);

        response.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(new[] { "word-1", "word-2" }, options => options.WithStrictOrdering());
        response.Pagination.Should().NotBeNull();
        response.Pagination!.Page.Should().Be(2);
        response.Pagination.Limit.Should().Be(2);
        response.Pagination.TotalItems.Should().Be(5);
        response.Pagination.TotalPages.Should().Be(3);
    }

    [Fact]
    public void FromResult_Should_Return_Error_Response_For_Failed_Result()
    {
        var result = Result<string>.Conflict("Phone already exists.");

        var response = ApiResponseFormatter.FromResult(result);

        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Message.Should().Be("Phone already exists.");
        response.Errors.Should().ContainSingle("Phone already exists.");
    }
}

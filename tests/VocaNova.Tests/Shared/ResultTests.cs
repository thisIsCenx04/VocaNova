using FluentAssertions;
using Microsoft.AspNetCore.Http;
using VocaNova.API.Common.Results;

namespace VocaNova.Tests.Shared;

public class ResultTests
{
    [Theory]
    [MemberData(nameof(ResultFactories))]
    public void Factory_Should_Set_Expected_Status_Code(Result<string> result, int expectedStatusCode)
    {
        result.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void Ok_With_Value_Should_Create_Success_Result()
    {
        var result = Result<string>.Ok("created");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("created");
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void Fail_Should_Create_Error_Result()
    {
        var result = Result<string>.Fail("Invalid request.");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("Invalid request.");
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    public static TheoryData<Result<string>, int> ResultFactories()
    {
        return new TheoryData<Result<string>, int>
        {
            { Result<string>.Ok(), StatusCodes.Status200OK },
            { Result<string>.Fail(), StatusCodes.Status400BadRequest },
            { Result<string>.NotFound(), StatusCodes.Status404NotFound },
            { Result<string>.Conflict(), StatusCodes.Status409Conflict },
            { Result<string>.Unauthorized(), StatusCodes.Status401Unauthorized },
            { Result<string>.Forbidden(), StatusCodes.Status403Forbidden },
        };
    }
}

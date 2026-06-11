using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;

namespace VocaNova.Tests.Shared;

public class ControllerResultExtensionsTests
{
    [Fact]
    public void OkResult_Should_Return_200_With_Formatted_Response()
    {
        var controller = new TestController();

        var result = controller.OkResult("created");

        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeAssignableTo<ApiResponse<string>>();
    }

    [Fact]
    public void CreatedResult_Should_Return_201_With_Formatted_Response()
    {
        var controller = new TestController();

        var result = controller.CreatedResult("created");

        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Value.Should().BeAssignableTo<ApiResponse<string>>();
    }

    [Fact]
    public void ErrorResult_Should_Use_Result_Status_Code()
    {
        var controller = new TestController();

        var result = controller.ErrorResult(Result<string>.Forbidden());

        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.Value.Should().BeAssignableTo<ApiResponse<string>>();
    }

    private sealed class TestController : ControllerBase;
}

using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Responses;
using VocaNova.API.Common.Results;

namespace VocaNova.API.Common.Extensions;

public static class ControllerResultExtensions
{
    public static OkObjectResult OkResult<T>(
        this ControllerBase controller,
        T data,
        string? message = null)
    {
        return controller.Ok(ApiResponseFormatter.Success(data, message));
    }

    public static ObjectResult CreatedResult<T>(
        this ControllerBase controller,
        T data,
        string? message = null)
    {
        return controller.StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFormatter.Created(data, message));
    }

    public static ObjectResult ErrorResult<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        return controller.StatusCode(
            result.StatusCode,
            ApiResponseFormatter.FromResult(result));
    }
}

namespace Example.Rbac.Api.Common;

public static class Extensions
{
    internal static IResult ToProblemDetail(this Error error, string? instance = null, int statusCode = StatusCodes.Status500InternalServerError)
    {
        return Results.Problem(
            detail: error.Message,
            title: error.Code,
            statusCode: statusCode,
            instance: instance
        );
    }
}

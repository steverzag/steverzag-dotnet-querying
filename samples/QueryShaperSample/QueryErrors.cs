using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Steverzag.QueryShaper.Exceptions;

namespace QueryShaperSample;

// Every error in a request (a selector, a value, a condition, the page) is a QueryRequestException with the
// path of the error, so one filter turns them all into a 400.
public sealed class QueryRequestErrorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (QueryRequestException error)
        {
            return QueryErrors.Problem(error.Detail, error.Path);
        }
    }
}

// A body that doesn't bind (an unknown condition, a node with both "field" and "filters") fails before the
// endpoint runs, as a JsonException whose Path and message point at the error. ASP.NET only throws it when
// RouteHandlerOptions.ThrowOnBadRequest is on; otherwise the client gets an empty 400.
public sealed class JsonBindingErrorHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not BadHttpRequestException { InnerException: JsonException json })
            return false;

        await QueryErrors.Problem(json.Message, json.Path).ExecuteAsync(context);
        return true;
    }
}

public static class QueryErrors
{
    public static IResult Problem(string detail, string? path) => TypedResults.Problem(
        title: "The query request is invalid.",
        detail: detail,
        statusCode: StatusCodes.Status400BadRequest,
        extensions: new Dictionary<string, object?> { ["path"] = path });
}

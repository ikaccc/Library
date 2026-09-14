using System.Diagnostics;
using Google.Rpc;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Errors;

internal sealed class RpcExceptionHandler(IProblemDetailsService problemDetails, ILogger<RpcExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RpcException rpcException)
        {
            return false;
        }

        var richStatus = rpcException.GetRpcStatus();
        var (statusCode, title) = Map(rpcException.StatusCode);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(rpcException, "Lending service call failed with {GrpcStatus}", rpcException.StatusCode);
        }

        ProblemDetails problem = richStatus?.GetDetail<BadRequest>() is { } badRequest
            ? new ValidationProblemDetails(ToErrors(badRequest)) { Status = statusCode, Title = title, Detail = rpcException.Status.Detail }
            : new ProblemDetails { Status = statusCode, Title = title, Detail = rpcException.Status.Detail };

        if (richStatus?.GetDetail<ErrorInfo>() is { } errorInfo)
        {
            problem.Extensions["code"] = errorInfo.Reason;
        }

        problem.Extensions["grpcStatus"] = rpcException.StatusCode.ToString();
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static Dictionary<string, string[]> ToErrors(BadRequest badRequest) =>
        badRequest.FieldViolations
            .GroupBy(violation => ToJsonFieldName(violation.Field), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(violation => violation.Description).ToArray(), StringComparer.Ordinal);

    internal static string ToJsonFieldName(string protoField) =>
        string.Join('.', protoField.Split('.').Select(segment =>
        {
            var parts = segment.Split('_', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0
                ? segment
                : string.Concat(parts.Select((part, index) => index == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
        }));

    private static (int StatusCode, string Title) Map(StatusCode status) => status switch
    {
        StatusCode.InvalidArgument => (StatusCodes.Status400BadRequest, "The request is invalid."),
        StatusCode.NotFound => (StatusCodes.Status404NotFound, "The requested resource was not found."),
        StatusCode.AlreadyExists => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
        StatusCode.Aborted => (StatusCodes.Status409Conflict, "The request was aborted by a concurrent change. Retry it."),
        StatusCode.FailedPrecondition => (StatusCodes.Status422UnprocessableEntity, "A business rule prevents this operation."),
        StatusCode.PermissionDenied => (StatusCodes.Status403Forbidden, "Permission denied."),
        StatusCode.Unauthenticated => (StatusCodes.Status401Unauthorized, "Authentication is required."),
        StatusCode.ResourceExhausted => (StatusCodes.Status429TooManyRequests, "Too many requests."),
        StatusCode.Unavailable => (StatusCodes.Status503ServiceUnavailable, "The lending service is unavailable."),
        StatusCode.DeadlineExceeded => (StatusCodes.Status504GatewayTimeout, "The lending service did not respond in time."),
        StatusCode.Cancelled => (StatusCodes.Status499ClientClosedRequest, "The request was cancelled."),
        _ => (StatusCodes.Status502BadGateway, "The lending service returned an unexpected error."),
    };
}

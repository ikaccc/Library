using System.Text.Json;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Library.Lending.Domain.Common;
using GrpcStatus = Google.Rpc.Status;

namespace Library.Lending.Grpc.Errors;

public static class GrpcErrors
{
    public const string ErrorDomain = "library.lending.v1";

    public static RpcException ToRpcException(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = new GrpcStatus
        {
            Code = (int)error.Type.ToStatusCode(),
            Message = error.Message,
            Details = { Any.Pack(new ErrorInfo { Reason = ToReason(error.Code), Domain = ErrorDomain }) },
        };

        return status.ToRpcException();
    }

    public static RpcException ToRpcException(this ValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var badRequest = new BadRequest();
        foreach (var failure in exception.Errors)
        {
            badRequest.FieldViolations.Add(new BadRequest.Types.FieldViolation
            {
                Field = ToFieldName(failure.PropertyName),
                Description = failure.ErrorMessage,
            });
        }

        var status = new GrpcStatus
        {
            Code = (int)StatusCode.InvalidArgument,
            Message = "One or more request fields are invalid.",
            Details =
            {
                Any.Pack(new ErrorInfo { Reason = "INVALID_ARGUMENT", Domain = ErrorDomain }),
                Any.Pack(badRequest),
            },
        };

        return status.ToRpcException();
    }

    public static RpcException InvalidField(string field, string description) =>
        new ValidationException([new FluentValidation.Results.ValidationFailure(field, description)]).ToRpcException();

    public static RpcException Aborted(string reason, string message) =>
        new GrpcStatus
        {
            Code = (int)StatusCode.Aborted,
            Message = message,
            Details = { Any.Pack(new ErrorInfo { Reason = reason, Domain = ErrorDomain }) },
        }.ToRpcException();

    public static StatusCode ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCode.InvalidArgument,
        ErrorType.NotFound => StatusCode.NotFound,
        ErrorType.Conflict => StatusCode.AlreadyExists,
        ErrorType.PreconditionFailed => StatusCode.FailedPrecondition,
        _ => StatusCode.Unknown,
    };

    public static void ThrowIfFailure(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            throw result.Error!.ToRpcException();
        }
    }

    public static TValue GetValueOrThrow<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? result.Value : throw result.Error!.ToRpcException();
    }

    internal static string ToReason(string code) => code.Replace('.', '_').ToUpperInvariant();

    internal static string ToFieldName(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.SnakeCaseLower.ConvertName));
}

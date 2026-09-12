using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Exceptions;
using Library.Lending.Grpc.Errors;

namespace Library.Lending.Grpc.Interceptors;

public sealed class ExceptionHandlingInterceptor(ILogger<ExceptionHandlingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ValidationException exception)
        {
            throw exception.ToRpcException();
        }
        catch (ConcurrencyConflictException exception)
        {
            logger.LogWarning(exception, "Write conflict while handling {Method}", context.Method);
            throw GrpcErrors.Aborted("CONCURRENCY_CONFLICT", exception.Message);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "The call was cancelled by the client."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while handling {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred while processing the request."));
        }
    }
}

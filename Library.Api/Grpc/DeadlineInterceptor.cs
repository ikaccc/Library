using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Library.Api.Grpc;

internal sealed class DeadlineInterceptor(TimeSpan timeout, TimeProvider clock) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (context.Options.Deadline is null)
        {
            var options = context.Options.WithDeadline(clock.GetUtcNow().UtcDateTime.Add(timeout));
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        }

        return continuation(request, context);
    }
}

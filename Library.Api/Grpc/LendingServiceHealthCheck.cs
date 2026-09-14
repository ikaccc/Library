using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Library.Api.Grpc;

internal sealed class LendingServiceHealthCheck(Health.HealthClient health) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await health.CheckAsync(new HealthCheckRequest(), cancellationToken: cancellationToken);

            return response.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("Lending service is serving.")
                : HealthCheckResult.Unhealthy($"Lending service reports '{response.Status}'.");
        }
        catch (RpcException exception)
        {
            return HealthCheckResult.Unhealthy($"Lending service is unreachable: {exception.Status.StatusCode}.", exception);
        }
    }
}

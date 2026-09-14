using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client.Configuration;
using Library.Lending.Contracts.V1;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Library.Api.Grpc;

internal static class LendingServiceClients
{
    public static IServiceCollection AddLendingServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LendingServiceOptions>()
            .Bind(configuration.GetSection(LendingServiceOptions.SectionName))
            .Validate(options => options.Address.IsAbsoluteUri, "LendingService:Address must be an absolute URI.")
            .Validate(options => options.Timeout > TimeSpan.Zero, "LendingService:Timeout must be positive.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);

        services.AddLendingClient<BooksService.BooksServiceClient>();
        services.AddLendingClient<BorrowersService.BorrowersServiceClient>();
        services.AddLendingClient<LoansService.LoansServiceClient>();
        services.AddLendingClient<AnalyticsService.AnalyticsServiceClient>();
        services.AddLendingClient<Health.HealthClient>();

        return services;
    }

    private static void AddLendingClient<TClient>(this IServiceCollection services)
        where TClient : ClientBase
    {
        services
            .AddGrpcClient<TClient>((provider, options) =>
            {
                options.Address = provider.GetRequiredService<IOptions<LendingServiceOptions>>().Value.Address;
            })
            .ConfigureChannel(channel =>
            {
                channel.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 3,
                                InitialBackoff = TimeSpan.FromMilliseconds(200),
                                MaxBackoff = TimeSpan.FromSeconds(2),
                                BackoffMultiplier = 2,
                                RetryableStatusCodes = { StatusCode.Unavailable },
                            },
                        },
                    },
                };
            })
            .AddInterceptor(provider =>
            {
                var options = provider.GetRequiredService<IOptions<LendingServiceOptions>>().Value;
                return new DeadlineInterceptor(options.Timeout, provider.GetRequiredService<TimeProvider>());
            });
    }
}

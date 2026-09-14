// ai-touched
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

[Collection(PostgresCollection.Name)]
public class ReflectionTests(PostgresContainerFixture postgres)
{
    private static readonly string[] ContractServices =
    [
        "library.lending.v1.BooksService",
        "library.lending.v1.BorrowersService",
        "library.lending.v1.LoansService",
        "library.lending.v1.AnalyticsService",
    ];

    [Fact]
    public async Task Server_reflection_lists_every_contract_service_when_enabled()
    {
        var settings = new Dictionary<string, string?> { ["Grpc:Reflection"] = "true" };
        await using var host = new LendingGrpcFactory(await postgres.CreateDatabaseAsync(), settings: settings);
        using var channel = host.CreateGrpcChannel();

        var services = await ListServicesAsync(channel);

        foreach (var service in ContractServices)
        {
            services.ShouldContain(service);
        }

        services.ShouldContain("grpc.health.v1.Health");
    }

    [Fact]
    public async Task Server_reflection_is_off_by_default_outside_Development()
    {
        await using var host = new LendingGrpcFactory(await postgres.CreateDatabaseAsync());
        using var channel = host.CreateGrpcChannel();

        var exception = await Should.ThrowAsync<RpcException>(() => ListServicesAsync(channel));

        exception.StatusCode.ShouldBe(StatusCode.Unimplemented);
    }

    private static async Task<List<string>> ListServicesAsync(GrpcChannel channel)
    {
        var reflection = new ServerReflection.ServerReflectionClient(channel);
        using var call = reflection.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = string.Empty });
        await call.RequestStream.CompleteAsync();

        var services = new List<string>();
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            services.AddRange(response.ListServicesResponse.Service.Select(service => service.Name));
        }

        return services;
    }
}

using Grpc.Net.Client;
using Library.Lending.Grpc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Library.TestSupport;

public sealed class LendingGrpcFactory(string connectionString, FakeTimeProvider? clock = null) : WebApplicationFactory<GrpcEntryPoint>
{
    public FakeTimeProvider? Clock { get; } = clock;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Lending", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            if (Clock is not null)
            {
                services.AddSingleton<TimeProvider>(Clock);
            }
        });
    }

    public GrpcChannel CreateGrpcChannel() =>
        GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });
}

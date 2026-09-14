// ai-touched
using Grpc.Health.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

public class HealthTests(PostgresContainerFixture postgres) : GrpcServiceTest(postgres)
{
    [Fact]
    public async Task The_service_reports_healthy_over_grpc_and_http()
    {
        var health = new Health.HealthClient(Channel);
        var response = await health.CheckAsync(new HealthCheckRequest());
        response.Status.ShouldBe(HealthCheckResponse.Types.ServingStatus.Serving);

        using var http = Host.CreateClient();
        var httpResponse = await http.GetAsync(new Uri("/health", UriKind.Relative));
        httpResponse.EnsureSuccessStatusCode();
        (await httpResponse.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}

using Library.TestSupport;

namespace Library.SystemTests;

public class RateLimitingTests(PostgresContainerFixture postgres) : LibrarySystemTest(postgres)
{
    protected override IReadOnlyDictionary<string, string?>? ApiSettings => new Dictionary<string, string?>
    {
        ["RateLimiting:PermitLimit"] = "3",
        ["RateLimiting:Window"] = "00:01:00",
    };

    [Fact]
    public async Task A_client_over_the_limit_is_rejected_with_retry_after_while_probes_keep_working()
    {
        var books = new Uri("/api/v1/books", UriKind.Relative);
        for (var i = 0; i < 3; i++)
        {
            (await Client.GetAsync(books)).EnsureSuccessStatusCode();
        }

        var rejected = await Client.GetAsync(books);

        var problem = await ShouldBeProblemAsync(rejected, 429, "RATE_LIMITED");
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("3 requests per 60 seconds");
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        rejected.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ShouldBeGreaterThan(0);

        (await Client.GetAsync(new Uri("/health/live", UriKind.Relative))).EnsureSuccessStatusCode();
        (await Client.GetAsync(new Uri("/health", UriKind.Relative))).EnsureSuccessStatusCode();
    }
}

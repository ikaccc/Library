namespace Library.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; init; } = 100;

    public TimeSpan Window { get; init; } = TimeSpan.FromSeconds(10);
}
namespace Library.Api.Grpc;

public sealed class LendingServiceOptions
{
    public const string SectionName = "LendingService";

    public required Uri Address { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}

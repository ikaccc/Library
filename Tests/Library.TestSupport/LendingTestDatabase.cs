using Library.Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.TestSupport;

public sealed class LendingTestDatabase
{
    private LendingTestDatabase(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<LendingTestDatabase> CreateMigratedAsync(PostgresContainerFixture postgres, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        var connectionString = await postgres.CreateDatabaseAsync(cancellationToken);
        await using var db = CreateContext(connectionString);
        await db.Database.MigrateAsync(cancellationToken);

        return new LendingTestDatabase(connectionString);
    }

    public LendingDbContext CreateContext() => CreateContext(ConnectionString);

    public static LendingDbContext CreateContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<LendingDbContext>();
        LendingDbContextOptions.Configure(builder, connectionString);

        return new LendingDbContext(builder.Options);
    }
}

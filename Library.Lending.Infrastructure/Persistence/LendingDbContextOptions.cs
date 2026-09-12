using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence;

public static class LendingDbContextOptions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable(MigrationsHistoryTable, LendingDbContext.Schema)
                .EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention();
    }
}

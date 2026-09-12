using Library.Lending.Infrastructure.Persistence;
using Library.Lending.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Library.Lending.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeLendingDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (options.MigrateOnStartup)
        {
            var db = scope.ServiceProvider.GetRequiredService<LendingDbContext>();
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database schema is up to date ({PendingCount} migration(s) applied)", pending.Count);
        }

        if (options.SeedOnStartup)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<SampleDataSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
    }
}

// ai-touched
using Library.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresContainerFixture postgres)
{
    [Fact]
    public async Task Migrations_apply_to_an_empty_database_and_the_snapshot_matches_the_model()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = LendingTestDatabase.CreateContext(connectionString);

        await db.Database.MigrateAsync();

        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        db.Database.HasPendingModelChanges().ShouldBeFalse("a model change without a migration would break deployments");

        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'lending' ORDER BY table_name")
            .ToListAsync();
        tables.ShouldBe(["__ef_migrations_history", "books", "borrowers", "loans"]);
    }

    [Fact]
    public async Task Migrating_twice_is_a_no_op()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = LendingTestDatabase.CreateContext(connectionString);

        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();

        (await db.Database.GetAppliedMigrationsAsync()).Count().ShouldBe(1);
    }
}

using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Library.TestSupport;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    public const string Image = "postgres:18-alpine";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image).Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var databaseName = "lending_" + Guid.NewGuid().ToString("N")[..12];

        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = databaseName }.ConnectionString;
    }
}

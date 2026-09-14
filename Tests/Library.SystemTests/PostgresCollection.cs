// ai-touched
using Library.TestSupport;

namespace Library.SystemTests;

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}

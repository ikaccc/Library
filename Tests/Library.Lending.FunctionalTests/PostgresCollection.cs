// ai-touched
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}

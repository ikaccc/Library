namespace Library.Lending.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string ConnectionStringName = "Lending";

    public bool MigrateOnStartup { get; set; }

    public bool SeedOnStartup { get; set; }
}

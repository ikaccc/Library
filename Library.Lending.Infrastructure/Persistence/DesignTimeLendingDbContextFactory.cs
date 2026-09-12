using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Library.Lending.Infrastructure.Persistence;

internal sealed class DesignTimeLendingDbContextFactory : IDesignTimeDbContextFactory<LendingDbContext>
{
    public LendingDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LendingDbContext>();
        LendingDbContextOptions.Configure(builder, "Host=localhost;Database=library;Username=library;Password=library");

        return new LendingDbContext(builder.Options);
    }
}

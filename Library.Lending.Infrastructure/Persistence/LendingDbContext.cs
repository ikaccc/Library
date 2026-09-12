using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence;

public sealed class LendingDbContext(DbContextOptions<LendingDbContext> options) : DbContext(options)
{
    public const string Schema = "lending";

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Borrower> Borrowers => Set<Borrower>();

    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LendingDbContext).Assembly);
    }
}

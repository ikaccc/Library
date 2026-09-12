// ai-touched
using Library.Lending.Domain.Loans;
using Library.Lending.Infrastructure.Seeding;
using Library.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Library.Lending.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class SampleDataSeederTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private LendingTestDatabase _database = null!;

    public async Task InitializeAsync() => _database = await LendingTestDatabase.CreateMigratedAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Seeds_a_history_that_satisfies_every_lending_rule_and_is_idempotent()
    {
        var now = TimeProvider.System.GetUtcNow();
        SeedResult result;
        await using (var db = _database.CreateContext())
        {
            result = await CreateSeeder(db).SeedAsync(CancellationToken.None);
        }

        result.Seeded.ShouldBeTrue();
        result.Books.ShouldBe(40);
        result.Borrowers.ShouldBe(25);
        result.Loans.ShouldBeGreaterThan(300);
        result.OpenLoans.ShouldBeGreaterThan(0);

        await using var reader = _database.CreateContext();
        var books = await reader.Books.AsNoTracking().ToListAsync();
        var loans = await reader.Loans.AsNoTracking().ToListAsync();

        foreach (var book in books)
        {
            var open = loans.Count(l => l.BookId == book.Id && !l.IsReturned);
            book.AvailableCopies.ShouldBe(book.TotalCopies - open, $"inventory of '{book.Title}' must match its open loans");
            book.Isbn.ShouldNotBeNull();
        }

        loans.Where(l => !l.IsReturned)
            .GroupBy(l => l.BorrowerId)
            .ShouldAllBe(g => g.Count() <= LoanPolicy.Default.MaxOpenLoansPerBorrower);

        loans.Where(l => !l.IsReturned)
            .GroupBy(l => (l.BorrowerId, l.BookId))
            .ShouldAllBe(g => g.Count() == 1);

        loans.ShouldAllBe(l => l.DueAt == l.BorrowedAt.AddDays(LoanPolicy.Default.LoanPeriodDays));
        loans.ShouldAllBe(l => l.BorrowedAt <= now);
        loans.Where(l => l.IsReturned).ShouldAllBe(l => l.ReturnedAt >= l.BorrowedAt && l.ReturnedAt <= now);
        loans.Select(l => l.BorrowedAt).Min().ShouldBeLessThan(now.AddDays(-365), "the history should span well over a year");

        await using var again = _database.CreateContext();
        (await CreateSeeder(again).SeedAsync(CancellationToken.None)).Seeded.ShouldBeFalse();
        (await again.Loans.CountAsync()).ShouldBe(result.Loans);
    }

    [Fact]
    public void Synthetic_isbns_are_valid_and_unique()
    {
        var isbns = Enumerable.Range(0, 40).Select(SampleDataSeeder.SyntheticIsbn13).ToList();

        isbns.ShouldAllBe(isbn => Library.Lending.Domain.Books.Isbn.IsValid(isbn));
        isbns.Distinct().Count().ShouldBe(40);
    }

    private static SampleDataSeeder CreateSeeder(Library.Lending.Infrastructure.Persistence.LendingDbContext db) =>
        new(db, LoanPolicy.Default, TimeProvider.System, NullLogger<SampleDataSeeder>.Instance);
}

// ai-touched
using Library.Lending.Application.Common;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Infrastructure.Persistence.Analytics;
using Library.TestSupport;

namespace Library.Lending.IntegrationTests;

/// <summary>
/// One small, hand-built history with known answers:
///   Alice: A (Jan, returned), B (Feb, returned), C (Mar, open)
///   Bob:   A (Jan, returned), B (Apr, returned)
///   Carol: A (May, returned), D (Jun, returned), A again (Jul, open)
/// </summary>
[Collection(PostgresCollection.Name)]
public class AnalyticsQueriesTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Jan = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Feb = Jan.AddMonths(1);
    private static readonly DateTimeOffset Mar = Jan.AddMonths(2);
    private static readonly DateTimeOffset Apr = Jan.AddMonths(3);
    private static readonly DateTimeOffset May = Jan.AddMonths(4);
    private static readonly DateTimeOffset Jun = Jan.AddMonths(5);
    private static readonly DateTimeOffset Jul = Jan.AddMonths(6);

    private LendingTestDatabase _database = null!;
    private Book _a = null!;
    private Book _b = null!;
    private Book _c = null!;
    private Book _d = null!;
    private Borrower _alice = null!;
    private Borrower _bob = null!;
    private Borrower _carol = null!;

    public async Task InitializeAsync()
    {
        _database = await LendingTestDatabase.CreateMigratedAsync(postgres);

        _a = TestData.NewBook("Anna Karenina", "Leo Tolstoy", pageCount: 800);
        _b = TestData.NewBook("Beloved", "Toni Morrison", pageCount: 320);
        _c = TestData.NewBook("Candide", "Voltaire", pageCount: 120);
        _d = TestData.NewBook("Dune", "Frank Herbert", pageCount: 412);
        _alice = TestData.NewBorrower("Alice");
        _bob = TestData.NewBorrower("Bob");
        _carol = TestData.NewBorrower("Carol");

        var loans = new[]
        {
            TestData.BorrowAndReturn(_a, _alice, Jan, daysOnLoan: 10),
            TestData.BorrowAndReturn(_b, _alice, Feb, daysOnLoan: 4),
            TestData.Borrow(_c, _alice, Mar),
            TestData.BorrowAndReturn(_a, _bob, Jan.AddDays(1), daysOnLoan: 20),
            TestData.BorrowAndReturn(_b, _bob, Apr, daysOnLoan: 8),
            TestData.BorrowAndReturn(_a, _carol, May, daysOnLoan: 12),
            TestData.BorrowAndReturn(_d, _carol, Jun, daysOnLoan: 6),
            TestData.Borrow(_a, _carol, Jul),
        };

        await using var db = _database.CreateContext();
        db.Books.AddRange(_a, _b, _c, _d);
        db.Borrowers.AddRange(_alice, _bob, _carol);
        db.Loans.AddRange(loans);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Most_borrowed_books_all_time_rank_by_count_then_title()
    {
        await using var db = _database.CreateContext();

        var books = await new AnalyticsQueries(db).GetMostBorrowedBooksAsync(TimeRange.AllTime, top: 10, CancellationToken.None);

        books.Select(b => (b.Title, b.BorrowCount, b.UniqueBorrowerCount)).ShouldBe(
        [
            ("Anna Karenina", 4, 3),
            ("Beloved", 2, 2),
            ("Candide", 1, 1),
            ("Dune", 1, 1),
        ]);
        books[0].BookId.ShouldBe(_a.Id);
        books[0].Author.ShouldBe("Leo Tolstoy");
    }

    [Fact]
    public async Task Most_borrowed_books_respect_the_half_open_range_and_top()
    {
        await using var db = _database.CreateContext();
        var queries = new AnalyticsQueries(db);

        var febToMay = await queries.GetMostBorrowedBooksAsync(new TimeRange(Feb, May), top: 10, CancellationToken.None);
        febToMay.Select(b => (b.Title, b.BorrowCount)).ShouldBe([("Beloved", 2), ("Candide", 1)]);

        var janOnly = await queries.GetMostBorrowedBooksAsync(new TimeRange(Jan, Feb), top: 10, CancellationToken.None);
        janOnly.Select(b => (b.Title, b.BorrowCount, b.UniqueBorrowerCount)).ShouldBe([("Anna Karenina", 2, 2)]);

        var upperBoundIsExclusive = await queries.GetMostBorrowedBooksAsync(new TimeRange(null, Jan), top: 10, CancellationToken.None);
        upperBoundIsExclusive.ShouldBeEmpty();

        var topOne = await queries.GetMostBorrowedBooksAsync(TimeRange.AllTime, top: 1, CancellationToken.None);
        topOne.Select(b => b.Title).ShouldBe(["Anna Karenina"]);
    }

    [Fact]
    public async Task Top_borrowers_rank_by_loan_count_then_name()
    {
        await using var db = _database.CreateContext();
        var queries = new AnalyticsQueries(db);

        var allTime = await queries.GetTopBorrowersAsync(TimeRange.AllTime, top: 10, CancellationToken.None);
        allTime.Select(b => (b.FullName, b.LoanCount, b.UniqueBookCount)).ShouldBe(
        [
            ("Alice", 3, 3),
            ("Carol", 3, 2),
            ("Bob", 2, 2),
        ]);

        var springOnly = await queries.GetTopBorrowersAsync(new TimeRange(Mar, Jun), top: 10, CancellationToken.None);
        springOnly.Select(b => (b.FullName, b.LoanCount)).ShouldBe([("Alice", 1), ("Bob", 1), ("Carol", 1)]);
    }

    [Fact]
    public async Task Completed_loans_exclude_open_ones_and_carry_the_page_count()
    {
        await using var db = _database.CreateContext();

        var completed = await new AnalyticsQueries(db).GetCompletedLoansForBorrowerAsync(_alice.Id, CancellationToken.None);

        completed.Select(l => (l.Title, l.PageCount, (l.ReturnedAt - l.BorrowedAt).TotalDays)).ShouldBe(
        [
            ("Anna Karenina", 800, 10d),
            ("Beloved", 320, 4d),
        ]);
    }

    [Fact]
    public async Task Also_borrowed_ranks_other_books_by_co_borrowers_and_excludes_the_book_itself()
    {
        await using var db = _database.CreateContext();
        var queries = new AnalyticsQueries(db);

        var alsoWithA = await queries.GetAlsoBorrowedBooksAsync(_a.Id, top: 10, CancellationToken.None);
        alsoWithA.Select(b => (b.Title, b.CoBorrowerCount, b.LoanCount)).ShouldBe(
        [
            ("Beloved", 2, 2),
            ("Candide", 1, 1),
            ("Dune", 1, 1),
        ]);

        var alsoWithC = await queries.GetAlsoBorrowedBooksAsync(_c.Id, top: 10, CancellationToken.None);
        alsoWithC.Select(b => (b.Title, b.CoBorrowerCount, b.LoanCount)).ShouldBe(
        [
            ("Anna Karenina", 1, 1),
            ("Beloved", 1, 1),
        ]);

        var nobodyBorrowedIt = await queries.GetAlsoBorrowedBooksAsync(Guid.CreateVersion7(), top: 10, CancellationToken.None);
        nobodyBorrowedIt.ShouldBeEmpty();
    }
}

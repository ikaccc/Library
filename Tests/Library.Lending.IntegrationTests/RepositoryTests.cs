// ai-touched
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Exceptions;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Books;
using Library.Lending.Infrastructure.Persistence.Repositories;
using Library.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Npgsql;

namespace Library.Lending.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class RepositoryTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private LendingTestDatabase _database = null!;

    public async Task InitializeAsync() => _database = await LendingTestDatabase.CreateMigratedAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Book_round_trips_including_the_isbn_value_object()
    {
        var book = TestData.NewBook("Dune", "Frank Herbert", 412, copies: 2, isbn: "978-0-306-40615-7");

        await using (var db = _database.CreateContext())
        {
            new BookRepository(db).Add(book);
            await db.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var repository = new BookRepository(reader);

        var loaded = await repository.GetByIdAsync(book.Id, CancellationToken.None);
        loaded.ShouldNotBeNull();
        loaded.Title.ShouldBe("Dune");
        loaded.Isbn.ShouldBe(Isbn.Create("9780306406157").Value);
        loaded.AvailableCopies.ShouldBe(2);

        (await repository.ExistsAsync(book.Id, CancellationToken.None)).ShouldBeTrue();
        (await repository.ExistsWithIsbnAsync(Isbn.Create("978 0 306 40615 7").Value, CancellationToken.None)).ShouldBeTrue();
        (await repository.ExistsWithIsbnAsync(Isbn.Create("0306406152").Value, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Duplicate_isbn_is_rejected_by_the_database_as_a_conflict()
    {
        await using var db = _database.CreateContext();
        db.Books.Add(TestData.NewBook("First", isbn: "9780306406157"));
        await db.SaveChangesAsync();

        db.Books.Add(TestData.NewBook("Second", isbn: "978-0-306-40615-7"));

        await Should.ThrowAsync<ConcurrencyConflictException>(() => new EfUnitOfWorkProxy(db).SaveChangesAsync());
    }

    [Fact]
    public async Task ListBooks_searches_title_and_author_case_insensitively_and_treats_wildcards_literally()
    {
        await using (var db = _database.CreateContext())
        {
            db.Books.AddRange(
                TestData.NewBook("Moby Dick", "Herman Melville"),
                TestData.NewBook("100% Cotton", "Textile Guild"),
                TestData.NewBook("Under_score", "Someone"),
                TestData.NewBook("Anything", "Melville Junior"));
            await db.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var repository = new BookRepository(reader);

        (await repository.ListAsync("MELVILLE", 1, 10, CancellationToken.None)).Items.Select(b => b.Title).ShouldBe(["Anything", "Moby Dick"]);
        (await repository.ListAsync("100%", 1, 10, CancellationToken.None)).Items.Select(b => b.Title).ShouldBe(["100% Cotton"]);
        (await repository.ListAsync("_", 1, 10, CancellationToken.None)).Items.Select(b => b.Title).ShouldBe(["Under_score"]);
        (await repository.ListAsync("nothing-like-this", 1, 10, CancellationToken.None)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListBooks_pages_in_title_order()
    {
        await using (var db = _database.CreateContext())
        {
            db.Books.AddRange(Enumerable.Range(1, 5).Select(i => TestData.NewBook($"Title {i:D2}")));
            await db.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var repository = new BookRepository(reader);

        var page2 = await repository.ListAsync(null, page: 2, pageSize: 2, CancellationToken.None);

        page2.Items.Select(b => b.Title).ShouldBe(["Title 03", "Title 04"]);
        page2.TotalCount.ShouldBe(5);
        page2.TotalPages.ShouldBe(3);
        page2.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task Loans_can_be_filtered_by_borrower_status_and_book()
    {
        var book = TestData.NewBook(copies: 5);
        var otherBook = TestData.NewBook("Other", copies: 5);
        var borrower = TestData.NewBorrower("Ada");
        var otherBorrower = TestData.NewBorrower("Grace");
        var now = TestData.Anchor.AddDays(30);

        var openLoan = TestData.Borrow(book, borrower, now.AddDays(-3)); // due in 11 days
        var overdueLoan = TestData.Borrow(otherBook, borrower, now.AddDays(-20)); // due 6 days ago
        var returnedLoan = TestData.BorrowAndReturn(book, borrower, now.AddDays(-40), daysOnLoan: 7);
        var someoneElsesLoan = TestData.Borrow(book, otherBorrower, now.AddDays(-1));

        await using (var db = _database.CreateContext())
        {
            db.Books.AddRange(book, otherBook);
            db.Borrowers.AddRange(borrower, otherBorrower);
            db.Loans.AddRange(openLoan, overdueLoan, returnedLoan, someoneElsesLoan);
            await db.SaveChangesAsync();
        }

        await using var reader = _database.CreateContext();
        var repository = new LoanRepository(reader);

        (await repository.GetOpenLoansForBorrowerAsync(borrower.Id, CancellationToken.None)).Select(l => l.Id)
            .ShouldBe([overdueLoan.Id, openLoan.Id]);

        async Task<IEnumerable<Guid>> Ids(LoanFilter filter)
        {
            var page = await repository.ListAsync(filter, 1, 10, CancellationToken.None);
            return page.Items.Select(l => l.Id);
        }

        (await Ids(new LoanFilter(null, borrower.Id, LoanStatus.Open, now))).ShouldBe([openLoan.Id]);
        (await Ids(new LoanFilter(null, borrower.Id, LoanStatus.Overdue, now))).ShouldBe([overdueLoan.Id]);
        (await Ids(new LoanFilter(null, borrower.Id, LoanStatus.Returned, now))).ShouldBe([returnedLoan.Id]);
        (await Ids(new LoanFilter(book.Id, null, null, now))).ShouldBe([someoneElsesLoan.Id, openLoan.Id, returnedLoan.Id]);
        (await Ids(new LoanFilter(null, null, null, now))).Count().ShouldBe(4);
    }

    [Fact]
    public async Task Paging_fetches_items_and_total_in_one_round_trip()
    {
        await using (var db = _database.CreateContext())
        {
            db.Books.AddRange(Enumerable.Range(1, 5).Select(i => TestData.NewBook($"Title {i:D2}")));
            await db.SaveChangesAsync();
        }

        var executedCommands = new List<string>();
        var options = new DbContextOptionsBuilder<Infrastructure.Persistence.LendingDbContext>();
        Infrastructure.Persistence.LendingDbContextOptions.Configure(options, _database.ConnectionString);
        options.LogTo(executedCommands.Add, [RelationalEventId.CommandExecuted]);
        await using var reader = new Infrastructure.Persistence.LendingDbContext(options.Options);
        var repository = new BookRepository(reader);

        var firstPage = await repository.ListAsync(null, page: 1, pageSize: 2, CancellationToken.None);
        firstPage.Items.Count.ShouldBe(2);
        firstPage.TotalCount.ShouldBe(5);
        executedCommands.Count.ShouldBe(1, "a populated page must not need a separate count query");

        executedCommands.Clear();
        var pastTheEnd = await repository.ListAsync(null, page: 4, pageSize: 2, CancellationToken.None);
        pastTheEnd.Items.ShouldBeEmpty();
        pastTheEnd.TotalCount.ShouldBe(5, "an empty page still reports the real total");
        pastTheEnd.TotalPages.ShouldBe(3);
        executedCommands.Count.ShouldBe(2, "only a page past the end pays for a second query");
    }

    [Fact]
    public async Task Loan_history_is_visible_per_book_and_per_borrower()
    {
        var lentBook = TestData.NewBook("Lent");
        var untouchedBook = TestData.NewBook("Untouched");
        var reader = TestData.NewBorrower("Reader");
        var newcomer = TestData.NewBorrower("Newcomer");
        var loan = TestData.BorrowAndReturn(lentBook, reader, TestData.Anchor, daysOnLoan: 3);

        await using (var db = _database.CreateContext())
        {
            db.Books.AddRange(lentBook, untouchedBook);
            db.Borrowers.AddRange(reader, newcomer);
            db.Loans.Add(loan);
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        var loans = new LoanRepository(context);

        (await loans.ExistsForBookAsync(lentBook.Id, CancellationToken.None)).ShouldBeTrue("returned loans are history too");
        (await loans.ExistsForBookAsync(untouchedBook.Id, CancellationToken.None)).ShouldBeFalse();
        (await loans.ExistsForBorrowerAsync(reader.Id, CancellationToken.None)).ShouldBeTrue();
        (await loans.ExistsForBorrowerAsync(newcomer.Id, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task The_database_itself_refuses_deleting_a_book_with_loans()
    {
        var book = TestData.NewBook();
        var borrower = TestData.NewBorrower();
        var loan = TestData.Borrow(book, borrower, TestData.Anchor);
        await using (var db = _database.CreateContext())
        {
            db.Books.Add(book);
            db.Borrowers.Add(borrower);
            db.Loans.Add(loan);
            await db.SaveChangesAsync();
        }

        await using var deleter = _database.CreateContext();
        deleter.Books.Remove(await deleter.Books.SingleAsync(b => b.Id == book.Id));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => deleter.SaveChangesAsync());

        exception.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.RestrictViolation);
    }

    /// <summary>Thin wrapper so the test reads like the application code that uses the unit of work.</summary>
    private sealed class EfUnitOfWorkProxy(Infrastructure.Persistence.LendingDbContext db)
    {
        public Task SaveChangesAsync() => new Infrastructure.Persistence.EfUnitOfWork(db).SaveChangesAsync(CancellationToken.None);
    }
}

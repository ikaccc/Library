// ai-touched
using System.Net.Http.Json;
using Library.Api.Contracts;
using Library.TestSupport;

namespace Library.SystemTests;

public class LendingFlowTests(PostgresContainerFixture postgres) : LibrarySystemTest(postgres)
{
    /// <summary>Query-string safe ISO 8601: the "+" of a UTC offset would otherwise decode as a space.</summary>
    private static string Q(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("O"));

    [Fact]
    public async Task A_librarian_registers_stock_and_members_lends_books_and_gets_insights()
    {
        var dune = await RegisterBookAsync("Dune", "Frank Herbert", pageCount: 400, copies: 2, isbn: "978-0-306-40615-7");
        var emma = await RegisterBookAsync("Emma", "Jane Austen", pageCount: 200, copies: 1);
        var alice = await RegisterBorrowerAsync("Alice", "alice@example.com");
        var bob = await RegisterBorrowerAsync("Bob");

        dune.Isbn.ShouldBe("9780306406157");
        alice.Email.ShouldBe("alice@example.com");

        var aliceDune = await BorrowAsync(dune, alice);
        var aliceEmma = await BorrowAsync(emma, alice);
        await BorrowAsync(dune, bob);

        aliceDune.Status.ShouldBe(LoanStatus.Open);
        aliceDune.DueAt.ShouldBe(Start.AddDays(14));
        (await GetAsync<BookResponse>($"/api/v1/books/{dune.Id}")).AvailableCopies.ShouldBe(0);

        Clock.Advance(TimeSpan.FromDays(5));
        var returned = await ReturnAsync(aliceDune);
        returned.Status.ShouldBe(LoanStatus.Returned);
        returned.ReturnedAt.ShouldBe(Start.AddDays(5));
        (await GetAsync<BookResponse>($"/api/v1/books/{dune.Id}")).AvailableCopies.ShouldBe(1);

        Clock.Advance(TimeSpan.FromDays(5));
        await ReturnAsync(aliceEmma);

        var mostBorrowed = await GetAsync<List<MostBorrowedBookResponse>>("/api/v1/analytics/books/most-borrowed?top=5");
        mostBorrowed.Select(b => (b.Title, b.BorrowCount, b.UniqueBorrowerCount)).ShouldBe([("Dune", 2, 2), ("Emma", 1, 1)]);

        var topBorrowers = await GetAsync<List<TopBorrowerResponse>>($"/api/v1/analytics/borrowers/top?from={Q(Start)}&to={Q(Start.AddDays(1))}");
        topBorrowers.Select(b => (b.FullName, b.LoanCount)).ShouldBe([("Alice", 2), ("Bob", 1)]);

        var pace = await GetAsync<ReadingPaceResponse>($"/api/v1/analytics/borrowers/{alice.Id}/reading-pace");
        pace.LoansConsidered.ShouldBe(2);
        pace.PagesPerDay.ShouldBe(40d); // 600 pages over 5 + 10 days
        pace.Loans.Select(l => (l.Title, l.Days, l.PagesPerDay)).ShouldBe([("Emma", 10d, 20d), ("Dune", 5d, 80d)]);

        var alsoBorrowed = await GetAsync<List<AlsoBorrowedBookResponse>>($"/api/v1/analytics/books/{dune.Id}/also-borrowed");
        alsoBorrowed.Select(b => (b.Title, b.CoBorrowerCount)).ShouldBe([("Emma", 1)]);

        var loans = await GetAsync<PagedResponse<LoanResponse>>($"/api/v1/loans?borrowerId={alice.Id}&status=Returned");
        loans.TotalCount.ShouldBe(2);
        loans.Items.ShouldAllBe(l => l.Status == LoanStatus.Returned);

        var books = await GetAsync<PagedResponse<BookResponse>>("/api/v1/books?page=2&pageSize=1");
        books.Items.Select(b => b.Title).ShouldBe(["Emma"]);
        books.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Failures_become_problem_details_with_machine_readable_codes()
    {
        var book = await RegisterBookAsync(copies: 1);
        var alice = await RegisterBorrowerAsync("Alice");
        var bob = await RegisterBorrowerAsync("Bob");
        var loan = await BorrowAsync(book, alice);

        var notFound = await Client.GetAsync(new Uri($"/api/v1/books/{Guid.NewGuid()}", UriKind.Relative));
        await ShouldBeProblemAsync(notFound, 404, "BOOK_NOT_FOUND");

        var noCopies = await Client.PostAsJsonAsync("/api/v1/loans", new BorrowBookRequest(book.Id, bob.Id), Json);
        var problem = await ShouldBeProblemAsync(noCopies, 422, "BOOK_NO_AVAILABLE_COPIES");
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("currently on loan");

        await ReturnAsync(loan);
        var twice = await Client.PostAsync(new Uri($"/api/v1/loans/{loan.Id}/return", UriKind.Relative), content: null);
        await ShouldBeProblemAsync(twice, 409, "LOAN_ALREADY_RETURNED");

        var invalid = await Client.PostAsJsonAsync("/api/v1/books", new RegisterBookRequest("", "Author", "bad-isbn", 0, 0), Json);
        var validation = await ShouldBeProblemAsync(invalid, 400, "INVALID_ARGUMENT");
        var errors = validation.Extensions["errors"]!.ToString()!;
        errors.ShouldContain("\"title\"");
        errors.ShouldContain("\"isbn\"");
        errors.ShouldContain("\"pageCount\"");
        errors.ShouldContain("\"totalCopies\"");

        var invertedRange = await Client.GetAsync(new Uri($"/api/v1/analytics/books/most-borrowed?from={Q(Start.AddDays(1))}&to={Q(Start)}", UriKind.Relative));
        var rangeProblem = await ShouldBeProblemAsync(invertedRange, 400, "INVALID_ARGUMENT");
        rangeProblem.Extensions["errors"]!.ToString()!.ShouldContain("\"range\"");
    }

    [Fact]
    public async Task The_api_documents_itself_and_reports_health()
    {
        var openApi = await Client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        openApi.ShouldContain("/api/v1/books/most-borrowed".Replace("/books/most-borrowed", "/analytics/books/most-borrowed", StringComparison.Ordinal));
        openApi.ShouldContain("\"/api/v1/loans/{id}/return\"");

        var scalar = await Client.GetAsync(new Uri("/scalar", UriKind.Relative));
        scalar.EnsureSuccessStatusCode();

        var root = await Client.GetAsync(new Uri("/", UriKind.Relative));
        root.EnsureSuccessStatusCode();
        root.RequestMessage!.RequestUri!.AbsolutePath.ShouldStartWith("/scalar");

        var health = await Client.GetAsync(new Uri("/health", UriKind.Relative));
        health.EnsureSuccessStatusCode();
        (await health.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }
}

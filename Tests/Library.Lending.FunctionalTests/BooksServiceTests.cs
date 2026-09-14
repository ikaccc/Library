// ai-touched
using Grpc.Core;
using Library.Lending.Contracts.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

public class BooksServiceTests(PostgresContainerFixture postgres) : GrpcServiceTest(postgres)
{
    [Fact]
    public async Task Registering_a_book_makes_it_retrievable_with_a_normalized_isbn()
    {
        var registered = await RegisterBookAsync("Dune", "Frank Herbert", pageCount: 412, copies: 3, isbn: "978-0-306-40615-7");

        registered.Id.ShouldNotBeNullOrEmpty();
        registered.Isbn.ShouldBe("9780306406157");
        registered.AvailableCopies.ShouldBe(3);
        registered.RegisteredAt.ToDateTimeOffset().ShouldBe(Start);

        var fetched = await Books.GetBookAsync(new GetBookRequest { Id = registered.Id });
        fetched.ShouldBe(registered);
    }

    [Fact]
    public async Task Listing_supports_search_and_paging()
    {
        await RegisterBookAsync("Moby-Dick", "Herman Melville");
        await RegisterBookAsync("Emma", "Jane Austen");
        await RegisterBookAsync("Persuasion", "Jane Austen");

        var austen = await Books.ListBooksAsync(new ListBooksRequest { Search = "austen" });
        austen.Books.Select(b => b.Title).ShouldBe(["Emma", "Persuasion"]);
        austen.TotalCount.ShouldBe(2);
        austen.Page.ShouldBe(1);
        austen.PageSize.ShouldBe(20);

        var secondPage = await Books.ListBooksAsync(new ListBooksRequest { Page = 2, PageSize = 2 });
        secondPage.Books.Select(b => b.Title).ShouldBe(["Persuasion"]);
        secondPage.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Invalid_input_is_rejected_with_field_level_details()
    {
        var call = Books.RegisterBookAsync(new RegisterBookRequest { Title = "", Author = "Someone", Isbn = "nope", PageCount = 0, TotalCopies = 0 });

        var exception = await ShouldFailAsync(call, StatusCode.InvalidArgument);

        ReasonOf(exception).ShouldBe("INVALID_ARGUMENT");
        var violations = FieldViolationsOf(exception);
        violations.Keys.ShouldBe(["title", "isbn", "page_count", "total_copies"], ignoreOrder: true);
        violations["isbn"].ShouldContain("ISBN");
    }

    [Fact]
    public async Task Registering_the_same_isbn_twice_is_a_conflict()
    {
        await RegisterBookAsync("First", isbn: "9780306406157");

        var exception = await ShouldFailAsync(Books.RegisterBookAsync(new RegisterBookRequest { Title = "Second", Author = "X", Isbn = "978-0-306-40615-7", PageCount = 10, TotalCopies = 1 }), StatusCode.AlreadyExists);

        ReasonOf(exception).ShouldBe("BOOK_DUPLICATE_ISBN");
    }

    [Fact]
    public async Task Unknown_or_malformed_ids_are_reported_precisely()
    {
        var notFound = await ShouldFailAsync(Books.GetBookAsync(new GetBookRequest { Id = Guid.NewGuid().ToString() }), StatusCode.NotFound);
        ReasonOf(notFound).ShouldBe("BOOK_NOT_FOUND");

        var malformed = await ShouldFailAsync(Books.GetBookAsync(new GetBookRequest { Id = "not-a-guid" }), StatusCode.InvalidArgument);
        FieldViolationsOf(malformed).Keys.ShouldBe(["id"]);
    }

    [Fact]
    public async Task Page_size_above_the_maximum_is_rejected()
    {
        var exception = await ShouldFailAsync(Books.ListBooksAsync(new ListBooksRequest { PageSize = 101 }), StatusCode.InvalidArgument);

        FieldViolationsOf(exception).Keys.ShouldBe(["page_size"]);
    }
}

// ai-touched
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Library.Lending.Contracts.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

public class AnalyticsServiceTests(PostgresContainerFixture postgres) : GrpcServiceTest(postgres)
{
    [Fact]
    public async Task The_four_business_questions_are_answered_from_the_lending_history()
    {
        // Day 0: Alice and Bob both take "Dune"; Alice also takes "Emma".
        var dune = await RegisterBookAsync("Dune", pageCount: 400, copies: 3);
        var emma = await RegisterBookAsync("Emma", pageCount: 200, copies: 3);
        var alice = await RegisterBorrowerAsync("Alice");
        var bob = await RegisterBorrowerAsync("Bob");
        var aliceDune = await BorrowAsync(dune, alice);
        var aliceEmma = await BorrowAsync(emma, alice);
        await BorrowAsync(dune, bob);

        // Day 10: Alice returns both (400 + 200 pages in 10 + 10 days = 30 pages/day).
        Clock.Advance(TimeSpan.FromDays(10));
        await ReturnAsync(aliceDune);
        await ReturnAsync(aliceEmma);

        // Day 20: Bob takes "Emma" as well.
        Clock.Advance(TimeSpan.FromDays(10));
        await BorrowAsync(emma, bob);

        var mostBorrowed = await Analytics.GetMostBorrowedBooksAsync(new GetMostBorrowedBooksRequest());
        mostBorrowed.Books.Select(b => (b.Title, b.BorrowCount, b.UniqueBorrowerCount)).ShouldBe([("Dune", 2, 2), ("Emma", 2, 2)]);

        var lastWeekOnly = await Analytics.GetMostBorrowedBooksAsync(new GetMostBorrowedBooksRequest
        {
            Range = new TimeRange { From = Timestamp.FromDateTimeOffset(Start.AddDays(15)) },
        });
        lastWeekOnly.Books.Select(b => (b.Title, b.BorrowCount)).ShouldBe([("Emma", 1)]);

        var topBorrowers = await Analytics.GetTopBorrowersAsync(new GetTopBorrowersRequest { Top = 1 });
        topBorrowers.Borrowers.Select(b => (b.FullName, b.LoanCount, b.UniqueBookCount)).ShouldBe([("Alice", 2, 2)]);

        var pace = await Analytics.GetReadingPaceAsync(new GetReadingPaceRequest { BorrowerId = alice.Id });
        pace.FullName.ShouldBe("Alice");
        pace.LoansConsidered.ShouldBe(2);
        pace.PagesPerDay.ShouldBe(30d);
        pace.Loans.Select(l => (l.Title, l.Days, l.PagesPerDay)).ShouldBe([("Dune", 10d, 40d), ("Emma", 10d, 20d)], ignoreOrder: true);

        var bobPace = await Analytics.GetReadingPaceAsync(new GetReadingPaceRequest { BorrowerId = bob.Id });
        bobPace.LoansConsidered.ShouldBe(0);
        bobPace.HasPagesPerDay.ShouldBeFalse();

        var alsoBorrowed = await Analytics.GetAlsoBorrowedBooksAsync(new GetAlsoBorrowedBooksRequest { BookId = dune.Id });
        alsoBorrowed.Books.Select(b => (b.Title, b.CoBorrowerCount, b.LoanCount)).ShouldBe([("Emma", 2, 2)]);
    }

    [Fact]
    public async Task Analytics_validate_their_input_and_report_unknown_subjects()
    {
        var invertedRange = await ShouldFailAsync(
            Analytics.GetMostBorrowedBooksAsync(new GetMostBorrowedBooksRequest
            {
                Range = new TimeRange { From = Timestamp.FromDateTimeOffset(Start), To = Timestamp.FromDateTimeOffset(Start.AddDays(-1)) },
            }),
            StatusCode.InvalidArgument);
        FieldViolationsOf(invertedRange).Keys.ShouldBe(["range"]);

        var tooMany = await ShouldFailAsync(Analytics.GetTopBorrowersAsync(new GetTopBorrowersRequest { Top = 101 }), StatusCode.InvalidArgument);
        FieldViolationsOf(tooMany).Keys.ShouldBe(["top"]);

        var unknownBorrower = await ShouldFailAsync(Analytics.GetReadingPaceAsync(new GetReadingPaceRequest { BorrowerId = Guid.NewGuid().ToString() }), StatusCode.NotFound);
        ReasonOf(unknownBorrower).ShouldBe("BORROWER_NOT_FOUND");

        var unknownBook = await ShouldFailAsync(Analytics.GetAlsoBorrowedBooksAsync(new GetAlsoBorrowedBooksRequest { BookId = Guid.NewGuid().ToString() }), StatusCode.NotFound);
        ReasonOf(unknownBook).ShouldBe("BOOK_NOT_FOUND");
    }

    [Fact]
    public async Task Empty_history_yields_empty_rankings_not_errors()
    {
        (await Analytics.GetMostBorrowedBooksAsync(new GetMostBorrowedBooksRequest())).Books.ShouldBeEmpty();
        (await Analytics.GetTopBorrowersAsync(new GetTopBorrowersRequest())).Borrowers.ShouldBeEmpty();
    }
}

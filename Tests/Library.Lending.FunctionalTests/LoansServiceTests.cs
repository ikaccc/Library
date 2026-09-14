// ai-touched
using Grpc.Core;
using Library.Lending.Contracts.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

public class LoansServiceTests(PostgresContainerFixture postgres) : GrpcServiceTest(postgres)
{
    [Fact]
    public async Task Borrowing_opens_a_loan_due_in_fourteen_days_and_takes_a_copy()
    {
        var book = await RegisterBookAsync(copies: 2);
        var borrower = await RegisterBorrowerAsync();

        var loan = await BorrowAsync(book, borrower);

        loan.BookId.ShouldBe(book.Id);
        loan.BorrowerId.ShouldBe(borrower.Id);
        loan.Status.ShouldBe(LoanStatus.Open);
        loan.BorrowedAt.ToDateTimeOffset().ShouldBe(Start);
        loan.DueAt.ToDateTimeOffset().ShouldBe(Start.AddDays(14));
        loan.ReturnedAt.ShouldBeNull();

        var bookAfter = await Books.GetBookAsync(new GetBookRequest { Id = book.Id });
        bookAfter.AvailableCopies.ShouldBe(1);
    }

    [Fact]
    public async Task Returning_closes_the_loan_and_puts_the_copy_back()
    {
        var book = await RegisterBookAsync(copies: 1);
        var borrower = await RegisterBorrowerAsync();
        var loan = await BorrowAsync(book, borrower);
        Clock.Advance(TimeSpan.FromDays(3));

        var returned = await ReturnAsync(loan);

        returned.Status.ShouldBe(LoanStatus.Returned);
        returned.ReturnedAt!.ToDateTimeOffset().ShouldBe(Start.AddDays(3));
        (await Books.GetBookAsync(new GetBookRequest { Id = book.Id })).AvailableCopies.ShouldBe(1);

        var again = await ShouldFailAsync(Loans.ReturnBookAsync(new ReturnBookRequest { LoanId = loan.Id }), StatusCode.AlreadyExists);
        ReasonOf(again).ShouldBe("LOAN_ALREADY_RETURNED");
    }

    [Fact]
    public async Task The_last_copy_cannot_be_borrowed_twice()
    {
        var book = await RegisterBookAsync(copies: 1);
        var first = await RegisterBorrowerAsync("First");
        var second = await RegisterBorrowerAsync("Second");
        await BorrowAsync(book, first);

        var exception = await ShouldFailAsync(Loans.BorrowBookAsync(new BorrowBookRequest { BookId = book.Id, BorrowerId = second.Id }), StatusCode.FailedPrecondition);

        ReasonOf(exception).ShouldBe("BOOK_NO_AVAILABLE_COPIES");
    }

    [Fact]
    public async Task A_borrower_cannot_exceed_the_open_loan_limit_or_hold_the_same_title_twice()
    {
        var borrower = await RegisterBorrowerAsync();
        var books = new List<Book>();
        for (var i = 0; i < 5; i++)
        {
            books.Add(await RegisterBookAsync($"Book {i}", copies: 2));
            await BorrowAsync(books[i], borrower);
        }

        var sixth = await RegisterBookAsync("Book 6");
        var limit = await ShouldFailAsync(Loans.BorrowBookAsync(new BorrowBookRequest { BookId = sixth.Id, BorrowerId = borrower.Id }), StatusCode.FailedPrecondition);
        ReasonOf(limit).ShouldBe("LOAN_OPEN_LOAN_LIMIT_REACHED");

        var otherBorrower = await RegisterBorrowerAsync("Other");
        await BorrowAsync(books[0], otherBorrower);
        var sameTitle = await ShouldFailAsync(Loans.BorrowBookAsync(new BorrowBookRequest { BookId = books[0].Id, BorrowerId = otherBorrower.Id }), StatusCode.AlreadyExists);
        ReasonOf(sameTitle).ShouldBe("LOAN_BOOK_ALREADY_ON_LOAN_TO_BORROWER");
    }

    [Fact]
    public async Task Unknown_book_or_borrower_is_not_found()
    {
        var book = await RegisterBookAsync();
        var borrower = await RegisterBorrowerAsync();

        var noBook = await ShouldFailAsync(Loans.BorrowBookAsync(new BorrowBookRequest { BookId = Guid.NewGuid().ToString(), BorrowerId = borrower.Id }), StatusCode.NotFound);
        ReasonOf(noBook).ShouldBe("BOOK_NOT_FOUND");

        var noBorrower = await ShouldFailAsync(Loans.BorrowBookAsync(new BorrowBookRequest { BookId = book.Id, BorrowerId = Guid.NewGuid().ToString() }), StatusCode.NotFound);
        ReasonOf(noBorrower).ShouldBe("BORROWER_NOT_FOUND");

        var noLoan = await ShouldFailAsync(Loans.GetLoanAsync(new GetLoanRequest { Id = Guid.NewGuid().ToString() }), StatusCode.NotFound);
        ReasonOf(noLoan).ShouldBe("LOAN_NOT_FOUND");
    }

    [Fact]
    public async Task Loans_become_overdue_when_the_due_date_passes_and_can_be_listed_by_status()
    {
        var book = await RegisterBookAsync(copies: 3);
        var borrower = await RegisterBorrowerAsync();
        var returnedLoan = await BorrowAsync(book, borrower);
        await ReturnAsync(returnedLoan);
        var overdueLoan = await BorrowAsync(book, borrower);
        Clock.Advance(TimeSpan.FromDays(15));
        var otherBook = await RegisterBookAsync("Other");
        var openLoan = await BorrowAsync(otherBook, borrower);

        (await Loans.GetLoanAsync(new GetLoanRequest { Id = overdueLoan.Id })).Status.ShouldBe(LoanStatus.Overdue);

        var overdue = await Loans.ListLoansAsync(new ListLoansRequest { BorrowerId = borrower.Id, Status = LoanStatus.Overdue });
        overdue.Loans.Select(l => l.Id).ShouldBe([overdueLoan.Id]);

        var open = await Loans.ListLoansAsync(new ListLoansRequest { BorrowerId = borrower.Id, Status = LoanStatus.Open });
        open.Loans.Select(l => l.Id).ShouldBe([openLoan.Id]);

        var all = await Loans.ListLoansAsync(new ListLoansRequest { BookId = book.Id });
        all.Loans.Select(l => l.Id).ShouldBe([overdueLoan.Id, returnedLoan.Id]);
        all.TotalCount.ShouldBe(2);
    }
}

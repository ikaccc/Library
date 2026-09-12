using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.Lending.UnitTests.Domain;

public class LendingDeskTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly LoanPolicy Policy = new(loanPeriodDays: 14, maxOpenLoansPerBorrower: 2);

    private readonly Borrower _borrower = Borrower.Register("Ada Lovelace", null, Now);

    [Fact]
    public void Borrow_opens_a_loan_due_after_the_loan_period_and_takes_a_copy()
    {
        var book = NewBook(copies: 2);

        var result = LendingDesk.Borrow(book, _borrower, [], Policy, Now);

        result.IsSuccess.ShouldBeTrue();
        var loan = result.Value;
        loan.BookId.ShouldBe(book.Id);
        loan.BorrowerId.ShouldBe(_borrower.Id);
        loan.BorrowedAt.ShouldBe(Now);
        loan.DueAt.ShouldBe(Now.AddDays(14));
        loan.ReturnedAt.ShouldBeNull();
        loan.IsReturned.ShouldBeFalse();
        book.AvailableCopies.ShouldBe(1);
    }

    [Fact]
    public void Borrow_fails_when_no_copy_is_available_and_changes_nothing()
    {
        var book = NewBook(copies: 1);
        book.LendCopy();

        var result = LendingDesk.Borrow(book, _borrower, [], Policy, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("book.no_available_copies");
        result.Error.Type.ShouldBe(ErrorType.PreconditionFailed);
        book.AvailableCopies.ShouldBe(0);
    }

    [Fact]
    public void Borrow_fails_when_the_borrower_reached_the_open_loan_limit()
    {
        var book = NewBook(copies: 5);
        var openLoans = new[]
        {
            LendingDesk.Borrow(NewBook(1), _borrower, [], Policy, Now).Value,
            LendingDesk.Borrow(NewBook(1), _borrower, [], Policy, Now).Value,
        };

        var result = LendingDesk.Borrow(book, _borrower, openLoans, Policy, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("loan.open_loan_limit_reached");
        result.Error.Type.ShouldBe(ErrorType.PreconditionFailed);
        book.AvailableCopies.ShouldBe(5);
    }

    [Fact]
    public void Borrow_fails_when_the_borrower_already_has_the_same_book()
    {
        var book = NewBook(copies: 5);
        var existing = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;

        var result = LendingDesk.Borrow(book, _borrower, [existing], Policy, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("loan.book_already_on_loan_to_borrower");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        book.AvailableCopies.ShouldBe(4);
    }

    [Fact]
    public void Borrow_rejects_open_loans_that_belong_to_someone_else_or_are_closed()
    {
        var book = NewBook(copies: 5);
        var other = Borrower.Register("Grace Hopper", null, Now);
        var othersLoan = LendingDesk.Borrow(NewBook(1), other, [], Policy, Now).Value;

        Should.Throw<ArgumentException>(() => LendingDesk.Borrow(book, _borrower, [othersLoan], Policy, Now));

        var returnedBook = NewBook(1);
        var returnedLoan = LendingDesk.Borrow(returnedBook, _borrower, [], Policy, Now).Value;
        LendingDesk.Return(returnedLoan, returnedBook, Now.AddDays(1));

        Should.Throw<ArgumentException>(() => LendingDesk.Borrow(book, _borrower, [returnedLoan], Policy, Now));
    }

    [Fact]
    public void Return_closes_the_loan_and_puts_the_copy_back()
    {
        var book = NewBook(copies: 1);
        var loan = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;
        var returnedAt = Now.AddDays(3);

        var result = LendingDesk.Return(loan, book, returnedAt);

        result.IsSuccess.ShouldBeTrue();
        loan.IsReturned.ShouldBeTrue();
        loan.ReturnedAt.ShouldBe(returnedAt);
        book.AvailableCopies.ShouldBe(1);
    }

    [Fact]
    public void Return_twice_is_a_conflict_and_does_not_touch_the_inventory()
    {
        var book = NewBook(copies: 1);
        var loan = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;
        LendingDesk.Return(loan, book, Now.AddDays(3));

        var second = LendingDesk.Return(loan, book, Now.AddDays(4));

        second.IsFailure.ShouldBeTrue();
        second.Error!.Code.ShouldBe("loan.already_returned");
        second.Error.Type.ShouldBe(ErrorType.Conflict);
        loan.ReturnedAt.ShouldBe(Now.AddDays(3));
        book.AvailableCopies.ShouldBe(1);
    }

    [Fact]
    public void Return_with_the_wrong_book_is_a_programming_error()
    {
        var book = NewBook(copies: 1);
        var loan = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;

        Should.Throw<ArgumentException>(() => LendingDesk.Return(loan, NewBook(1), Now));
    }

    [Fact]
    public void Return_before_borrowing_is_a_programming_error()
    {
        var book = NewBook(copies: 1);
        var loan = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;

        Should.Throw<ArgumentOutOfRangeException>(() => LendingDesk.Return(loan, book, Now.AddSeconds(-1)));
    }

    [Fact]
    public void Loan_is_overdue_only_after_the_due_date_and_never_once_returned()
    {
        var book = NewBook(copies: 1);
        var loan = LendingDesk.Borrow(book, _borrower, [], Policy, Now).Value;

        loan.IsOverdueAt(loan.DueAt).ShouldBeFalse();
        loan.IsOverdueAt(loan.DueAt.AddSeconds(1)).ShouldBeTrue();

        LendingDesk.Return(loan, book, loan.DueAt.AddDays(2));
        loan.IsOverdueAt(loan.DueAt.AddDays(5)).ShouldBeFalse();
    }

    private static Book NewBook(int copies) => Book.Register("Title", "Author", null, 300, copies, Now);
}

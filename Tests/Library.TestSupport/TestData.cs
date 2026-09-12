using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Loans;

namespace Library.TestSupport;

public static class TestData
{
    public static readonly DateTimeOffset Anchor = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    public static Book NewBook(string title = "Sample Title", string author = "Sample Author", int pageCount = 300, int copies = 5, string? isbn = null) =>
        Book.Register(title, author, isbn is null ? null : Isbn.Create(isbn).Value, pageCount, copies, Anchor.AddYears(-1));

    public static Borrower NewBorrower(string fullName = "Sample Borrower", string? email = null) =>
        Borrower.Register(fullName, email, Anchor.AddYears(-1));

    public static Loan Borrow(Book book, Borrower borrower, DateTimeOffset at, LoanPolicy? policy = null)
    {
        var result = LendingDesk.Borrow(book, borrower, [], policy ?? LoanPolicy.Default, at);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error!.Message);
    }

    public static Loan BorrowAndReturn(Book book, Borrower borrower, DateTimeOffset at, double daysOnLoan, LoanPolicy? policy = null)
    {
        var loan = Borrow(book, borrower, at, policy);
        var returned = LendingDesk.Return(loan, book, at.AddDays(daysOnLoan));
        return returned.IsSuccess ? loan : throw new InvalidOperationException(returned.Error!.Message);
    }
}

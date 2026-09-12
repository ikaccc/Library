using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Loans;

public static class LendingDesk
{
    public static Result<Loan> Borrow(
        Book book,
        Borrower borrower,
        IReadOnlyCollection<Loan> borrowerOpenLoans,
        LoanPolicy policy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(borrower);
        ArgumentNullException.ThrowIfNull(borrowerOpenLoans);
        ArgumentNullException.ThrowIfNull(policy);

        if (borrowerOpenLoans.Any(loan => loan.IsReturned || loan.BorrowerId != borrower.Id))
        {
            throw new ArgumentException("Only the borrower's own open loans are expected.", nameof(borrowerOpenLoans));
        }

        if (borrowerOpenLoans.Count >= policy.MaxOpenLoansPerBorrower)
        {
            return LoanErrors.OpenLoanLimitReached(borrower.Id, policy.MaxOpenLoansPerBorrower);
        }

        if (borrowerOpenLoans.Any(loan => loan.BookId == book.Id))
        {
            return LoanErrors.BookAlreadyOnLoanToBorrower(book.Id, borrower.Id);
        }

        var lent = book.LendCopy();
        if (lent.IsFailure)
        {
            return lent.Error!;
        }

        return Loan.Open(book.Id, borrower.Id, now, policy);
    }

    public static Result Return(Loan loan, Book book, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(book);

        if (loan.BookId != book.Id)
        {
            throw new ArgumentException($"Loan '{loan.Id}' is for book '{loan.BookId}', not '{book.Id}'.", nameof(book));
        }

        var returned = loan.MarkReturned(now);
        if (returned.IsFailure)
        {
            return returned;
        }

        book.ReturnCopy();
        return Result.Success();
    }
}

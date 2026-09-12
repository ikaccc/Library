using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Loans;

public static class LoanErrors
{
    public static Error NotFound(Guid loanId) =>
        Error.NotFound("loan.not_found", $"Loan '{loanId}' was not found.");

    public static Error AlreadyReturned(Guid loanId, DateTimeOffset returnedAt) =>
        Error.Conflict("loan.already_returned", $"Loan '{loanId}' was already returned at {returnedAt:O}.");

    public static Error OpenLoanLimitReached(Guid borrowerId, int limit) =>
        Error.PreconditionFailed("loan.open_loan_limit_reached", $"Borrower '{borrowerId}' already has {limit} books on loan, which is the maximum.");

    public static Error BookAlreadyOnLoanToBorrower(Guid bookId, Guid borrowerId) =>
        Error.Conflict("loan.book_already_on_loan_to_borrower", $"Borrower '{borrowerId}' already has book '{bookId}' on loan.");
}

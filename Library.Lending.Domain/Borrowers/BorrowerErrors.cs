using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Borrowers;

public static class BorrowerErrors
{
    public static Error NotFound(Guid borrowerId) =>
        Error.NotFound("borrower.not_found", $"Borrower '{borrowerId}' was not found.");

    public static Error HasLoans(Guid borrowerId, string fullName) =>
        Error.Conflict("borrower.has_loans", $"'{fullName}' ({borrowerId}) has lending history and cannot be deleted.");

}

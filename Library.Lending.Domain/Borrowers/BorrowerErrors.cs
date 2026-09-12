using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Borrowers;

public static class BorrowerErrors
{
    public static Error NotFound(Guid borrowerId) =>
        Error.NotFound("borrower.not_found", $"Borrower '{borrowerId}' was not found.");
}

using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Books;

public static class BookErrors
{
    public static Error NotFound(Guid bookId) =>
        Error.NotFound("book.not_found", $"Book '{bookId}' was not found.");

    public static Error NoAvailableCopies(Guid bookId, string title) =>
        Error.PreconditionFailed("book.no_available_copies", $"All copies of '{title}' ({bookId}) are currently on loan.");

    public static Error DuplicateIsbn(Isbn isbn) =>
        Error.Conflict("book.duplicate_isbn", $"A book with ISBN {isbn} is already registered.");

    public static Error TotalCopiesBelowCopiesOnLoan(Guid bookId, string title, int copiesOnLoan, int requestedTotal) =>
        Error.PreconditionFailed(
            "book.total_copies_below_copies_on_loan",
            $"'{title}' ({bookId}) has {copiesOnLoan} copies on loan, so the inventory cannot be reduced to {requestedTotal}.");

    public static Error HasLoans(Guid bookId, string title) =>
        Error.Conflict("book.has_loans", $"'{title}' ({bookId}) has lending history and cannot be deleted.");

}

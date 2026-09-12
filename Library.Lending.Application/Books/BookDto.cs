using Library.Lending.Domain.Books;

namespace Library.Lending.Application.Books;

public sealed record BookDto(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int PageCount,
    int TotalCopies,
    int AvailableCopies,
    DateTimeOffset RegisteredAt);

internal static class BookMappings
{
    public static BookDto ToDto(this Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn?.Value,
        book.PageCount,
        book.TotalCopies,
        book.AvailableCopies,
        book.RegisteredAt);
}

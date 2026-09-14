namespace Library.Api.Contracts;

public sealed record RegisterBookRequest(string Title, string Author, string? Isbn, int PageCount, int TotalCopies);

public sealed record UpdateBookRequest(string Title, string Author, string? Isbn, int PageCount, int TotalCopies);

public sealed record RegisterBorrowerRequest(string FullName, string? Email);

public sealed record UpdateBorrowerRequest(string FullName, string? Email);

public sealed record BorrowBookRequest(Guid BookId, Guid BorrowerId);

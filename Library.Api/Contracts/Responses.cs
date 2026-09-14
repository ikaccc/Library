namespace Library.Api.Contracts;

public enum LoanStatus
{
    Open,
    Overdue,
    Returned,
}

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int PageCount,
    int TotalCopies,
    int AvailableCopies,
    DateTimeOffset RegisteredAt);

public sealed record BorrowerResponse(Guid Id, string FullName, string? Email, DateTimeOffset JoinedAt);

public sealed record LoanResponse(
    Guid Id,
    Guid BookId,
    Guid BorrowerId,
    DateTimeOffset BorrowedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    LoanStatus Status);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record MostBorrowedBookResponse(Guid BookId, string Title, string Author, int BorrowCount, int UniqueBorrowerCount);

public sealed record TopBorrowerResponse(Guid BorrowerId, string FullName, int LoanCount, int UniqueBookCount);

public sealed record LoanReadingPaceResponse(
    Guid LoanId,
    Guid BookId,
    string Title,
    int PageCount,
    DateTimeOffset BorrowedAt,
    DateTimeOffset ReturnedAt,
    double Days,
    double PagesPerDay);

public sealed record ReadingPaceResponse(
    Guid BorrowerId,
    string FullName,
    int LoansConsidered,
    double? PagesPerDay,
    IReadOnlyList<LoanReadingPaceResponse> Loans);

public sealed record AlsoBorrowedBookResponse(Guid BookId, string Title, string Author, int CoBorrowerCount, int LoanCount);

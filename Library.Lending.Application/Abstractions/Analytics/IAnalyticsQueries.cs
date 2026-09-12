using Library.Lending.Application.Common;

namespace Library.Lending.Application.Abstractions.Analytics;

public interface IAnalyticsQueries
{
    Task<IReadOnlyList<MostBorrowedBook>> GetMostBorrowedBooksAsync(TimeRange range, int top, CancellationToken cancellationToken);

    Task<IReadOnlyList<TopBorrower>> GetTopBorrowersAsync(TimeRange range, int top, CancellationToken cancellationToken);

    Task<IReadOnlyList<CompletedLoan>> GetCompletedLoansForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AlsoBorrowedBook>> GetAlsoBorrowedBooksAsync(Guid bookId, int top, CancellationToken cancellationToken);
}

public sealed record MostBorrowedBook(Guid BookId, string Title, string Author, int BorrowCount, int UniqueBorrowerCount);

public sealed record TopBorrower(Guid BorrowerId, string FullName, int LoanCount, int UniqueBookCount);

public sealed record CompletedLoan(Guid LoanId, Guid BookId, string Title, int PageCount, DateTimeOffset BorrowedAt, DateTimeOffset ReturnedAt);

public sealed record AlsoBorrowedBook(Guid BookId, string Title, string Author, int CoBorrowerCount, int LoanCount);

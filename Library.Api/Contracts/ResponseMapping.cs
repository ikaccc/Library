using V1 = Library.Lending.Contracts.V1;

namespace Library.Api.Contracts;

/// <summary>Wire messages of the Lending service to the JSON shapes this API promises its clients.</summary>
internal static class ResponseMapping
{
    public static BookResponse ToResponse(this V1.Book book) => new(
        Guid.Parse(book.Id),
        book.Title,
        book.Author,
        book.HasIsbn ? book.Isbn : null,
        book.PageCount,
        book.TotalCopies,
        book.AvailableCopies,
        book.RegisteredAt.ToDateTimeOffset());

    public static PagedResponse<BookResponse> ToResponse(this V1.ListBooksResponse response) =>
        new(response.Books.Select(ToResponse).ToList(), response.Page, response.PageSize, response.TotalCount);

    public static BorrowerResponse ToResponse(this V1.Borrower borrower) => new(
        Guid.Parse(borrower.Id),
        borrower.FullName,
        borrower.HasEmail ? borrower.Email : null,
        borrower.JoinedAt.ToDateTimeOffset());

    public static PagedResponse<BorrowerResponse> ToResponse(this V1.ListBorrowersResponse response) =>
        new(response.Borrowers.Select(ToResponse).ToList(), response.Page, response.PageSize, response.TotalCount);

    public static LoanResponse ToResponse(this V1.Loan loan) => new(
        Guid.Parse(loan.Id),
        Guid.Parse(loan.BookId),
        Guid.Parse(loan.BorrowerId),
        loan.BorrowedAt.ToDateTimeOffset(),
        loan.DueAt.ToDateTimeOffset(),
        loan.ReturnedAt?.ToDateTimeOffset(),
        loan.Status.ToResponse());

    public static PagedResponse<LoanResponse> ToResponse(this V1.ListLoansResponse response) =>
        new(response.Loans.Select(ToResponse).ToList(), response.Page, response.PageSize, response.TotalCount);

    public static LoanStatus ToResponse(this V1.LoanStatus status) => status switch
    {
        V1.LoanStatus.Open => LoanStatus.Open,
        V1.LoanStatus.Overdue => LoanStatus.Overdue,
        V1.LoanStatus.Returned => LoanStatus.Returned,
        _ => throw new InvalidOperationException($"The Lending service returned an unexpected loan status '{status}'."),
    };

    public static V1.LoanStatus ToProto(this LoanStatus? status) => status switch
    {
        null => V1.LoanStatus.Unspecified,
        LoanStatus.Open => V1.LoanStatus.Open,
        LoanStatus.Overdue => V1.LoanStatus.Overdue,
        LoanStatus.Returned => V1.LoanStatus.Returned,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown loan status."),
    };

    public static IReadOnlyList<MostBorrowedBookResponse> ToResponse(this V1.GetMostBorrowedBooksResponse response) =>
        response.Books
            .Select(book => new MostBorrowedBookResponse(Guid.Parse(book.BookId), book.Title, book.Author, book.BorrowCount, book.UniqueBorrowerCount))
            .ToList();

    public static IReadOnlyList<TopBorrowerResponse> ToResponse(this V1.GetTopBorrowersResponse response) =>
        response.Borrowers
            .Select(borrower => new TopBorrowerResponse(Guid.Parse(borrower.BorrowerId), borrower.FullName, borrower.LoanCount, borrower.UniqueBookCount))
            .ToList();

    public static ReadingPaceResponse ToResponse(this V1.GetReadingPaceResponse response) => new(
        Guid.Parse(response.BorrowerId),
        response.FullName,
        response.LoansConsidered,
        response.HasPagesPerDay ? response.PagesPerDay : null,
        response.Loans.Select(loan => new LoanReadingPaceResponse(
            Guid.Parse(loan.LoanId),
            Guid.Parse(loan.BookId),
            loan.Title,
            loan.PageCount,
            loan.BorrowedAt.ToDateTimeOffset(),
            loan.ReturnedAt.ToDateTimeOffset(),
            loan.Days,
            loan.PagesPerDay)).ToList());

    public static IReadOnlyList<AlsoBorrowedBookResponse> ToResponse(this V1.GetAlsoBorrowedBooksResponse response) =>
        response.Books
            .Select(book => new AlsoBorrowedBookResponse(Guid.Parse(book.BookId), book.Title, book.Author, book.CoBorrowerCount, book.LoanCount))
            .ToList();
}

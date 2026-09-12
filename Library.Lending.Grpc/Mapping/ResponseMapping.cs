using Google.Protobuf.WellKnownTypes;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Analytics;
using Library.Lending.Application.Books;
using Library.Lending.Application.Borrowers;
using Library.Lending.Application.Common;
using Library.Lending.Application.Loans;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Mapping;

internal static class ResponseMapping
{
    public static V1.Book ToProto(this BookDto book)
    {
        var message = new V1.Book
        {
            Id = book.Id.ToString(),
            Title = book.Title,
            Author = book.Author,
            PageCount = book.PageCount,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            RegisteredAt = Timestamp.FromDateTimeOffset(book.RegisteredAt),
        };

        if (book.Isbn is not null)
        {
            message.Isbn = book.Isbn;
        }

        return message;
    }

    public static V1.ListBooksResponse ToProto(this PagedResult<BookDto> page)
    {
        var response = new V1.ListBooksResponse { Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount };
        response.Books.AddRange(page.Items.Select(ToProto));
        return response;
    }

    public static V1.Borrower ToProto(this BorrowerDto borrower)
    {
        var message = new V1.Borrower
        {
            Id = borrower.Id.ToString(),
            FullName = borrower.FullName,
            JoinedAt = Timestamp.FromDateTimeOffset(borrower.JoinedAt),
        };

        if (borrower.Email is not null)
        {
            message.Email = borrower.Email;
        }

        return message;
    }

    public static V1.ListBorrowersResponse ToProto(this PagedResult<BorrowerDto> page)
    {
        var response = new V1.ListBorrowersResponse { Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount };
        response.Borrowers.AddRange(page.Items.Select(ToProto));
        return response;
    }

    public static V1.Loan ToProto(this LoanDto loan) => new()
    {
        Id = loan.Id.ToString(),
        BookId = loan.BookId.ToString(),
        BorrowerId = loan.BorrowerId.ToString(),
        BorrowedAt = Timestamp.FromDateTimeOffset(loan.BorrowedAt),
        DueAt = Timestamp.FromDateTimeOffset(loan.DueAt),
        ReturnedAt = loan.ReturnedAt is { } returnedAt ? Timestamp.FromDateTimeOffset(returnedAt) : null,
        Status = loan.Status.ToProto(),
    };

    public static V1.ListLoansResponse ToProto(this PagedResult<LoanDto> page)
    {
        var response = new V1.ListLoansResponse { Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount };
        response.Loans.AddRange(page.Items.Select(ToProto));
        return response;
    }

    public static V1.LoanStatus ToProto(this LoanStatus status) => status switch
    {
        LoanStatus.Open => V1.LoanStatus.Open,
        LoanStatus.Overdue => V1.LoanStatus.Overdue,
        LoanStatus.Returned => V1.LoanStatus.Returned,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown loan status."),
    };

    public static V1.GetMostBorrowedBooksResponse ToProto(this IReadOnlyList<MostBorrowedBook> books)
    {
        var response = new V1.GetMostBorrowedBooksResponse();
        response.Books.AddRange(books.Select(book => new V1.MostBorrowedBook
        {
            BookId = book.BookId.ToString(),
            Title = book.Title,
            Author = book.Author,
            BorrowCount = book.BorrowCount,
            UniqueBorrowerCount = book.UniqueBorrowerCount,
        }));
        return response;
    }

    public static V1.GetTopBorrowersResponse ToProto(this IReadOnlyList<TopBorrower> borrowers)
    {
        var response = new V1.GetTopBorrowersResponse();
        response.Borrowers.AddRange(borrowers.Select(borrower => new V1.TopBorrower
        {
            BorrowerId = borrower.BorrowerId.ToString(),
            FullName = borrower.FullName,
            LoanCount = borrower.LoanCount,
            UniqueBookCount = borrower.UniqueBookCount,
        }));
        return response;
    }

    public static V1.GetReadingPaceResponse ToProto(this ReadingPaceReport report)
    {
        var response = new V1.GetReadingPaceResponse
        {
            BorrowerId = report.BorrowerId.ToString(),
            FullName = report.FullName,
            LoansConsidered = report.LoansConsidered,
        };

        if (report.PagesPerDay is { } pagesPerDay)
        {
            response.PagesPerDay = pagesPerDay;
        }

        response.Loans.AddRange(report.Loans.Select(loan => new V1.LoanReadingPace
        {
            LoanId = loan.LoanId.ToString(),
            BookId = loan.BookId.ToString(),
            Title = loan.Title,
            PageCount = loan.PageCount,
            BorrowedAt = Timestamp.FromDateTimeOffset(loan.BorrowedAt),
            ReturnedAt = Timestamp.FromDateTimeOffset(loan.ReturnedAt),
            Days = loan.Days,
            PagesPerDay = loan.PagesPerDay,
        }));

        return response;
    }

    public static V1.GetAlsoBorrowedBooksResponse ToProto(this IReadOnlyList<AlsoBorrowedBook> books)
    {
        var response = new V1.GetAlsoBorrowedBooksResponse();
        response.Books.AddRange(books.Select(book => new V1.AlsoBorrowedBook
        {
            BookId = book.BookId.ToString(),
            Title = book.Title,
            Author = book.Author,
            CoBorrowerCount = book.CoBorrowerCount,
            LoanCount = book.LoanCount,
        }));
        return response;
    }
}

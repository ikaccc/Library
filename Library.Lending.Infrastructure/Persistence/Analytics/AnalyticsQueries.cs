using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence.Analytics;

internal sealed class AnalyticsQueries(LendingDbContext db) : IAnalyticsQueries
{
    public async Task<IReadOnlyList<MostBorrowedBook>> GetMostBorrowedBooksAsync(TimeRange range, int top, CancellationToken cancellationToken)
    {
        var ranking = ApplyRange(db.Loans.AsNoTracking(), range)
            .GroupBy(loan => loan.BookId)
            .Select(group => new
            {
                BookId = group.Key,
                BorrowCount = group.Count(),
                UniqueBorrowerCount = group.Select(loan => loan.BorrowerId).Distinct().Count(),
            });

        var rows = await ranking
            .Join(
                db.Books.AsNoTracking(),
                rank => rank.BookId,
                book => book.Id,
                (rank, book) => new { book.Id, book.Title, book.Author, rank.BorrowCount, rank.UniqueBorrowerCount })
            .OrderByDescending(row => row.BorrowCount)
            .ThenBy(row => row.Title)
            .ThenBy(row => row.Id)
            .Take(top)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new MostBorrowedBook(row.Id, row.Title, row.Author, row.BorrowCount, row.UniqueBorrowerCount))
            .ToList();
    }

    public async Task<IReadOnlyList<TopBorrower>> GetTopBorrowersAsync(TimeRange range, int top, CancellationToken cancellationToken)
    {
        var ranking = ApplyRange(db.Loans.AsNoTracking(), range)
            .GroupBy(loan => loan.BorrowerId)
            .Select(group => new
            {
                BorrowerId = group.Key,
                LoanCount = group.Count(),
                UniqueBookCount = group.Select(loan => loan.BookId).Distinct().Count(),
            });

        var rows = await ranking
            .Join(
                db.Borrowers.AsNoTracking(),
                rank => rank.BorrowerId,
                borrower => borrower.Id,
                (rank, borrower) => new { borrower.Id, borrower.FullName, rank.LoanCount, rank.UniqueBookCount })
            .OrderByDescending(row => row.LoanCount)
            .ThenBy(row => row.FullName)
            .ThenBy(row => row.Id)
            .Take(top)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new TopBorrower(row.Id, row.FullName, row.LoanCount, row.UniqueBookCount))
            .ToList();
    }

    public async Task<IReadOnlyList<CompletedLoan>> GetCompletedLoansForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken)
    {
        var rows = await db.Loans.AsNoTracking()
            .Where(loan => loan.BorrowerId == borrowerId && loan.ReturnedAt != null)
            .Join(
                db.Books.AsNoTracking(),
                loan => loan.BookId,
                book => book.Id,
                (loan, book) => new { loan.Id, loan.BookId, book.Title, book.PageCount, loan.BorrowedAt, ReturnedAt = loan.ReturnedAt!.Value })
            .OrderBy(row => row.ReturnedAt)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CompletedLoan(row.Id, row.BookId, row.Title, row.PageCount, row.BorrowedAt, row.ReturnedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AlsoBorrowedBook>> GetAlsoBorrowedBooksAsync(Guid bookId, int top, CancellationToken cancellationToken)
    {
        var loans = db.Loans.AsNoTracking();

        var coBorrowers = loans
            .Where(loan => loan.BookId == bookId)
            .Select(loan => loan.BorrowerId)
            .Distinct();

        var ranking = loans
            .Where(loan => loan.BookId != bookId && coBorrowers.Contains(loan.BorrowerId))
            .GroupBy(loan => loan.BookId)
            .Select(group => new
            {
                BookId = group.Key,
                CoBorrowerCount = group.Select(loan => loan.BorrowerId).Distinct().Count(),
                LoanCount = group.Count(),
            });

        var rows = await ranking
            .Join(
                db.Books.AsNoTracking(),
                rank => rank.BookId,
                book => book.Id,
                (rank, book) => new { book.Id, book.Title, book.Author, rank.CoBorrowerCount, rank.LoanCount })
            .OrderByDescending(row => row.CoBorrowerCount)
            .ThenByDescending(row => row.LoanCount)
            .ThenBy(row => row.Title)
            .ThenBy(row => row.Id)
            .Take(top)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new AlsoBorrowedBook(row.Id, row.Title, row.Author, row.CoBorrowerCount, row.LoanCount))
            .ToList();
    }

    private static IQueryable<Loan> ApplyRange(IQueryable<Loan> loans, TimeRange range)
    {
        if (range.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            loans = loans.Where(loan => loan.BorrowedAt >= fromUtc);
        }

        if (range.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            loans = loans.Where(loan => loan.BorrowedAt < toUtc);
        }

        return loans;
    }
}

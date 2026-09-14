using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence.Repositories;

internal sealed class LoanRepository(LendingDbContext db) : ILoanRepository
{
    public Task<Loan?> GetByIdAsync(Guid loanId, CancellationToken cancellationToken) =>
        db.Loans.FirstOrDefaultAsync(loan => loan.Id == loanId, cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetOpenLoansForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken) =>
        await db.Loans.AsNoTracking()
            .Where(loan => loan.BorrowerId == borrowerId && loan.ReturnedAt == null)
            .OrderBy(loan => loan.BorrowedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        db.Loans.AnyAsync(loan => loan.BookId == bookId, cancellationToken);

    public Task<bool> ExistsForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken) =>
        db.Loans.AnyAsync(loan => loan.BorrowerId == borrowerId, cancellationToken);


    public Task<PagedResult<Loan>> ListAsync(LoanFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Loan> query = db.Loans.AsNoTracking();

        if (filter.BookId is { } bookId)
        {
            query = query.Where(loan => loan.BookId == bookId);
        }

        if (filter.BorrowerId is { } borrowerId)
        {
            query = query.Where(loan => loan.BorrowerId == borrowerId);
        }

        var now = filter.Now.ToUniversalTime();
        query = filter.Status switch
        {
            LoanStatus.Open => query.Where(loan => loan.ReturnedAt == null && loan.DueAt >= now),
            LoanStatus.Overdue => query.Where(loan => loan.ReturnedAt == null && loan.DueAt < now),
            LoanStatus.Returned => query.Where(loan => loan.ReturnedAt != null),
            _ => query,
        };

        return query
            .OrderByDescending(loan => loan.BorrowedAt)
            .ThenByDescending(loan => loan.Id)
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public void Add(Loan loan) => db.Loans.Add(loan);
}

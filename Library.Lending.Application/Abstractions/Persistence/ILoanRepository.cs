using Library.Lending.Application.Common;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Abstractions.Persistence;

public sealed record LoanFilter(Guid? BookId, Guid? BorrowerId, LoanStatus? Status, DateTimeOffset Now);

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid loanId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Loan>> GetOpenLoansForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task<PagedResult<Loan>> ListAsync(LoanFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    void Add(Loan loan);

    Task<bool> ExistsForBookAsync(Guid bookId, CancellationToken cancellationToken);

    Task<bool> ExistsForBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken);
}


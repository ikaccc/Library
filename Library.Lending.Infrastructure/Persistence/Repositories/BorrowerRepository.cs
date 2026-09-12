using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Borrowers;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence.Repositories;

internal sealed class BorrowerRepository(LendingDbContext db) : IBorrowerRepository
{
    public Task<Borrower?> GetByIdAsync(Guid borrowerId, CancellationToken cancellationToken) =>
        db.Borrowers.FirstOrDefaultAsync(borrower => borrower.Id == borrowerId, cancellationToken);

    public Task<bool> ExistsAsync(Guid borrowerId, CancellationToken cancellationToken) =>
        db.Borrowers.AnyAsync(borrower => borrower.Id == borrowerId, cancellationToken);

    public Task<PagedResult<Borrower>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        db.Borrowers.AsNoTracking()
            .OrderBy(borrower => borrower.FullName)
            .ThenBy(borrower => borrower.Id)
            .ToPagedResultAsync(page, pageSize, cancellationToken);

    public void Add(Borrower borrower) => db.Borrowers.Add(borrower);
}

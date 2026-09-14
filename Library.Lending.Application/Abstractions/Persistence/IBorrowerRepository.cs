using Library.Lending.Application.Common;
using Library.Lending.Domain.Borrowers;

namespace Library.Lending.Application.Abstractions.Persistence;

public interface IBorrowerRepository
{
    Task<Borrower?> GetByIdAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task<PagedResult<Borrower>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    void Add(Borrower borrower);

    void Remove(Borrower borrower);
}

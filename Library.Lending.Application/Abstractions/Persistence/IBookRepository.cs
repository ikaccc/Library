using Library.Lending.Application.Common;
using Library.Lending.Domain.Books;

namespace Library.Lending.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid bookId, CancellationToken cancellationToken);

    Task<bool> ExistsWithIsbnAsync(Isbn isbn, CancellationToken cancellationToken);

    Task<PagedResult<Book>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    void Add(Book book);

    Task<bool> ExistsWithIsbnAsync(Isbn isbn, Guid? excludingBookId, CancellationToken cancellationToken);

    void Remove(Book book);
}
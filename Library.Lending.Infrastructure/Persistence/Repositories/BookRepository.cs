using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence.Repositories;

internal sealed class BookRepository(LendingDbContext db) : IBookRepository
{
    public Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken) =>
        db.Books.FirstOrDefaultAsync(book => book.Id == bookId, cancellationToken);

    public Task<bool> ExistsAsync(Guid bookId, CancellationToken cancellationToken) =>
        db.Books.AnyAsync(book => book.Id == bookId, cancellationToken);

    public Task<bool> ExistsWithIsbnAsync(Isbn isbn, Guid? excludingBookId, CancellationToken cancellationToken) =>
        db.Books.AnyAsync(book => book.Isbn == isbn && (excludingBookId == null || book.Id != excludingBookId), cancellationToken);


    public Task<bool> ExistsWithIsbnAsync(Isbn isbn, CancellationToken cancellationToken) =>
        db.Books.AnyAsync(book => book.Isbn == isbn, cancellationToken);

    public Task<PagedResult<Book>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Book> query = db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePatterns.Contains(search.Trim());
            query = query.Where(book =>
                EF.Functions.ILike(book.Title, pattern, LikePatterns.EscapeCharacter) ||
                EF.Functions.ILike(book.Author, pattern, LikePatterns.EscapeCharacter));
        }

        return query
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public void Add(Book book) => db.Books.Add(book);
    public void Remove(Book book) => db.Books.Remove(book);
}

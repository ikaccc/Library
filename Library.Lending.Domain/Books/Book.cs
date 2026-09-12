using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Books;

public sealed class Book : Entity
{
    public const int MaxTitleLength = 200;
    public const int MaxAuthorLength = 200;

    private Book()
    {
        // Materialized by EF Core.
    }

    private Book(string title, string author, Isbn? isbn, int pageCount, int totalCopies, DateTimeOffset registeredAt)
        : base(Guid.CreateVersion7())
    {
        Title = title;
        Author = author;
        Isbn = isbn;
        PageCount = pageCount;
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
        RegisteredAt = registeredAt;
    }


    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public Isbn? Isbn { get; private set; }

    public int PageCount { get; private set; }

    public int TotalCopies { get; private set; }

    public int AvailableCopies { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public int CopiesOnLoan => TotalCopies - AvailableCopies;

    public static Book Register(string title, string author, Isbn? isbn, int pageCount, int totalCopies, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(title.Length, MaxTitleLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(author.Length, MaxAuthorLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalCopies);

        return new Book(title.Trim(), author.Trim(), isbn, pageCount, totalCopies, now);
    }

    public Result LendCopy()
    {
        if (AvailableCopies == 0)
        {
            return BookErrors.NoAvailableCopies(Id, Title);
        }

        AvailableCopies--;
        return Result.Success();
    }

    public void ReturnCopy()
    {
        if (AvailableCopies >= TotalCopies)
        {
            throw new InvalidOperationException($"Book '{Title}' ({Id}) already has all {TotalCopies} copies on the shelf.");
        }

        AvailableCopies++;
    }
}

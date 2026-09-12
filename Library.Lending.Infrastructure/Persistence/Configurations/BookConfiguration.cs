using Library.Lending.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Lending.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books", table =>
        {
            table.HasCheckConstraint("ck_books_page_count_positive", "page_count > 0");
            table.HasCheckConstraint("ck_books_total_copies_positive", "total_copies > 0");
            table.HasCheckConstraint("ck_books_available_copies_in_range", "available_copies >= 0 AND available_copies <= total_copies");
        });

        builder.HasKey(book => book.Id);
        builder.Property(book => book.Id).ValueGeneratedNever();

        builder.Property(book => book.Title).HasMaxLength(Book.MaxTitleLength).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(Book.MaxAuthorLength).IsRequired();

        builder.Property(book => book.Isbn)
            .HasConversion(isbn => isbn!.Value, value => Isbn.FromTrusted(value))
            .HasMaxLength(13);
        builder.HasIndex(book => book.Isbn).IsUnique();

        builder.Property(book => book.PageCount);
        builder.Property(book => book.TotalCopies);
        builder.Property(book => book.AvailableCopies);
        builder.Property(book => book.RegisteredAt);

        builder.HasIndex(book => book.Title);

        // PostgreSQL's xmin system column as the optimistic concurrency token.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Ignore(book => book.CopiesOnLoan);
    }
}

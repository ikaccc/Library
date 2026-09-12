using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Lending.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans", table =>
        {
            table.HasCheckConstraint("ck_loans_due_after_borrowed", "due_at > borrowed_at");
            table.HasCheckConstraint("ck_loans_returned_after_borrowed", "returned_at IS NULL OR returned_at >= borrowed_at");
        });

        builder.HasKey(loan => loan.Id);
        builder.Property(loan => loan.Id).ValueGeneratedNever();

        builder.Property(loan => loan.BookId);
        builder.Property(loan => loan.BorrowerId);
        builder.Property(loan => loan.BorrowedAt);
        builder.Property(loan => loan.DueAt);
        builder.Property(loan => loan.ReturnedAt);

        // The domain model deliberately has no navigation properties.
        builder.HasOne<Book>().WithMany().HasForeignKey(loan => loan.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Borrower>().WithMany().HasForeignKey(loan => loan.BorrowerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(loan => loan.BookId);
        builder.HasIndex(loan => loan.BorrowedAt);
        builder.HasIndex(loan => loan.ReturnedAt);
        builder.HasIndex(loan => loan.BorrowerId)
            .HasDatabaseName("ix_loans_borrower_id_open")
            .HasFilter("returned_at IS NULL");
        builder.HasIndex(loan => new { loan.BorrowerId, loan.ReturnedAt });

        builder.Ignore(loan => loan.IsReturned);
    }
}

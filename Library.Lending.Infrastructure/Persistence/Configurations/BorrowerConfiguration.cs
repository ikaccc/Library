using Library.Lending.Domain.Borrowers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Lending.Infrastructure.Persistence.Configurations;

internal sealed class BorrowerConfiguration : IEntityTypeConfiguration<Borrower>
{
    public void Configure(EntityTypeBuilder<Borrower> builder)
    {
        builder.ToTable("borrowers");

        builder.HasKey(borrower => borrower.Id);
        builder.Property(borrower => borrower.Id).ValueGeneratedNever();

        builder.Property(borrower => borrower.FullName).HasMaxLength(Borrower.MaxFullNameLength).IsRequired();
        builder.Property(borrower => borrower.Email).HasMaxLength(Borrower.MaxEmailLength);
        builder.Property(borrower => borrower.JoinedAt);

        builder.HasIndex(borrower => borrower.FullName);
    }
}

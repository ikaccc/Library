using Library.Lending.Domain.Borrowers;

namespace Library.Lending.Application.Borrowers;

public sealed record BorrowerDto(Guid Id, string FullName, string? Email, DateTimeOffset JoinedAt);

internal static class BorrowerMappings
{
    public static BorrowerDto ToDto(this Borrower borrower) =>
        new(borrower.Id, borrower.FullName, borrower.Email, borrower.JoinedAt);
}

using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Loans;

public enum LoanStatus
{
    Open,
    Overdue,
    Returned,
}

public sealed record LoanDto(
    Guid Id,
    Guid BookId,
    Guid BorrowerId,
    DateTimeOffset BorrowedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    LoanStatus Status);

internal static class LoanMappings
{
    public static LoanDto ToDto(this Loan loan, DateTimeOffset now) => new(
        loan.Id,
        loan.BookId,
        loan.BorrowerId,
        loan.BorrowedAt,
        loan.DueAt,
        loan.ReturnedAt,
        loan.StatusAt(now));

    public static LoanStatus StatusAt(this Loan loan, DateTimeOffset now) => loan switch
    {
        { IsReturned: true } => LoanStatus.Returned,
        _ when loan.IsOverdueAt(now) => LoanStatus.Overdue,
        _ => LoanStatus.Open,
    };
}

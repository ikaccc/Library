using Library.Lending.Domain.Loans;

namespace Library.Lending.Application;

public sealed class LendingOptions
{
    public const string SectionName = "Lending";

    public int LoanPeriodDays { get; set; } = LoanPolicy.Default.LoanPeriodDays;

    public int MaxOpenLoansPerBorrower { get; set; } = LoanPolicy.Default.MaxOpenLoansPerBorrower;

    public LoanPolicy ToPolicy() => new(LoanPeriodDays, MaxOpenLoansPerBorrower);
}

namespace Library.Lending.Domain.Loans;

public sealed record LoanPolicy
{
    public static LoanPolicy Default { get; } = new(loanPeriodDays: 14, maxOpenLoansPerBorrower: 5);

    public LoanPolicy(int loanPeriodDays, int maxOpenLoansPerBorrower)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loanPeriodDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOpenLoansPerBorrower);

        LoanPeriodDays = loanPeriodDays;
        MaxOpenLoansPerBorrower = maxOpenLoansPerBorrower;
    }

    public int LoanPeriodDays { get; }

    public int MaxOpenLoansPerBorrower { get; }

    public DateTimeOffset DueDateFor(DateTimeOffset borrowedAt) => borrowedAt.AddDays(LoanPeriodDays);
}

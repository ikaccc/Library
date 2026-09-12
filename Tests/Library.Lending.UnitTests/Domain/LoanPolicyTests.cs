using Library.Lending.Domain.Loans;

namespace Library.Lending.UnitTests.Domain;

public class LoanPolicyTests
{
    [Fact]
    public void Default_policy_is_fourteen_days_and_five_open_loans()
    {
        LoanPolicy.Default.LoanPeriodDays.ShouldBe(14);
        LoanPolicy.Default.MaxOpenLoansPerBorrower.ShouldBe(5);
    }

    [Fact]
    public void DueDateFor_adds_the_loan_period()
    {
        var policy = new LoanPolicy(loanPeriodDays: 21, maxOpenLoansPerBorrower: 3);
        var borrowedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        policy.DueDateFor(borrowedAt).ShouldBe(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(14, 0)]
    public void Rejects_non_positive_settings(int days, int maxOpen)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new LoanPolicy(days, maxOpen));
    }
}

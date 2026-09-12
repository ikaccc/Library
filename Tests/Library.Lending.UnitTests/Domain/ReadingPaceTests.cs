using Library.Lending.Domain.Loans;

namespace Library.Lending.UnitTests.Domain;

public class ReadingPaceTests
{
    private static readonly DateTimeOffset BorrowedAt = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pace_is_pages_divided_by_days_on_loan()
    {
        var pace = ReadingPace.ForLoan(pageCount: 300, BorrowedAt, BorrowedAt.AddDays(10));

        pace.Days.ShouldBe(10d);
        pace.PagesPerDay.ShouldBe(30d);
    }

    [Fact]
    public void Fractional_days_are_kept()
    {
        var pace = ReadingPace.ForLoan(pageCount: 300, BorrowedAt, BorrowedAt.AddDays(2.5));

        pace.Days.ShouldBe(2.5d);
        pace.PagesPerDay.ShouldBe(120d);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(23)]
    public void Loans_shorter_than_a_day_count_as_one_day(int hours)
    {
        var pace = ReadingPace.ForLoan(pageCount: 240, BorrowedAt, BorrowedAt.AddHours(hours));

        pace.Days.ShouldBe(ReadingPace.MinimumDays);
        pace.PagesPerDay.ShouldBe(240d);
    }

    [Fact]
    public void Rejects_non_positive_page_count_and_return_before_borrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ReadingPace.ForLoan(0, BorrowedAt, BorrowedAt.AddDays(1)));
        Should.Throw<ArgumentOutOfRangeException>(() => ReadingPace.ForLoan(100, BorrowedAt, BorrowedAt.AddSeconds(-1)));
    }

    [Fact]
    public void Overall_pace_weights_by_days_not_by_loan()
    {
        // 300 pages in 10 days (30/day) and 100 pages in 1 day (100/day):
        // a plain average of paces would say 65/day, but the reader actually did 400 pages in 11 days.
        var paces = new[]
        {
            ReadingPace.ForLoan(300, BorrowedAt, BorrowedAt.AddDays(10)),
            ReadingPace.ForLoan(100, BorrowedAt, BorrowedAt.AddDays(1)),
        };

        var overall = ReadingPace.Overall(paces);

        overall.ShouldNotBeNull();
        overall.Value.ShouldBe(400d / 11d, tolerance: 1e-9);
    }

    [Fact]
    public void Overall_pace_is_null_without_completed_loans()
    {
        ReadingPace.Overall([]).ShouldBeNull();
    }
}

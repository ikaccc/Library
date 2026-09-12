using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Analytics;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class GetReadingPaceHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBorrowerRepository> _borrowers = new(MockBehavior.Strict);
    private readonly Mock<IAnalyticsQueries> _analytics = new(MockBehavior.Strict);
    private readonly Borrower _borrower = Borrower.Register("Ada Lovelace", null, Start);

    [Fact]
    public async Task Reports_weighted_overall_pace_and_per_loan_paces_newest_first()
    {
        var slowRead = new CompletedLoan(Guid.CreateVersion7(), Guid.CreateVersion7(), "Long Novel", 300, Start, Start.AddDays(10));
        var quickRead = new CompletedLoan(Guid.CreateVersion7(), Guid.CreateVersion7(), "Novella", 100, Start.AddDays(12), Start.AddDays(13));
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _analytics.Setup(a => a.GetCompletedLoansForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slowRead, quickRead]);

        var result = await CreateHandler().HandleAsync(new GetReadingPaceQuery(_borrower.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value;
        report.BorrowerId.ShouldBe(_borrower.Id);
        report.FullName.ShouldBe("Ada Lovelace");
        report.LoansConsidered.ShouldBe(2);
        report.PagesPerDay.ShouldBe(Math.Round(400d / 11d, 2)); // 36.36, not the naive average of 30 and 100
        report.Loans.Select(l => l.Title).ShouldBe(["Novella", "Long Novel"]);
        report.Loans[0].Days.ShouldBe(1d);
        report.Loans[0].PagesPerDay.ShouldBe(100d);
        report.Loans[1].Days.ShouldBe(10d);
        report.Loans[1].PagesPerDay.ShouldBe(30d);
    }

    [Fact]
    public async Task Reports_no_pace_when_the_borrower_has_not_returned_anything_yet()
    {
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _analytics.Setup(a => a.GetCompletedLoansForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateHandler().HandleAsync(new GetReadingPaceQuery(_borrower.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LoansConsidered.ShouldBe(0);
        result.Value.PagesPerDay.ShouldBeNull();
        result.Value.Loans.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fails_with_not_found_for_an_unknown_borrower()
    {
        var borrowerId = Guid.CreateVersion7();
        _borrowers.Setup(r => r.GetByIdAsync(borrowerId, It.IsAny<CancellationToken>())).ReturnsAsync((Borrower?)null);

        var result = await CreateHandler().HandleAsync(new GetReadingPaceQuery(borrowerId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    private GetReadingPaceHandler CreateHandler() => new(_borrowers.Object, _analytics.Object);
}

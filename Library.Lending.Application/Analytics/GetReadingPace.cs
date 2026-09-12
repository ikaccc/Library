using FluentValidation;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Analytics;

public sealed record GetReadingPaceQuery(Guid BorrowerId) : IQuery<Result<ReadingPaceReport>>;

public sealed record ReadingPaceReport(
    Guid BorrowerId,
    string FullName,
    int LoansConsidered,
    double? PagesPerDay,
    IReadOnlyList<LoanReadingPace> Loans);

public sealed record LoanReadingPace(
    Guid LoanId,
    Guid BookId,
    string Title,
    int PageCount,
    DateTimeOffset BorrowedAt,
    DateTimeOffset ReturnedAt,
    double Days,
    double PagesPerDay);

internal sealed class GetReadingPaceValidator : AbstractValidator<GetReadingPaceQuery>
{
    public GetReadingPaceValidator()
    {
        RuleFor(x => x.BorrowerId).NotEmpty();
    }
}

internal sealed class GetReadingPaceHandler(IBorrowerRepository borrowers, IAnalyticsQueries analytics)
    : IQueryHandler<GetReadingPaceQuery, Result<ReadingPaceReport>>
{
    private const int Precision = 2;

    public async Task<Result<ReadingPaceReport>> HandleAsync(GetReadingPaceQuery query, CancellationToken cancellationToken)
    {
        var borrower = await borrowers.GetByIdAsync(query.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return BorrowerErrors.NotFound(query.BorrowerId);
        }

        var completedLoans = await analytics.GetCompletedLoansForBorrowerAsync(borrower.Id, cancellationToken);

        var paces = completedLoans
            .Select(loan => (Loan: loan, Pace: ReadingPace.ForLoan(loan.PageCount, loan.BorrowedAt, loan.ReturnedAt)))
            .ToList();

        var overall = ReadingPace.Overall(paces.Select(item => item.Pace).ToList());

        var perLoan = paces
            .OrderByDescending(item => item.Loan.ReturnedAt)
            .Select(item => new LoanReadingPace(
                item.Loan.LoanId,
                item.Loan.BookId,
                item.Loan.Title,
                item.Loan.PageCount,
                item.Loan.BorrowedAt,
                item.Loan.ReturnedAt,
                Math.Round(item.Pace.Days, Precision),
                Math.Round(item.Pace.PagesPerDay, Precision)))
            .ToList();

        return new ReadingPaceReport(
            borrower.Id,
            borrower.FullName,
            perLoan.Count,
            overall is null ? null : Math.Round(overall.Value, Precision),
            perLoan);
    }
}

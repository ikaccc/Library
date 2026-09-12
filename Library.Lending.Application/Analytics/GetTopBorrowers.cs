using FluentValidation;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Analytics;

public sealed record GetTopBorrowersQuery(TimeRange Range, int Top) : IQuery<Result<IReadOnlyList<TopBorrower>>>;

internal sealed class GetTopBorrowersValidator : AbstractValidator<GetTopBorrowersQuery>
{
    public GetTopBorrowersValidator()
    {
        RuleFor(x => x.Range).NotNull().BeWellOrdered();
        RuleFor(x => x.Top).InclusiveBetween(1, Ranking.MaxTop);
    }
}

internal sealed class GetTopBorrowersHandler(IAnalyticsQueries analytics)
    : IQueryHandler<GetTopBorrowersQuery, Result<IReadOnlyList<TopBorrower>>>
{
    public async Task<Result<IReadOnlyList<TopBorrower>>> HandleAsync(GetTopBorrowersQuery query, CancellationToken cancellationToken)
    {
        var borrowers = await analytics.GetTopBorrowersAsync(query.Range, query.Top, cancellationToken);

        return Result.Success(borrowers);
    }
}

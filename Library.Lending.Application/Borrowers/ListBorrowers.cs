using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Borrowers;

public sealed record ListBorrowersQuery(int Page, int PageSize) : IQuery<Result<PagedResult<BorrowerDto>>>;

internal sealed class ListBorrowersValidator : AbstractValidator<ListBorrowersQuery>
{
    public ListBorrowersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class ListBorrowersHandler(IBorrowerRepository borrowers)
    : IQueryHandler<ListBorrowersQuery, Result<PagedResult<BorrowerDto>>>
{
    public async Task<Result<PagedResult<BorrowerDto>>> HandleAsync(ListBorrowersQuery query, CancellationToken cancellationToken)
    {
        var page = await borrowers.ListAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(borrower => borrower.ToDto());
    }
}

using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Loans;

public sealed record ListLoansQuery(
    Guid? BookId,
    Guid? BorrowerId,
    LoanStatus? Status,
    int Page,
    int PageSize) : IQuery<Result<PagedResult<LoanDto>>>;

internal sealed class ListLoansValidator : AbstractValidator<ListLoansQuery>
{
    public ListLoansValidator()
    {
        RuleFor(x => x.BookId).NotEqual(Guid.Empty).When(x => x.BookId is not null);
        RuleFor(x => x.BorrowerId).NotEqual(Guid.Empty).When(x => x.BorrowerId is not null);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class ListLoansHandler(ILoanRepository loans, TimeProvider clock)
    : IQueryHandler<ListLoansQuery, Result<PagedResult<LoanDto>>>
{
    public async Task<Result<PagedResult<LoanDto>>> HandleAsync(ListLoansQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var filter = new LoanFilter(query.BookId, query.BorrowerId, query.Status, now);

        var page = await loans.ListAsync(filter, query.Page, query.PageSize, cancellationToken);

        return page.Map(loan => loan.ToDto(now));
    }
}

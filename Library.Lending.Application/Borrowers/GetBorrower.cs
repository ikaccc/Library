using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Borrowers;

public sealed record GetBorrowerQuery(Guid BorrowerId) : IQuery<Result<BorrowerDto>>;

internal sealed class GetBorrowerValidator : AbstractValidator<GetBorrowerQuery>
{
    public GetBorrowerValidator()
    {
        RuleFor(x => x.BorrowerId).NotEmpty();
    }
}

internal sealed class GetBorrowerHandler(IBorrowerRepository borrowers) : IQueryHandler<GetBorrowerQuery, Result<BorrowerDto>>
{
    public async Task<Result<BorrowerDto>> HandleAsync(GetBorrowerQuery query, CancellationToken cancellationToken)
    {
        var borrower = await borrowers.GetByIdAsync(query.BorrowerId, cancellationToken);

        return borrower is null ? BorrowerErrors.NotFound(query.BorrowerId) : borrower.ToDto();
    }
}

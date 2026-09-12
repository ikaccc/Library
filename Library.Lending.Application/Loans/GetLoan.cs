using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Loans;

public sealed record GetLoanQuery(Guid LoanId) : IQuery<Result<LoanDto>>;

internal sealed class GetLoanValidator : AbstractValidator<GetLoanQuery>
{
    public GetLoanValidator()
    {
        RuleFor(x => x.LoanId).NotEmpty();
    }
}

internal sealed class GetLoanHandler(ILoanRepository loans, TimeProvider clock) : IQueryHandler<GetLoanQuery, Result<LoanDto>>
{
    public async Task<Result<LoanDto>> HandleAsync(GetLoanQuery query, CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(query.LoanId, cancellationToken);

        return loan is null ? LoanErrors.NotFound(query.LoanId) : loan.ToDto(clock.GetUtcNow());
    }
}

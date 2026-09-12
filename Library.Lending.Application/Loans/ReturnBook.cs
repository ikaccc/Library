using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Loans;

public sealed record ReturnBookCommand(Guid LoanId) : ICommand<Result<LoanDto>>;

internal sealed class ReturnBookValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookValidator()
    {
        RuleFor(x => x.LoanId).NotEmpty();
    }
}

internal sealed class ReturnBookHandler(
    ILoanRepository loans,
    IBookRepository books,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ReturnBookCommand, Result<LoanDto>>
{
    public async Task<Result<LoanDto>> HandleAsync(ReturnBookCommand command, CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(command.LoanId, cancellationToken);
        if (loan is null)
        {
            return LoanErrors.NotFound(command.LoanId);
        }

        var book = await books.GetByIdAsync(loan.BookId, cancellationToken)
            ?? throw new InvalidOperationException($"Loan '{loan.Id}' references book '{loan.BookId}', which no longer exists.");

        var now = clock.GetUtcNow();
        var returned = LendingDesk.Return(loan, book, now);
        if (returned.IsFailure)
        {
            return returned.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return loan.ToDto(now);
    }
}

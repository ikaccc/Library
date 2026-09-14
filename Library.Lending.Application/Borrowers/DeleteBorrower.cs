using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Borrowers;

public sealed record DeleteBorrowerCommand(Guid BorrowerId) : ICommand<Result>;

internal sealed class DeleteBorrowerValidator : AbstractValidator<DeleteBorrowerCommand>
{
    public DeleteBorrowerValidator()
    {
        RuleFor(x => x.BorrowerId).NotEmpty();
    }
}

internal sealed class DeleteBorrowerHandler(
    IBorrowerRepository borrowers,
    ILoanRepository loans,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteBorrowerCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteBorrowerCommand command, CancellationToken cancellationToken)
    {
        var borrower = await borrowers.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return BorrowerErrors.NotFound(command.BorrowerId);
        }

        if (await loans.ExistsForBorrowerAsync(borrower.Id, cancellationToken))
        {
            return BorrowerErrors.HasLoans(borrower.Id, borrower.FullName);
        }

        borrowers.Remove(borrower);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Borrowers;

public sealed record UpdateBorrowerCommand(Guid BorrowerId, string FullName, string? Email) : ICommand<Result<BorrowerDto>>;

internal sealed class UpdateBorrowerValidator : AbstractValidator<UpdateBorrowerCommand>
{
    public UpdateBorrowerValidator()
    {
        RuleFor(x => x.BorrowerId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(Borrower.MaxFullNameLength);
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(Borrower.MaxEmailLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

internal sealed class UpdateBorrowerHandler(IBorrowerRepository borrowers, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateBorrowerCommand, Result<BorrowerDto>>
{
    public async Task<Result<BorrowerDto>> HandleAsync(UpdateBorrowerCommand command, CancellationToken cancellationToken)
    {
        var borrower = await borrowers.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return BorrowerErrors.NotFound(command.BorrowerId);
        }

        borrower.Update(command.FullName, command.Email);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return borrower.ToDto();
    }
}

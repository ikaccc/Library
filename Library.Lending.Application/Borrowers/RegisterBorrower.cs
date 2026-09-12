using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Borrowers;

public sealed record RegisterBorrowerCommand(string FullName, string? Email) : ICommand<Result<BorrowerDto>>;

internal sealed class RegisterBorrowerValidator : AbstractValidator<RegisterBorrowerCommand>
{
    public RegisterBorrowerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(Borrower.MaxFullNameLength);
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(Borrower.MaxEmailLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

internal sealed class RegisterBorrowerHandler(
    IBorrowerRepository borrowers,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RegisterBorrowerCommand, Result<BorrowerDto>>
{
    public async Task<Result<BorrowerDto>> HandleAsync(RegisterBorrowerCommand command, CancellationToken cancellationToken)
    {
        var borrower = Borrower.Register(command.FullName, command.Email, clock.GetUtcNow());

        borrowers.Add(borrower);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return borrower.ToDto();
    }
}

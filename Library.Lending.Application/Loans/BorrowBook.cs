using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;

namespace Library.Lending.Application.Loans;

public sealed record BorrowBookCommand(Guid BookId, Guid BorrowerId) : ICommand<Result<LoanDto>>;

internal sealed class BorrowBookValidator : AbstractValidator<BorrowBookCommand>
{
    public BorrowBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.BorrowerId).NotEmpty();
    }
}

internal sealed class BorrowBookHandler(
    IBookRepository books,
    IBorrowerRepository borrowers,
    ILoanRepository loans,
    IUnitOfWork unitOfWork,
    LoanPolicy policy,
    TimeProvider clock) : ICommandHandler<BorrowBookCommand, Result<LoanDto>>
{
    public async Task<Result<LoanDto>> HandleAsync(BorrowBookCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
        {
            return BookErrors.NotFound(command.BookId);
        }

        var borrower = await borrowers.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null)
        {
            return BorrowerErrors.NotFound(command.BorrowerId);
        }

        var openLoans = await loans.GetOpenLoansForBorrowerAsync(borrower.Id, cancellationToken);
        var now = clock.GetUtcNow();

        var borrowed = LendingDesk.Borrow(book, borrower, openLoans, policy, now);
        if (borrowed.IsFailure)
        {
            return borrowed.Error!;
        }

        loans.Add(borrowed.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return borrowed.Value.ToDto(now);
    }
}

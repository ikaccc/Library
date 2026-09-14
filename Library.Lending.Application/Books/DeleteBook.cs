using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Books;

public sealed record DeleteBookCommand(Guid BookId) : ICommand<Result>;

internal sealed class DeleteBookValidator : AbstractValidator<DeleteBookCommand>
{
    public DeleteBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
    }
}

internal sealed class DeleteBookHandler(
    IBookRepository books,
    ILoanRepository loans,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteBookCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
        {
            return BookErrors.NotFound(command.BookId);
        }

        if (await loans.ExistsForBookAsync(book.Id, cancellationToken))
        {
            return BookErrors.HasLoans(book.Id, book.Title);
        }

        books.Remove(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

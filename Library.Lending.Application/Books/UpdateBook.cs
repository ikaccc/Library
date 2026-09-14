using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Books;

public sealed record UpdateBookCommand(
    Guid BookId,
    string Title,
    string Author,
    string? Isbn,
    int PageCount,
    int TotalCopies) : ICommand<Result<BookDto>>, IBookDetails;

internal sealed class UpdateBookValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        this.ApplyBookDetailsRules();
    }
}

internal sealed class UpdateBookHandler(IBookRepository books, IUnitOfWork unitOfWork) : ICommandHandler<UpdateBookCommand, Result<BookDto>>
{
    public async Task<Result<BookDto>> HandleAsync(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
        {
            return BookErrors.NotFound(command.BookId);
        }

        Isbn? isbn = null;
        if (!string.IsNullOrWhiteSpace(command.Isbn))
        {
            var parsed = Isbn.Create(command.Isbn);
            if (parsed.IsFailure)
            {
                return parsed.Error!;
            }

            isbn = parsed.Value;
            if (await books.ExistsWithIsbnAsync(isbn, excludingBookId: book.Id, cancellationToken))
            {
                return BookErrors.DuplicateIsbn(isbn);
            }
        }

        var updated = book.Update(command.Title, command.Author, isbn, command.PageCount, command.TotalCopies);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book.ToDto();
    }
}

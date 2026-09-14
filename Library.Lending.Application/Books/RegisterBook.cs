using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Books;

public sealed record RegisterBookCommand(
    string Title,
    string Author,
    string? Isbn,
    int PageCount,
    int TotalCopies) : ICommand<Result<BookDto>>, IBookDetails;

internal sealed class RegisterBookValidator : AbstractValidator<RegisterBookCommand>
{
    public RegisterBookValidator()
    {
        this.ApplyBookDetailsRules();
    }
}

internal sealed class RegisterBookHandler(
    IBookRepository books,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RegisterBookCommand, Result<BookDto>>
{
    public async Task<Result<BookDto>> HandleAsync(RegisterBookCommand command, CancellationToken cancellationToken)
    {
        Isbn? isbn = null;
        if (!string.IsNullOrWhiteSpace(command.Isbn))
        {
            var parsed = Isbn.Create(command.Isbn);
            if (parsed.IsFailure)
            {
                return parsed.Error!;
            }

            isbn = parsed.Value;
            if (await books.ExistsWithIsbnAsync(isbn, excludingBookId: null, cancellationToken))
            {
                return BookErrors.DuplicateIsbn(isbn);
            }
        }

        var book = Book.Register(command.Title, command.Author, isbn, command.PageCount, command.TotalCopies, clock.GetUtcNow());

        books.Add(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book.ToDto();
    }
}

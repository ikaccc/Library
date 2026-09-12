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
    int TotalCopies) : ICommand<Result<BookDto>>;

internal sealed class RegisterBookValidator : AbstractValidator<RegisterBookCommand>
{
    public const int MaxPageCount = 50_000;
    public const int MaxTotalCopies = 1_000;

    public RegisterBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Book.MaxTitleLength);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(Book.MaxAuthorLength);
        RuleFor(x => x.Isbn)
            .Must(Isbn.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.Isbn))
            .WithMessage("'{PropertyValue}' is not a valid ISBN-10 or ISBN-13.");
        RuleFor(x => x.PageCount).InclusiveBetween(1, MaxPageCount);
        RuleFor(x => x.TotalCopies).InclusiveBetween(1, MaxTotalCopies);
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
            if (await books.ExistsWithIsbnAsync(isbn, cancellationToken))
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

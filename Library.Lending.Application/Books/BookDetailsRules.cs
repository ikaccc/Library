using FluentValidation;
using Library.Lending.Domain.Books;

namespace Library.Lending.Application.Books;

public interface IBookDetails
{
    string Title { get; }

    string Author { get; }

    string? Isbn { get; }

    int PageCount { get; }

    int TotalCopies { get; }
}

internal static class BookDetailsRules
{
    public const int MaxPageCount = 50_000;
    public const int MaxTotalCopies = 1_000;

    public static void ApplyBookDetailsRules<TCommand>(this AbstractValidator<TCommand> validator)
        where TCommand : IBookDetails
    {
        validator.RuleFor(x => x.Title).NotEmpty().MaximumLength(Book.MaxTitleLength);
        validator.RuleFor(x => x.Author).NotEmpty().MaximumLength(Book.MaxAuthorLength);
        validator.RuleFor(x => x.Isbn)
            .Must(Isbn.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.Isbn))
            .WithMessage("'{PropertyValue}' is not a valid ISBN-10 or ISBN-13.");
        validator.RuleFor(x => x.PageCount).InclusiveBetween(1, MaxPageCount);
        validator.RuleFor(x => x.TotalCopies).InclusiveBetween(1, MaxTotalCopies);
    }
}

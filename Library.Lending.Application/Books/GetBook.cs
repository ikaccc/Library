using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Books;

public sealed record GetBookQuery(Guid BookId) : IQuery<Result<BookDto>>;

internal sealed class GetBookValidator : AbstractValidator<GetBookQuery>
{
    public GetBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
    }
}

internal sealed class GetBookHandler(IBookRepository books) : IQueryHandler<GetBookQuery, Result<BookDto>>
{
    public async Task<Result<BookDto>> HandleAsync(GetBookQuery query, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(query.BookId, cancellationToken);

        return book is null ? BookErrors.NotFound(query.BookId) : book.ToDto();
    }
}

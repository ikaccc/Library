using Library.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Api.Endpoints;

internal static class BooksEndpoints
{
    public static RouteGroupBuilder MapBooks(this RouteGroupBuilder api)
    {
        var books = api.MapGroup("/books").WithTags("Books");

        books.MapPost("/", RegisterBook)
            .WithName("RegisterBook")
            .WithSummary("Register a title and how many copies the library owns.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        books.MapGet("/", ListBooks)
            .WithName("ListBooks")
            .WithSummary("List books, optionally filtered by a case insensitive title or author search.")
            .ProducesValidationProblem();

        books.MapGet("/{id:guid}", GetBook)
            .WithName("GetBook")
            .WithSummary("Get one book with its current inventory.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Created<BookResponse>> RegisterBook(
        RegisterBookRequest request,
        V1.BooksService.BooksServiceClient client,
        CancellationToken cancellationToken)
    {
        var message = new V1.RegisterBookRequest
        {
            Title = request.Title ?? string.Empty,
            Author = request.Author ?? string.Empty,
            PageCount = request.PageCount,
            TotalCopies = request.TotalCopies,
        };
        if (request.Isbn is not null)
        {
            message.Isbn = request.Isbn;
        }

        var book = await client.RegisterBookAsync(message, cancellationToken: cancellationToken);
        var response = book.ToResponse();

        return TypedResults.Created($"/api/v1/books/{response.Id}", response);
    }

    private static async Task<Ok<PagedResponse<BookResponse>>> ListBooks(
        V1.BooksService.BooksServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var message = new V1.ListBooksRequest { Page = page, PageSize = pageSize };
        if (search is not null)
        {
            message.Search = search;
        }

        var response = await client.ListBooksAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<BookResponse>> GetBook(
        Guid id,
        V1.BooksService.BooksServiceClient client,
        CancellationToken cancellationToken)
    {
        var book = await client.GetBookAsync(new V1.GetBookRequest { Id = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.Ok(book.ToResponse());
    }
}

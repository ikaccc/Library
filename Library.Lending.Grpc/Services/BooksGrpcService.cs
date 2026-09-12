using Grpc.Core;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Books;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;
using Library.Lending.Grpc.Errors;
using Library.Lending.Grpc.Mapping;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Services;

public sealed class BooksGrpcService(
    ICommandHandler<RegisterBookCommand, Result<BookDto>> registerBook,
    IQueryHandler<GetBookQuery, Result<BookDto>> getBook,
    IQueryHandler<ListBooksQuery, Result<PagedResult<BookDto>>> listBooks) : V1.BooksService.BooksServiceBase
{
    public override async Task<V1.Book> RegisterBook(V1.RegisterBookRequest request, ServerCallContext context)
    {
        var command = new RegisterBookCommand(
            request.Title,
            request.Author,
            RequestMapping.OptionalString(request.HasIsbn, request.Isbn),
            request.PageCount,
            request.TotalCopies);

        var result = await registerBook.HandleAsync(command, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.Book> GetBook(V1.GetBookRequest request, ServerCallContext context)
    {
        var query = new GetBookQuery(RequestMapping.ParseId(request.Id, "id"));

        var result = await getBook.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.ListBooksResponse> ListBooks(V1.ListBooksRequest request, ServerCallContext context)
    {
        var query = new ListBooksQuery(
            RequestMapping.OptionalString(request.HasSearch, request.Search),
            RequestMapping.PageOrDefault(request.Page),
            RequestMapping.PageSizeOrDefault(request.PageSize));

        var result = await listBooks.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }
}

using Grpc.Core;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Analytics;
using Library.Lending.Domain.Common;
using Library.Lending.Grpc.Errors;
using Library.Lending.Grpc.Mapping;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Services;

public sealed class AnalyticsGrpcService(
    IQueryHandler<GetMostBorrowedBooksQuery, Result<IReadOnlyList<MostBorrowedBook>>> mostBorrowedBooks,
    IQueryHandler<GetTopBorrowersQuery, Result<IReadOnlyList<TopBorrower>>> topBorrowers,
    IQueryHandler<GetReadingPaceQuery, Result<ReadingPaceReport>> readingPace,
    IQueryHandler<GetAlsoBorrowedBooksQuery, Result<IReadOnlyList<AlsoBorrowedBook>>> alsoBorrowedBooks) : V1.AnalyticsService.AnalyticsServiceBase
{
    public override async Task<V1.GetMostBorrowedBooksResponse> GetMostBorrowedBooks(V1.GetMostBorrowedBooksRequest request, ServerCallContext context)
    {
        var query = new GetMostBorrowedBooksQuery(request.Range.ToTimeRange(), RequestMapping.TopOrDefault(request.Top));

        var result = await mostBorrowedBooks.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.GetTopBorrowersResponse> GetTopBorrowers(V1.GetTopBorrowersRequest request, ServerCallContext context)
    {
        var query = new GetTopBorrowersQuery(request.Range.ToTimeRange(), RequestMapping.TopOrDefault(request.Top));

        var result = await topBorrowers.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.GetReadingPaceResponse> GetReadingPace(V1.GetReadingPaceRequest request, ServerCallContext context)
    {
        var query = new GetReadingPaceQuery(RequestMapping.ParseId(request.BorrowerId, "borrower_id"));

        var result = await readingPace.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.GetAlsoBorrowedBooksResponse> GetAlsoBorrowedBooks(V1.GetAlsoBorrowedBooksRequest request, ServerCallContext context)
    {
        var query = new GetAlsoBorrowedBooksQuery(RequestMapping.ParseId(request.BookId, "book_id"), RequestMapping.TopOrDefault(request.Top));

        var result = await alsoBorrowedBooks.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }
}

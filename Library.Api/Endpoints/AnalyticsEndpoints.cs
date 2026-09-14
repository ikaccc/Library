using Google.Protobuf.WellKnownTypes;
using Library.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Api.Endpoints;

internal static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalytics(this RouteGroupBuilder api)
    {
        var analytics = api.MapGroup("/analytics").WithTags("Analytics");

        analytics.MapGet("/books/most-borrowed", MostBorrowedBooks)
            .WithName("GetMostBorrowedBooks")
            .WithSummary("Books ranked by how often they were borrowed. Omit from/to for all time, the range is [from, to).")
            .ProducesValidationProblem();

        analytics.MapGet("/borrowers/top", TopBorrowers)
            .WithName("GetTopBorrowers")
            .WithSummary("Borrowers ranked by how many books they borrowed. Omit from/to for all time, the range is [from, to).")
            .ProducesValidationProblem();

        analytics.MapGet("/borrowers/{id:guid}/reading-pace", ReadingPace)
            .WithName("GetReadingPace")
            .WithSummary("Estimated pages per day for a borrower, from returned loans, assuming continuous reading.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        analytics.MapGet("/books/{id:guid}/also-borrowed", AlsoBorrowedBooks)
            .WithName("GetAlsoBorrowedBooks")
            .WithSummary("Other books borrowed by the people who borrowed this one, ranked by how many of them did.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<IReadOnlyList<MostBorrowedBookResponse>>> MostBorrowedBooks(
        V1.AnalyticsService.AnalyticsServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int top = 10)
    {
        var message = new V1.GetMostBorrowedBooksRequest { Range = ToRange(from, to), Top = top };

        var response = await client.GetMostBorrowedBooksAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<IReadOnlyList<TopBorrowerResponse>>> TopBorrowers(
        V1.AnalyticsService.AnalyticsServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int top = 10)
    {
        var message = new V1.GetTopBorrowersRequest { Range = ToRange(from, to), Top = top };

        var response = await client.GetTopBorrowersAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<ReadingPaceResponse>> ReadingPace(
        Guid id,
        V1.AnalyticsService.AnalyticsServiceClient client,
        CancellationToken cancellationToken)
    {
        var response = await client.GetReadingPaceAsync(new V1.GetReadingPaceRequest { BorrowerId = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<IReadOnlyList<AlsoBorrowedBookResponse>>> AlsoBorrowedBooks(
        Guid id,
        V1.AnalyticsService.AnalyticsServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] int top = 10)
    {
        var message = new V1.GetAlsoBorrowedBooksRequest { BookId = id.ToString(), Top = top };

        var response = await client.GetAlsoBorrowedBooksAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static V1.TimeRange ToRange(DateTimeOffset? from, DateTimeOffset? to) => new()
    {
        From = from is { } start ? Timestamp.FromDateTimeOffset(start) : null,
        To = to is { } end ? Timestamp.FromDateTimeOffset(end) : null,
    };
}

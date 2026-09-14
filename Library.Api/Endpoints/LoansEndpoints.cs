using Library.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Api.Endpoints;

internal static class LoansEndpoints
{
    public static RouteGroupBuilder MapLoans(this RouteGroupBuilder api)
    {
        var loans = api.MapGroup("/loans").WithTags("Loans");

        loans.MapPost("/", BorrowBook)
            .WithName("BorrowBook")
            .WithSummary("Check a book out to a borrower. Fails when no copy is available, the borrower is at the open loan limit, or already has this title.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        loans.MapPost("/{id:guid}/return", ReturnBook)
            .WithName("ReturnBook")
            .WithSummary("Check a book back in. Returning the same loan twice is a conflict.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        loans.MapGet("/", ListLoans)
            .WithName("ListLoans")
            .WithSummary("List loans, newest first, optionally filtered by book, borrower and status.")
            .ProducesValidationProblem();

        loans.MapGet("/{id:guid}", GetLoan)
            .WithName("GetLoan")
            .WithSummary("Get one loan.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Created<LoanResponse>> BorrowBook(
        BorrowBookRequest request,
        V1.LoansService.LoansServiceClient client,
        CancellationToken cancellationToken)
    {
        var message = new V1.BorrowBookRequest { BookId = request.BookId.ToString(), BorrowerId = request.BorrowerId.ToString() };

        var loan = await client.BorrowBookAsync(message, cancellationToken: cancellationToken);
        var response = loan.ToResponse();

        return TypedResults.Created($"/api/v1/loans/{response.Id}", response);
    }

    private static async Task<Ok<LoanResponse>> ReturnBook(
        Guid id,
        V1.LoansService.LoansServiceClient client,
        CancellationToken cancellationToken)
    {
        var loan = await client.ReturnBookAsync(new V1.ReturnBookRequest { LoanId = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.Ok(loan.ToResponse());
    }

    private static async Task<Ok<PagedResponse<LoanResponse>>> ListLoans(
        V1.LoansService.LoansServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] Guid? bookId = null,
        [FromQuery] Guid? borrowerId = null,
        [FromQuery] LoanStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var message = new V1.ListLoansRequest { Status = status.ToProto(), Page = page, PageSize = pageSize };
        if (bookId is { } book)
        {
            message.BookId = book.ToString();
        }

        if (borrowerId is { } borrower)
        {
            message.BorrowerId = borrower.ToString();
        }

        var response = await client.ListLoansAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<LoanResponse>> GetLoan(
        Guid id,
        V1.LoansService.LoansServiceClient client,
        CancellationToken cancellationToken)
    {
        var loan = await client.GetLoanAsync(new V1.GetLoanRequest { Id = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.Ok(loan.ToResponse());
    }
}

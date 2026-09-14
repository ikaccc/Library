using Library.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Api.Endpoints;

internal static class BorrowersEndpoints
{
    public static RouteGroupBuilder MapBorrowers(this RouteGroupBuilder api)
    {
        var borrowers = api.MapGroup("/borrowers").WithTags("Borrowers");

        borrowers.MapPost("/", RegisterBorrower)
            .WithName("RegisterBorrower")
            .WithSummary("Register a library member.")
            .ProducesValidationProblem();

        borrowers.MapGet("/", ListBorrowers)
            .WithName("ListBorrowers")
            .WithSummary("List library members.")
            .ProducesValidationProblem();

        borrowers.MapGet("/{id:guid}", GetBorrower)
            .WithName("GetBorrower")
            .WithSummary("Get one library member.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        borrowers.MapPut("/{id:guid}", UpdateBorrower)
            .WithName("UpdateBorrower")
            .WithSummary("Replace a member's details.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        borrowers.MapDelete("/{id:guid}", DeleteBorrower)
            .WithName("DeleteBorrower")
            .WithSummary("Delete a member who never borrowed. A member with lending history cannot be deleted.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<BorrowerResponse>> UpdateBorrower(
        Guid id,
        UpdateBorrowerRequest request,
        V1.BorrowersService.BorrowersServiceClient client,
        CancellationToken cancellationToken)
    {
        var message = new V1.UpdateBorrowerRequest { Id = id.ToString(), FullName = request.FullName ?? string.Empty };
        if (request.Email is not null)
        {
            message.Email = request.Email;
        }

        var borrower = await client.UpdateBorrowerAsync(message, cancellationToken: cancellationToken);

        return TypedResults.Ok(borrower.ToResponse());
    }

    private static async Task<NoContent> DeleteBorrower(
        Guid id,
        V1.BorrowersService.BorrowersServiceClient client,
        CancellationToken cancellationToken)
    {
        await client.DeleteBorrowerAsync(new V1.DeleteBorrowerRequest { Id = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Created<BorrowerResponse>> RegisterBorrower(
        RegisterBorrowerRequest request,
        V1.BorrowersService.BorrowersServiceClient client,
        CancellationToken cancellationToken)
    {
        var message = new V1.RegisterBorrowerRequest { FullName = request.FullName ?? string.Empty };
        if (request.Email is not null)
        {
            message.Email = request.Email;
        }

        var borrower = await client.RegisterBorrowerAsync(message, cancellationToken: cancellationToken);
        var response = borrower.ToResponse();

        return TypedResults.Created($"/api/v1/borrowers/{response.Id}", response);
    }

    private static async Task<Ok<PagedResponse<BorrowerResponse>>> ListBorrowers(
        V1.BorrowersService.BorrowersServiceClient client,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await client.ListBorrowersAsync(new V1.ListBorrowersRequest { Page = page, PageSize = pageSize }, cancellationToken: cancellationToken);

        return TypedResults.Ok(response.ToResponse());
    }

    private static async Task<Ok<BorrowerResponse>> GetBorrower(
        Guid id,
        V1.BorrowersService.BorrowersServiceClient client,
        CancellationToken cancellationToken)
    {
        var borrower = await client.GetBorrowerAsync(new V1.GetBorrowerRequest { Id = id.ToString() }, cancellationToken: cancellationToken);

        return TypedResults.Ok(borrower.ToResponse());
    }
}

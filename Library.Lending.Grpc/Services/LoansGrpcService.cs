using Grpc.Core;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Common;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Common;
using Library.Lending.Grpc.Errors;
using Library.Lending.Grpc.Mapping;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Services;

public sealed class LoansGrpcService(
    ICommandHandler<BorrowBookCommand, Result<LoanDto>> borrowBook,
    ICommandHandler<ReturnBookCommand, Result<LoanDto>> returnBook,
    IQueryHandler<GetLoanQuery, Result<LoanDto>> getLoan,
    IQueryHandler<ListLoansQuery, Result<PagedResult<LoanDto>>> listLoans) : V1.LoansService.LoansServiceBase
{
    public override async Task<V1.Loan> BorrowBook(V1.BorrowBookRequest request, ServerCallContext context)
    {
        var command = new BorrowBookCommand(
            RequestMapping.ParseId(request.BookId, "book_id"),
            RequestMapping.ParseId(request.BorrowerId, "borrower_id"));

        var result = await borrowBook.HandleAsync(command, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.Loan> ReturnBook(V1.ReturnBookRequest request, ServerCallContext context)
    {
        var command = new ReturnBookCommand(RequestMapping.ParseId(request.LoanId, "loan_id"));

        var result = await returnBook.HandleAsync(command, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.Loan> GetLoan(V1.GetLoanRequest request, ServerCallContext context)
    {
        var query = new GetLoanQuery(RequestMapping.ParseId(request.Id, "id"));

        var result = await getLoan.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.ListLoansResponse> ListLoans(V1.ListLoansRequest request, ServerCallContext context)
    {
        var query = new ListLoansQuery(
            RequestMapping.ParseOptionalId(request.HasBookId, request.BookId, "book_id"),
            RequestMapping.ParseOptionalId(request.HasBorrowerId, request.BorrowerId, "borrower_id"),
            request.Status.ToApplication(),
            RequestMapping.PageOrDefault(request.Page),
            RequestMapping.PageSizeOrDefault(request.PageSize));

        var result = await listLoans.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }
}

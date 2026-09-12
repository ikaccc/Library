using Grpc.Core;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Borrowers;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;
using Library.Lending.Grpc.Errors;
using Library.Lending.Grpc.Mapping;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Services;

public sealed class BorrowersGrpcService(
    ICommandHandler<RegisterBorrowerCommand, Result<BorrowerDto>> registerBorrower,
    IQueryHandler<GetBorrowerQuery, Result<BorrowerDto>> getBorrower,
    IQueryHandler<ListBorrowersQuery, Result<PagedResult<BorrowerDto>>> listBorrowers) : V1.BorrowersService.BorrowersServiceBase
{
    public override async Task<V1.Borrower> RegisterBorrower(V1.RegisterBorrowerRequest request, ServerCallContext context)
    {
        var command = new RegisterBorrowerCommand(request.FullName, RequestMapping.OptionalString(request.HasEmail, request.Email));

        var result = await registerBorrower.HandleAsync(command, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.Borrower> GetBorrower(V1.GetBorrowerRequest request, ServerCallContext context)
    {
        var query = new GetBorrowerQuery(RequestMapping.ParseId(request.Id, "id"));

        var result = await getBorrower.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }

    public override async Task<V1.ListBorrowersResponse> ListBorrowers(V1.ListBorrowersRequest request, ServerCallContext context)
    {
        var query = new ListBorrowersQuery(
            RequestMapping.PageOrDefault(request.Page),
            RequestMapping.PageSizeOrDefault(request.PageSize));

        var result = await listBorrowers.HandleAsync(query, context.CancellationToken);

        return result.GetValueOrThrow().ToProto();
    }
}

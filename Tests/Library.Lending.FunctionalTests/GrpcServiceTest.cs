// ai-touched
using Google.Rpc;
using Grpc.Core;
using Grpc.Net.Client;
using Library.Lending.Contracts.V1;
using Library.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace Library.Lending.FunctionalTests;

[Collection(PostgresCollection.Name)]
public abstract class GrpcServiceTest(PostgresContainerFixture postgres) : IAsyncLifetime
{
    protected static readonly DateTimeOffset Start = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    protected FakeTimeProvider Clock { get; } = new(Start);

    protected LendingGrpcFactory Host { get; private set; } = null!;

    protected GrpcChannel Channel { get; private set; } = null!;

    protected BooksService.BooksServiceClient Books { get; private set; } = null!;

    protected BorrowersService.BorrowersServiceClient Borrowers { get; private set; } = null!;

    protected LoansService.LoansServiceClient Loans { get; private set; } = null!;

    protected AnalyticsService.AnalyticsServiceClient Analytics { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        Host = new LendingGrpcFactory(connectionString, Clock);
        Channel = Host.CreateGrpcChannel();
        Books = new BooksService.BooksServiceClient(Channel);
        Borrowers = new BorrowersService.BorrowersServiceClient(Channel);
        Loans = new LoansService.LoansServiceClient(Channel);
        Analytics = new AnalyticsService.AnalyticsServiceClient(Channel);
    }

    public async Task DisposeAsync()
    {
        Channel.Dispose();
        await Host.DisposeAsync();
    }

    protected Task<Book> RegisterBookAsync(string title = "Dune", string author = "Frank Herbert", int pageCount = 300, int copies = 1, string? isbn = null)
    {
        var request = new RegisterBookRequest { Title = title, Author = author, PageCount = pageCount, TotalCopies = copies };
        if (isbn is not null)
        {
            request.Isbn = isbn;
        }

        return Books.RegisterBookAsync(request).ResponseAsync;
    }

    protected Task<Borrower> RegisterBorrowerAsync(string fullName = "Ada Lovelace", string? email = null)
    {
        var request = new RegisterBorrowerRequest { FullName = fullName };
        if (email is not null)
        {
            request.Email = email;
        }

        return Borrowers.RegisterBorrowerAsync(request).ResponseAsync;
    }

    protected Task<Loan> BorrowAsync(Book book, Borrower borrower) =>
        Loans.BorrowBookAsync(new BorrowBookRequest { BookId = book.Id, BorrowerId = borrower.Id }).ResponseAsync;

    protected Task<Loan> ReturnAsync(Loan loan) =>
        Loans.ReturnBookAsync(new ReturnBookRequest { LoanId = loan.Id }).ResponseAsync;

    protected static async Task<RpcException> ShouldFailAsync<TResponse>(AsyncUnaryCall<TResponse> call, StatusCode expectedStatus)
    {
        var exception = await Should.ThrowAsync<RpcException>(async () => await call);
        exception.StatusCode.ShouldBe(expectedStatus, exception.Status.Detail);
        return exception;
    }

    protected static string ReasonOf(RpcException exception) =>
        exception.GetRpcStatus()?.GetDetail<ErrorInfo>()?.Reason ?? throw new InvalidOperationException("The error carries no ErrorInfo detail.");

    protected static IReadOnlyDictionary<string, string> FieldViolationsOf(RpcException exception)
    {
        var badRequest = exception.GetRpcStatus()?.GetDetail<BadRequest>() ?? throw new InvalidOperationException("The error carries no BadRequest detail.");
        return badRequest.FieldViolations.ToDictionary(violation => violation.Field, violation => violation.Description);
    }
}

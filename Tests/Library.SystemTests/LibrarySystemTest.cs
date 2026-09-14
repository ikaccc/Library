// ai-touched
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Api.Contracts;
using Library.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;

namespace Library.SystemTests;

[Collection(PostgresCollection.Name)]
public abstract class LibrarySystemTest(PostgresContainerFixture postgres) : IAsyncLifetime
{
    protected static readonly DateTimeOffset Start = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private LendingGrpcFactory _lending = null!;
    private LendingApiFactory _api = null!;

    protected FakeTimeProvider Clock { get; } = new(Start);

    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Configuration overrides for the API host under test; none by default.</summary>
    protected virtual IReadOnlyDictionary<string, string?>? ApiSettings => null;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        _lending = new LendingGrpcFactory(connectionString, Clock);
        _api = new LendingApiFactory(_lending, ApiSettings);
        Client = _api.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _api.DisposeAsync();
        await _lending.DisposeAsync();
    }

    protected async Task<BookResponse> RegisterBookAsync(string title = "Dune", string author = "Frank Herbert", int pageCount = 300, int copies = 1, string? isbn = null)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/books", new RegisterBookRequest(title, author, isbn, pageCount, copies), Json);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BookResponse>(Json))!;
    }

    protected async Task<BorrowerResponse> RegisterBorrowerAsync(string fullName = "Ada Lovelace", string? email = null)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/borrowers", new RegisterBorrowerRequest(fullName, email), Json);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BorrowerResponse>(Json))!;
    }

    protected async Task<LoanResponse> BorrowAsync(BookResponse book, BorrowerResponse borrower)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/loans", new BorrowBookRequest(book.Id, borrower.Id), Json);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(Json))!;
    }

    protected async Task<LoanResponse> ReturnAsync(LoanResponse loan)
    {
        var response = await Client.PostAsync(new Uri($"/api/v1/loans/{loan.Id}/return", UriKind.Relative), content: null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoanResponse>(Json))!;
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(new Uri(url, UriKind.Relative));
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, $"{url} responded {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    protected static async Task<ProblemDetails> ShouldBeProblemAsync(HttpResponseMessage response, int status, string? code = null)
    {
        ((int)response.StatusCode).ShouldBe(status, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, Json)!;
        problem.Status.ShouldBe(status, body);
        if (code is not null)
        {
            problem.Extensions.ShouldContainKey("code", body);
            problem.Extensions["code"]!.ToString().ShouldBe(code);
        }

        problem.Extensions.ShouldContainKey("traceId");
        return problem;
    }
}

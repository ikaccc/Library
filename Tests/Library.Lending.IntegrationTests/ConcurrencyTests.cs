// ai-touched
using Library.Lending.Application.Exceptions;
using Library.Lending.Infrastructure.Persistence;
using Library.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class ConcurrencyTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private LendingTestDatabase _database = null!;

    public async Task InitializeAsync() => _database = await LendingTestDatabase.CreateMigratedAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Two_requests_racing_for_the_last_copy_cannot_both_win()
    {
        var book = TestData.NewBook(copies: 1);
        await using (var db = _database.CreateContext())
        {
            db.Books.Add(book);
            await db.SaveChangesAsync();
        }

        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        var bookSeenByFirst = await first.Books.SingleAsync(b => b.Id == book.Id);
        var bookSeenBySecond = await second.Books.SingleAsync(b => b.Id == book.Id);
        bookSeenByFirst.LendCopy().IsSuccess.ShouldBeTrue();
        bookSeenBySecond.LendCopy().IsSuccess.ShouldBeTrue();

        await new EfUnitOfWork(first).SaveChangesAsync(CancellationToken.None);
        await Should.ThrowAsync<ConcurrencyConflictException>(() => new EfUnitOfWork(second).SaveChangesAsync(CancellationToken.None));

        await using var reader = _database.CreateContext();
        (await reader.Books.SingleAsync(b => b.Id == book.Id)).AvailableCopies.ShouldBe(0);
    }

    [Fact]
    public async Task The_database_itself_refuses_negative_inventory()
    {
        var book = TestData.NewBook(copies: 1);
        await using var db = _database.CreateContext();
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var rows = await db.Database.ExecuteSqlAsync(
            $"UPDATE lending.books SET available_copies = -1 WHERE id = {book.Id}").ContinueWith(t => t.IsFaulted ? -1 : t.Result);

        rows.ShouldBe(-1, "the check constraint must reject the update");
    }
}

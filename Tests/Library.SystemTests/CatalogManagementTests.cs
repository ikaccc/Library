// ai-touched
using System.Net;
using System.Net.Http.Json;
using Library.Api.Contracts;
using Library.TestSupport;

namespace Library.SystemTests;

public class CatalogManagementTests(PostgresContainerFixture postgres) : LibrarySystemTest(postgres)
{
    [Fact]
    public async Task Books_and_members_can_be_corrected_and_removed_until_they_have_history()
    {
        var book = await RegisterBookAsync("Dune", "F. Herbert", pageCount: 412, copies: 1);
        var spare = await RegisterBookAsync("Spare");
        var alice = await RegisterBorrowerAsync("Ada");
        var newcomer = await RegisterBorrowerAsync("Newcomer");

        var bookUpdate = await Client.PutAsJsonAsync($"/api/v1/books/{book.Id}", new UpdateBookRequest("Dune (Deluxe)", "Frank Herbert", "978-0-306-40615-7", 500, 2), Json);
        bookUpdate.StatusCode.ShouldBe(HttpStatusCode.OK, await bookUpdate.Content.ReadAsStringAsync());
        var updatedBook = (await bookUpdate.Content.ReadFromJsonAsync<BookResponse>(Json))!;
        updatedBook.Title.ShouldBe("Dune (Deluxe)");
        updatedBook.Isbn.ShouldBe("9780306406157");
        updatedBook.TotalCopies.ShouldBe(2);
        updatedBook.AvailableCopies.ShouldBe(2);
        (await GetAsync<BookResponse>($"/api/v1/books/{book.Id}")).ShouldBe(updatedBook);

        var memberUpdate = await Client.PutAsJsonAsync($"/api/v1/borrowers/{alice.Id}", new UpdateBorrowerRequest("Ada Lovelace", "ADA@Example.com"), Json);
        memberUpdate.StatusCode.ShouldBe(HttpStatusCode.OK, await memberUpdate.Content.ReadAsStringAsync());
        var updatedMember = (await memberUpdate.Content.ReadFromJsonAsync<BorrowerResponse>(Json))!;
        updatedMember.FullName.ShouldBe("Ada Lovelace");
        updatedMember.Email.ShouldBe("ada@example.com");

        (await Client.DeleteAsync(new Uri($"/api/v1/books/{spare.Id}", UriKind.Relative))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        ((int)(await Client.GetAsync(new Uri($"/api/v1/books/{spare.Id}", UriKind.Relative))).StatusCode).ShouldBe(404);

        (await Client.DeleteAsync(new Uri($"/api/v1/borrowers/{newcomer.Id}", UriKind.Relative))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await BorrowAsync(updatedBook, updatedMember);
        await ShouldBeProblemAsync(await Client.DeleteAsync(new Uri($"/api/v1/books/{book.Id}", UriKind.Relative)), 409, "BOOK_HAS_LOANS");
        await ShouldBeProblemAsync(await Client.DeleteAsync(new Uri($"/api/v1/borrowers/{alice.Id}", UriKind.Relative)), 409, "BORROWER_HAS_LOANS");

        var shrink = await Client.PutAsJsonAsync($"/api/v1/books/{book.Id}", new UpdateBookRequest("Dune", "Frank Herbert", null, 412, 0), Json);
        await ShouldBeProblemAsync(shrink, 400, "INVALID_ARGUMENT");

        var unknown = await Client.PutAsJsonAsync($"/api/v1/books/{Guid.NewGuid()}", new UpdateBookRequest("Dune", "Frank Herbert", null, 412, 1), Json);
        await ShouldBeProblemAsync(unknown, 404, "BOOK_NOT_FOUND");
    }
}

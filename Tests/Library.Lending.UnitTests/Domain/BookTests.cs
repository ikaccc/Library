using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.UnitTests.Domain;

public class BookTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_trims_text_and_starts_with_all_copies_available()
    {
        var isbn = Isbn.Create("9780306406157").Value;

        var book = Book.Register("  Moby Dick ", " Herman Melville ", isbn, pageCount: 635, totalCopies: 3, Now);

        book.Id.ShouldNotBe(Guid.Empty);
        book.Title.ShouldBe("Moby Dick");
        book.Author.ShouldBe("Herman Melville");
        book.Isbn.ShouldBe(isbn);
        book.PageCount.ShouldBe(635);
        book.TotalCopies.ShouldBe(3);
        book.AvailableCopies.ShouldBe(3);
        book.CopiesOnLoan.ShouldBe(0);
        book.RegisteredAt.ShouldBe(Now);
    }

    [Fact]
    public void Register_generates_time_ordered_ids()
    {
        var first = Book.Register("A", "B", null, 1, 1, Now);
        var second = Book.Register("A", "B", null, 1, 1, Now);

        first.Id.ShouldNotBe(second.Id);
        first.Id.Version.ShouldBe(7);
    }

    [Theory]
    [InlineData("", "Author", 100, 1)]
    [InlineData("   ", "Author", 100, 1)]
    [InlineData("Title", "", 100, 1)]
    [InlineData("Title", "Author", 0, 1)]
    [InlineData("Title", "Author", -5, 1)]
    [InlineData("Title", "Author", 100, 0)]
    public void Register_rejects_invalid_arguments(string title, string author, int pages, int copies)
    {
        Should.Throw<ArgumentException>(() => Book.Register(title, author, null, pages, copies, Now));
    }

    [Fact]
    public void Register_rejects_overlong_title()
    {
        var title = new string('x', Book.MaxTitleLength + 1);

        Should.Throw<ArgumentOutOfRangeException>(() => Book.Register(title, "Author", null, 10, 1, Now));
    }

    [Fact]
    public void LendCopy_takes_copies_off_the_shelf_until_none_are_left()
    {
        var book = Book.Register("Title", "Author", null, 100, totalCopies: 2, Now);

        book.LendCopy().IsSuccess.ShouldBeTrue();
        book.LendCopy().IsSuccess.ShouldBeTrue();
        var third = book.LendCopy();

        third.IsFailure.ShouldBeTrue();
        third.Error!.Type.ShouldBe(ErrorType.PreconditionFailed);
        third.Error.Code.ShouldBe("book.no_available_copies");
        book.AvailableCopies.ShouldBe(0);
        book.CopiesOnLoan.ShouldBe(2);
    }

    [Fact]
    public void ReturnCopy_puts_a_copy_back_and_refuses_to_exceed_the_inventory()
    {
        var book = Book.Register("Title", "Author", null, 100, totalCopies: 1, Now);
        book.LendCopy();

        book.ReturnCopy();

        book.AvailableCopies.ShouldBe(1);
        Should.Throw<InvalidOperationException>(book.ReturnCopy);
    }
}

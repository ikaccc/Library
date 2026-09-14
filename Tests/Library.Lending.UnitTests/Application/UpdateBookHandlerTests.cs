using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Books;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class UpdateBookHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBookRepository> _books = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Book _book = Book.Register("Dune", "Frank Herbert", null, 412, totalCopies: 3, Now);

    [Fact]
    public async Task Updates_details_checks_the_isbn_against_other_books_only_and_persists()
    {
        _book.LendCopy();
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _books.Setup(r => r.ExistsWithIsbnAsync(It.Is<Isbn>(i => i.Value == "9780306406157"), _book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new UpdateBookCommand(_book.Id, "Dune (Deluxe)", "Frank Herbert", "978-0-306-40615-7", 500, 5);
        var result = await CreateHandler().HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        result.Value.Title.ShouldBe("Dune (Deluxe)");
        result.Value.Isbn.ShouldBe("9780306406157");
        result.Value.TotalCopies.ShouldBe(5);
        result.Value.AvailableCopies.ShouldBe(4);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fails_with_not_found_for_an_unknown_book()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().HandleAsync(new UpdateBookCommand(_book.Id, "Dune", "Frank Herbert", null, 412, 3), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("book.not_found");
    }

    [Fact]
    public async Task Rejects_an_isbn_that_belongs_to_another_book()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _books.Setup(r => r.ExistsWithIsbnAsync(It.IsAny<Isbn>(), _book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(new UpdateBookCommand(_book.Id, "Dune", "Frank Herbert", "9780306406157", 412, 3), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("book.duplicate_isbn");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Does_not_persist_when_the_inventory_would_drop_below_the_copies_on_loan()
    {
        _book.LendCopy();
        _book.LendCopy();
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);

        var result = await CreateHandler().HandleAsync(new UpdateBookCommand(_book.Id, "Dune", "Frank Herbert", null, 412, 1), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.PreconditionFailed);
        result.Error.Code.ShouldBe("book.total_copies_below_copies_on_loan");
        _book.TotalCopies.ShouldBe(3);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private UpdateBookHandler CreateHandler() => new(_books.Object, _unitOfWork.Object);
}

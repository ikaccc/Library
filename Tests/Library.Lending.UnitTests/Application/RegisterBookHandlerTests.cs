using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Books;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class RegisterBookHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBookRepository> _books = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    [Fact]
    public async Task Registers_a_book_with_a_normalized_isbn_and_all_copies_available()
    {
        _books.Setup(r => r.ExistsWithIsbnAsync(It.Is<Isbn>(i => i.Value == "9780306406157"), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _books.Setup(r => r.ExistsWithIsbnAsync(It.Is<Isbn>(i => i.Value == "9780306406157"), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _books.Setup(r => r.Add(It.IsAny<Book>()));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new RegisterBookCommand(" Dune ", "Frank Herbert", "978-0-306-40615-7", 412, 3);
        var result = await CreateHandler().HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        result.Value.Title.ShouldBe("Dune");
        result.Value.Isbn.ShouldBe("9780306406157");
        result.Value.TotalCopies.ShouldBe(3);
        result.Value.AvailableCopies.ShouldBe(3);
        result.Value.RegisteredAt.ShouldBe(Now);
        _books.Verify(r => r.Add(It.IsAny<Book>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Registers_a_book_without_isbn_without_checking_for_duplicates()
    {
        _books.Setup(r => r.Add(It.IsAny<Book>()));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateHandler().HandleAsync(new RegisterBookCommand("Dune", "Frank Herbert", "  ", 412, 1), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Isbn.ShouldBeNull();
    }

    [Fact]
    public async Task Rejects_a_duplicate_isbn_with_a_conflict()
    {
        _books.Setup(r => r.ExistsWithIsbnAsync(It.IsAny<Isbn>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(new RegisterBookCommand("Dune", "Frank Herbert", "9780306406157", 412, 1), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("book.duplicate_isbn");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rejects_an_invalid_isbn_even_if_validation_was_bypassed()
    {
        var result = await CreateHandler().HandleAsync(new RegisterBookCommand("Dune", "Frank Herbert", "not-an-isbn", 412, 1), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
    }

    private RegisterBookHandler CreateHandler() => new(_books.Object, _unitOfWork.Object, new FakeTimeProvider(Now));
}

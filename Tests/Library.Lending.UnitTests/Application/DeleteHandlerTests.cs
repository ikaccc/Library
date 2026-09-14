using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Books;
using Library.Lending.Application.Borrowers;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class DeleteHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBookRepository> _books = new(MockBehavior.Strict);
    private readonly Mock<IBorrowerRepository> _borrowers = new(MockBehavior.Strict);
    private readonly Mock<ILoanRepository> _loans = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Book _book = Book.Register("Dune", "Frank Herbert", null, 412, 1, Now);
    private readonly Borrower _borrower = Borrower.Register("Ada Lovelace", null, Now);

    [Fact]
    public async Task A_book_without_history_is_removed_and_persisted()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _loans.Setup(r => r.ExistsForBookAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _books.Setup(r => r.Remove(_book));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await new DeleteBookHandler(_books.Object, _loans.Object, _unitOfWork.Object)
            .HandleAsync(new DeleteBookCommand(_book.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        _books.Verify(r => r.Remove(_book), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_book_with_lending_history_is_kept_with_a_conflict()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _loans.Setup(r => r.ExistsForBookAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await new DeleteBookHandler(_books.Object, _loans.Object, _unitOfWork.Object)
            .HandleAsync(new DeleteBookCommand(_book.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("book.has_loans");
        _books.Verify(r => r.Remove(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task An_unknown_book_is_not_found()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await new DeleteBookHandler(_books.Object, _loans.Object, _unitOfWork.Object)
            .HandleAsync(new DeleteBookCommand(_book.Id), CancellationToken.None);

        result.Error!.Code.ShouldBe("book.not_found");
    }

    [Fact]
    public async Task A_member_without_history_is_removed_but_a_reader_is_kept()
    {
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _loans.Setup(r => r.ExistsForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new DeleteBorrowerHandler(_borrowers.Object, _loans.Object, _unitOfWork.Object);

        var kept = await handler.HandleAsync(new DeleteBorrowerCommand(_borrower.Id), CancellationToken.None);
        kept.IsFailure.ShouldBeTrue();
        kept.Error!.Code.ShouldBe("borrower.has_loans");
        kept.Error.Type.ShouldBe(ErrorType.Conflict);

        _loans.Setup(r => r.ExistsForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _borrowers.Setup(r => r.Remove(_borrower));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var removed = await handler.HandleAsync(new DeleteBorrowerCommand(_borrower.Id), CancellationToken.None);
        removed.IsSuccess.ShouldBeTrue();
        _borrowers.Verify(r => r.Remove(_borrower), Times.Once);
    }

    [Fact]
    public async Task Updating_a_member_normalizes_and_persists()
    {
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await new UpdateBorrowerHandler(_borrowers.Object, _unitOfWork.Object)
            .HandleAsync(new UpdateBorrowerCommand(_borrower.Id, " Ada L. ", "ADA@Example.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FullName.ShouldBe("Ada L.");
        result.Value.Email.ShouldBe("ada@example.com");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

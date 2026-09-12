using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class ReturnBookHandlerTests
{
    private static readonly DateTimeOffset BorrowedAt = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = BorrowedAt.AddDays(5);

    private readonly Mock<ILoanRepository> _loans = new(MockBehavior.Strict);
    private readonly Mock<IBookRepository> _books = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly Book _book = Book.Register("Dune", "Frank Herbert", null, 412, totalCopies: 1, BorrowedAt);
    private readonly Loan _loan;

    public ReturnBookHandlerTests()
    {
        var borrower = Borrower.Register("Ada Lovelace", null, BorrowedAt);
        _loan = LendingDesk.Borrow(_book, borrower, [], LoanPolicy.Default, BorrowedAt).Value;
    }

    [Fact]
    public async Task Closes_the_loan_restores_the_copy_and_persists()
    {
        _loans.Setup(r => r.GetByIdAsync(_loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_loan);
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateHandler().HandleAsync(new ReturnBookCommand(_loan.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        result.Value.ReturnedAt.ShouldBe(Now);
        result.Value.Status.ShouldBe(LoanStatus.Returned);
        _book.AvailableCopies.ShouldBe(1);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fails_with_not_found_for_an_unknown_loan()
    {
        var loanId = Guid.CreateVersion7();
        _loans.Setup(r => r.GetByIdAsync(loanId, It.IsAny<CancellationToken>())).ReturnsAsync((Loan?)null);

        var result = await CreateHandler().HandleAsync(new ReturnBookCommand(loanId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("loan.not_found");
    }

    [Fact]
    public async Task Returning_twice_is_a_conflict_and_is_not_persisted()
    {
        LendingDesk.Return(_loan, _book, BorrowedAt.AddDays(2));
        _loans.Setup(r => r.GetByIdAsync(_loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_loan);
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);

        var result = await CreateHandler().HandleAsync(new ReturnBookCommand(_loan.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("loan.already_returned");
        _loan.ReturnedAt.ShouldBe(BorrowedAt.AddDays(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private ReturnBookHandler CreateHandler() =>
        new(_loans.Object, _books.Object, _unitOfWork.Object, new FakeTimeProvider(Now));
}

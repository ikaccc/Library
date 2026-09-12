using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class BorrowBookHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBookRepository> _books = new(MockBehavior.Strict);
    private readonly Mock<IBorrowerRepository> _borrowers = new(MockBehavior.Strict);
    private readonly Mock<ILoanRepository> _loans = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly LoanPolicy _policy = new(loanPeriodDays: 14, maxOpenLoansPerBorrower: 2);

    private readonly Book _book = Book.Register("Dune", "Frank Herbert", null, 412, totalCopies: 1, Now.AddYears(-1));
    private readonly Borrower _borrower = Borrower.Register("Ada Lovelace", null, Now.AddYears(-1));

    [Fact]
    public async Task Opens_a_loan_and_persists_it_when_everything_checks_out()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _loans.Setup(r => r.GetOpenLoansForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _loans.Setup(r => r.Add(It.Is<Loan>(l => l.BookId == _book.Id && l.BorrowerId == _borrower.Id)));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateHandler().HandleAsync(new BorrowBookCommand(_book.Id, _borrower.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        result.Value.BookId.ShouldBe(_book.Id);
        result.Value.BorrowerId.ShouldBe(_borrower.Id);
        result.Value.BorrowedAt.ShouldBe(Now);
        result.Value.DueAt.ShouldBe(Now.AddDays(14));
        result.Value.ReturnedAt.ShouldBeNull();
        result.Value.Status.ShouldBe(LoanStatus.Open);
        _book.AvailableCopies.ShouldBe(0);
        _loans.Verify(r => r.Add(It.IsAny<Loan>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Fails_with_not_found_when_the_book_does_not_exist()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().HandleAsync(new BorrowBookCommand(_book.Id, _borrower.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("book.not_found");
    }

    [Fact]
    public async Task Fails_with_not_found_when_the_borrower_does_not_exist()
    {
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Borrower?)null);

        var result = await CreateHandler().HandleAsync(new BorrowBookCommand(_book.Id, _borrower.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("borrower.not_found");
        _book.AvailableCopies.ShouldBe(1);
    }

    [Fact]
    public async Task Fails_without_persisting_when_a_lending_rule_blocks_the_loan()
    {
        var openLoans = new[]
        {
            LendingDesk.Borrow(Book.Register("A", "X", null, 100, 1, Now), _borrower, [], _policy, Now).Value,
            LendingDesk.Borrow(Book.Register("B", "X", null, 100, 1, Now), _borrower, [], _policy, Now).Value,
        };
        _books.Setup(r => r.GetByIdAsync(_book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_book);
        _borrowers.Setup(r => r.GetByIdAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_borrower);
        _loans.Setup(r => r.GetOpenLoansForBorrowerAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync(openLoans);

        var result = await CreateHandler().HandleAsync(new BorrowBookCommand(_book.Id, _borrower.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("loan.open_loan_limit_reached");
        result.Error.Type.ShouldBe(ErrorType.PreconditionFailed);
        _book.AvailableCopies.ShouldBe(1);
        _loans.Verify(r => r.Add(It.IsAny<Loan>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private BorrowBookHandler CreateHandler() =>
        new(_books.Object, _borrowers.Object, _loans.Object, _unitOfWork.Object, _policy, new FakeTimeProvider(Now));
}

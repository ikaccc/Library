using FluentValidation.TestHelper;
using Library.Lending.Application.Analytics;
using Library.Lending.Application.Books;
using Library.Lending.Application.Borrowers;
using Library.Lending.Application.Common;
using Library.Lending.Application.Loans;

namespace Library.Lending.UnitTests.Application;

public class ValidatorTests
{
    private static readonly DateTimeOffset Sept1 = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterBook_accepts_a_complete_command()
    {
        var result = new RegisterBookValidator().TestValidate(new RegisterBookCommand("Dune", "Frank Herbert", "978-0-306-40615-7", 412, 2));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RegisterBook_accepts_missing_isbn_but_rejects_a_malformed_one()
    {
        new RegisterBookValidator().TestValidate(new RegisterBookCommand("Dune", "Frank Herbert", null, 412, 2)).ShouldNotHaveAnyValidationErrors();
        new RegisterBookValidator().TestValidate(new RegisterBookCommand("Dune", "Frank Herbert", "123", 412, 2)).ShouldHaveValidationErrorFor(x => x.Isbn);
    }

    [Theory]
    [InlineData("", "Author", 100, 1, "Title")]
    [InlineData("Title", "", 100, 1, "Author")]
    [InlineData("Title", "Author", 0, 1, "PageCount")]
    [InlineData("Title", "Author", 50_001, 1, "PageCount")]
    [InlineData("Title", "Author", 100, 0, "TotalCopies")]
    [InlineData("Title", "Author", 100, 1_001, "TotalCopies")]
    public void RegisterBook_rejects_out_of_range_fields(string title, string author, int pages, int copies, string property)
    {
        var result = new RegisterBookValidator().TestValidate(new RegisterBookCommand(title, author, null, pages, copies));

        result.ShouldHaveValidationErrorFor(property);
    }

    [Fact]
    public void RegisterBorrower_requires_a_name_and_a_well_formed_optional_email()
    {
        var validator = new RegisterBorrowerValidator();

        validator.TestValidate(new RegisterBorrowerCommand("Ada", null)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new RegisterBorrowerCommand("Ada", "ada@example.com")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new RegisterBorrowerCommand("", "ada@example.com")).ShouldHaveValidationErrorFor(x => x.FullName);
        validator.TestValidate(new RegisterBorrowerCommand("Ada", "not-an-email")).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Paged_queries_reject_page_zero_and_oversized_pages()
    {
        var validator = new ListBooksValidator();

        validator.TestValidate(new ListBooksQuery(null, 1, Paging.MaxPageSize)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ListBooksQuery(null, 0, 20)).ShouldHaveValidationErrorFor(x => x.Page);
        validator.TestValidate(new ListBooksQuery(null, 1, Paging.MaxPageSize + 1)).ShouldHaveValidationErrorFor(x => x.PageSize);
        validator.TestValidate(new ListBooksQuery(null, 1, 0)).ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void ListLoans_rejects_empty_guids_used_as_filters()
    {
        var validator = new ListLoansValidator();

        validator.TestValidate(new ListLoansQuery(null, null, null, 1, 20)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ListLoansQuery(Guid.Empty, null, LoanStatus.Open, 1, 20)).ShouldHaveValidationErrorFor(x => x.BookId);
    }

    [Fact]
    public void Ranking_queries_accept_open_ranges_and_reject_inverted_ones()
    {
        var validator = new GetMostBorrowedBooksValidator();

        validator.TestValidate(new GetMostBorrowedBooksQuery(TimeRange.AllTime, 10)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new GetMostBorrowedBooksQuery(new TimeRange(Sept1, null), 10)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new GetMostBorrowedBooksQuery(new TimeRange(Sept1, Sept1.AddDays(30)), 10)).ShouldNotHaveAnyValidationErrors();

        var inverted = validator.TestValidate(new GetMostBorrowedBooksQuery(new TimeRange(Sept1.AddDays(30), Sept1), 10));
        inverted.ShouldHaveValidationErrorFor(x => x.Range).WithErrorMessage("'to' must be later than 'from'.");

        validator.TestValidate(new GetMostBorrowedBooksQuery(new TimeRange(Sept1, Sept1), 10)).ShouldHaveValidationErrorFor(x => x.Range);
        validator.TestValidate(new GetMostBorrowedBooksQuery(TimeRange.AllTime, 0)).ShouldHaveValidationErrorFor(x => x.Top);
        validator.TestValidate(new GetMostBorrowedBooksQuery(TimeRange.AllTime, Ranking.MaxTop + 1)).ShouldHaveValidationErrorFor(x => x.Top);
    }

    [Fact]
    public void TopBorrowers_and_AlsoBorrowed_share_the_same_bounds()
    {
        new GetTopBorrowersValidator().TestValidate(new GetTopBorrowersQuery(TimeRange.AllTime, 0)).ShouldHaveValidationErrorFor(x => x.Top);
        new GetAlsoBorrowedBooksValidator().TestValidate(new GetAlsoBorrowedBooksQuery(Guid.Empty, 10)).ShouldHaveValidationErrorFor(x => x.BookId);
        new GetAlsoBorrowedBooksValidator().TestValidate(new GetAlsoBorrowedBooksQuery(Guid.CreateVersion7(), 101)).ShouldHaveValidationErrorFor(x => x.Top);
    }
}

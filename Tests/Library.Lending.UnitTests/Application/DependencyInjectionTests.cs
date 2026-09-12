using FluentValidation;
using Library.Lending.Application;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Analytics;
using Library.Lending.Application.Books;
using Library.Lending.Application.Loans;
using Library.Lending.Domain.Common;
using Library.Lending.Domain.Loans;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Library.Lending.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Every_handler_is_registered_and_wrapped_in_validation()
    {
        var books = new Mock<IBookRepository>(MockBehavior.Strict);
        await using var provider = BuildProvider(books.Object);

        var borrow = provider.GetRequiredService<ICommandHandler<BorrowBookCommand, Result<LoanDto>>>();
        var mostBorrowed = provider.GetRequiredService<IQueryHandler<GetMostBorrowedBooksQuery, Result<IReadOnlyList<MostBorrowedBook>>>>();
        provider.GetRequiredService<ICommandHandler<RegisterBookCommand, Result<BookDto>>>();
        provider.GetRequiredService<IQueryHandler<GetReadingPaceQuery, Result<ReadingPaceReport>>>();

        borrow.GetType().Name.ShouldStartWith("ValidatingCommandHandler");
        mostBorrowed.GetType().Name.ShouldStartWith("ValidatingQueryHandler");

        var exception = await Should.ThrowAsync<ValidationException>(
            () => borrow.HandleAsync(new BorrowBookCommand(Guid.Empty, Guid.Empty), CancellationToken.None));

        exception.Errors.Select(e => e.PropertyName).ShouldBe(["BookId", "BorrowerId"], ignoreOrder: true);
        books.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Loan_policy_comes_from_options_and_fails_fast_when_misconfigured()
    {
        using var configured = BuildProvider(Mock.Of<IBookRepository>(), options =>
        {
            options.LoanPeriodDays = 21;
            options.MaxOpenLoansPerBorrower = 3;
        });
        var policy = configured.GetRequiredService<LoanPolicy>();
        policy.LoanPeriodDays.ShouldBe(21);
        policy.MaxOpenLoansPerBorrower.ShouldBe(3);

        using var broken = BuildProvider(Mock.Of<IBookRepository>(), options => options.LoanPeriodDays = 0);
        Should.Throw<ArgumentOutOfRangeException>(() => broken.GetRequiredService<LoanPolicy>());
    }

    private static ServiceProvider BuildProvider(IBookRepository books, Action<LendingOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(books);
        services.AddSingleton(Mock.Of<IBorrowerRepository>());
        services.AddSingleton(Mock.Of<ILoanRepository>());
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        services.AddSingleton(Mock.Of<IAnalyticsQueries>());
        services.AddLendingApplication();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false });
    }
}

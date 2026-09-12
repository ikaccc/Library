using Library.Lending.Domain.Borrowers;

namespace Library.Lending.UnitTests.Domain;

public class BorrowerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_trims_name_and_normalizes_email()
    {
        var borrower = Borrower.Register("  Ada Lovelace ", " Ada.Lovelace@Example.com ", Now);

        borrower.Id.ShouldNotBe(Guid.Empty);
        borrower.FullName.ShouldBe("Ada Lovelace");
        borrower.Email.ShouldBe("ada.lovelace@example.com");
        borrower.JoinedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_treats_blank_email_as_absent(string? email)
    {
        Borrower.Register("Ada Lovelace", email, Now).Email.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Register_rejects_blank_name(string name)
    {
        Should.Throw<ArgumentException>(() => Borrower.Register(name, null, Now));
    }
}

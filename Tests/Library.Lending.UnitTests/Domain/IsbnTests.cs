using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.UnitTests.Domain;
//https://en.wikipedia.org/wiki/ISBN
public class IsbnTests
{
    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("9780306406157", "9780306406157")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0 306 40615 2", "0306406152")]
    [InlineData("0-8044-2957-X", "080442957X")]
    [InlineData("0-8044-2957-x", "080442957X")]
    public void Accepts_valid_isbn_10_and_13_and_normalizes_them(string raw, string expected)
    {
        var result = Isbn.Create(raw);

        result.IsSuccess.ShouldBeTrue(result.Error?.Message);
        result.Value.Value.ShouldBe(expected);
        result.Value.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData("978-0-306-40615-8")] // wrong ISBN-13 check digit
    [InlineData("0-306-40615-3")] // wrong ISBN-10 check digit
    [InlineData("12345")] // wrong length
    [InlineData("97803064061570")] // 14 digits
    [InlineData("ABC-0-306-40615-7")] // letters
    [InlineData("X306406152")] // X only allowed as the last ISBN-10 character
    public void Rejects_invalid_isbns_with_a_validation_error(string raw)
    {
        var result = Isbn.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("isbn.invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_input(string? raw)
    {
        var result = Isbn.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe("isbn.empty");
    }

    [Fact]
    public void Two_isbns_with_the_same_value_are_equal()
    {
        Isbn.Create("978-0-306-40615-7").Value.ShouldBe(Isbn.Create("9780306406157").Value);
    }
}

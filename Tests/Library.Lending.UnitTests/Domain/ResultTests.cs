using Library.Lending.Domain.Common;

namespace Library.Lending.UnitTests.Domain;

public class ResultTests
{
    private static readonly Error SomeError = Error.NotFound("thing.not_found", "Thing was not found.");

    [Fact]
    public void Success_carries_value_and_no_error()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_carries_error_and_refuses_to_expose_value()
    {
        Result<int> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_converts_implicitly_to_successful_result()
    {
        Result<string> result = "hello";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void Non_generic_result_converts_from_error()
    {
        Result result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }
}

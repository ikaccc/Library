namespace Library.Warmup.Tests;

public class BookIdChecksTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(1024)]
    [InlineData(1L << 40)]
    [InlineData(1L << 62)]
    public void Powers_of_two_are_recognized(long id)
    {
        BookIdChecks.IsPowerOfTwo(id).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(100)]
    [InlineData(1023)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void Other_numbers_are_rejected(long id)
    {
        BookIdChecks.IsPowerOfTwo(id).ShouldBeFalse();
    }
}

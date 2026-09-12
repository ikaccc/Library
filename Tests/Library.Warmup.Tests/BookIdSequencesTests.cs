namespace Library.Warmup.Tests;

public class BookIdSequencesTests
{
    [Fact]
    public void Default_range_yields_the_fifty_odd_numbers_between_0_and_100()
    {
        var ids = BookIdSequences.OddBookIds().ToList();

        ids.Count.ShouldBe(50);
        ids.First().ShouldBe(1);
        ids.Last().ShouldBe(99);
        ids.ShouldAllBe(id => id % 2 == 1);
        ids.ShouldBe(ids.Order());
    }

    [Theory]
    [InlineData(0, 0, new int[0])]
    [InlineData(1, 1, new[] { 1 })]
    [InlineData(2, 2, new int[0])]
    [InlineData(3, 9, new[] { 3, 5, 7, 9 })]
    [InlineData(4, 9, new[] { 5, 7, 9 })]
    public void Custom_ranges_are_inclusive_on_both_ends(int from, int to, int[] expected)
    {
        BookIdSequences.OddBookIds(from, to).ShouldBe(expected);
    }

    [Fact]
    public void Invalid_ranges_are_rejected_eagerly()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BookIdSequences.OddBookIds(10, 5));
        Should.Throw<ArgumentOutOfRangeException>(() => BookIdSequences.OddBookIds(-1, 5));
    }
}

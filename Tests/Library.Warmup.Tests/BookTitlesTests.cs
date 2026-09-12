namespace Library.Warmup.Tests;

public class BookTitlesTests
{
    [Theory]
    [InlineData("Moby Dick", "kciD yboM")]
    [InlineData("", "")]
    [InlineData("A", "A")]
    [InlineData("ab", "ba")]
    [InlineData("Крвава свадба", "абдавс ававрК")]
    [InlineData("Ivan 😊 Cekov", "vokeC 😊 navI")]
    [InlineData("Cékov Ivan", "navI vokéC")]
    [InlineData("C😊éköv Ivän", "nävI vöké😊C")]
    public void Reverse_reverses_the_title(string title, string expected)
    {
        BookTitles.Reverse(title).ShouldBe(expected);
    }

    [Fact]
    public void Reverse_keeps_multi_code_unit_characters_intact()
    {
        BookTitles.Reverse("Code 👩‍💻 Book").ShouldBe("kooB 👩‍💻 edoC");
        BookTitles.Reverse("café").ShouldBe("éfac");
        BookTitles.Reverse("café").ShouldBe("éfac");
    }

    [Fact]
    public void Reverse_rejects_null()
    {
        Should.Throw<ArgumentNullException>(() => BookTitles.Reverse(null!));
    }

    [Theory]
    [InlineData("Read", 3, "ReadReadRead")]
    [InlineData("Read", 1, "Read")]
    [InlineData("Read", 0, "")]
    [InlineData("", 5, "")]
    public void Replicate_repeats_the_title(string title, int times, string expected)
    {
        BookTitles.Replicate(title, times).ShouldBe(expected);
    }

    [Fact]
    public void Replicate_rejects_negative_counts_and_null()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BookTitles.Replicate("Read", -1));
        Should.Throw<ArgumentNullException>(() => BookTitles.Replicate(null!, 2));
    }
}

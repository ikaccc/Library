using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.UnitTests.Domain;

public class EntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid SomeId = Guid.CreateVersion7();

    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal_whatever_their_state()
    {
        var first = new Shelf(SomeId, "north wing");
        var second = new Shelf(SomeId, "renamed");

        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_or_types_are_not_equal()
    {
        var shelf = new Shelf(SomeId, "north wing");
        var otherShelf = new Shelf(Guid.CreateVersion7(), "north wing");
        var cart = new Cart(SomeId);

        shelf.Equals(otherShelf).ShouldBeFalse();
        shelf.Equals(cart).ShouldBeFalse();
        shelf.Equals(null).ShouldBeFalse();
        (shelf == null).ShouldBeFalse();
        ((Shelf?)null == (Shelf?)null).ShouldBeTrue();
    }

    [Fact]
    public void Aggregates_get_distinct_ids_and_are_only_equal_to_themselves()
    {
        var first = Book.Register("Title", "Author", null, 1, 1, Now);
        var second = Book.Register("Title", "Author", null, 1, 1, Now);

        first.ShouldBe(first);
        first.ShouldNotBe(second);
        new HashSet<Book> { first, second, first }.Count.ShouldBe(2);
    }

    [Fact]
    public void An_empty_id_is_rejected()
    {
        Should.Throw<ArgumentException>(() => new Shelf(Guid.Empty, "anywhere"));
    }

    private sealed class Shelf(Guid id, string location) : Entity(id)
    {
        public string Location { get; } = location;
    }

    private sealed class Cart(Guid id) : Entity(id);
}

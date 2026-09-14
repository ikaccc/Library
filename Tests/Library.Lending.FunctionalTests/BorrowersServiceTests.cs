using Grpc.Core;
using Library.Lending.Contracts.V1;
using Library.TestSupport;

namespace Library.Lending.FunctionalTests;

public class BorrowersServiceTests(PostgresContainerFixture postgres) : GrpcServiceTest(postgres)
{
    [Fact]
    public async Task A_member_can_be_registered_updated_and_listed()
    {
        var registered = await RegisterBorrowerAsync("Ada", "ADA@Example.com");
        registered.Email.ShouldBe("ada@example.com");
        registered.JoinedAt.ToDateTimeOffset().ShouldBe(Start);

        var updated = await Borrowers.UpdateBorrowerAsync(new UpdateBorrowerRequest { Id = registered.Id, FullName = "  Ada Lovelace ", Email = "Lovelace@Example.com" });
        updated.FullName.ShouldBe("Ada Lovelace");
        updated.Email.ShouldBe("lovelace@example.com");
        updated.JoinedAt.ShouldBe(registered.JoinedAt);
        (await Borrowers.GetBorrowerAsync(new GetBorrowerRequest { Id = registered.Id })).ShouldBe(updated);

        var withoutEmail = await Borrowers.UpdateBorrowerAsync(new UpdateBorrowerRequest { Id = registered.Id, FullName = "Ada Lovelace" });
        withoutEmail.HasEmail.ShouldBeFalse();

        await RegisterBorrowerAsync("Grace Hopper");
        var page = await Borrowers.ListBorrowersAsync(new ListBorrowersRequest());
        page.Borrowers.Select(b => b.FullName).ShouldBe(["Ada Lovelace", "Grace Hopper"]);
        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Invalid_member_input_is_rejected_with_field_level_details()
    {
        var registration = await ShouldFailAsync(Borrowers.RegisterBorrowerAsync(new RegisterBorrowerRequest { FullName = "", Email = "nope" }), StatusCode.InvalidArgument);
        FieldViolationsOf(registration).Keys.ShouldBe(["full_name", "email"], ignoreOrder: true);

        var update = await ShouldFailAsync(Borrowers.UpdateBorrowerAsync(new UpdateBorrowerRequest { Id = "not-a-guid", FullName = "Ada" }), StatusCode.InvalidArgument);
        FieldViolationsOf(update).Keys.ShouldBe(["id"]);

        var unknown = await ShouldFailAsync(Borrowers.UpdateBorrowerAsync(new UpdateBorrowerRequest { Id = Guid.NewGuid().ToString(), FullName = "Ada" }), StatusCode.NotFound);
        ReasonOf(unknown).ShouldBe("BORROWER_NOT_FOUND");
    }

    [Fact]
    public async Task A_member_without_history_can_be_deleted_but_a_reader_cannot()
    {
        var newcomer = await RegisterBorrowerAsync("Newcomer");
        var reader = await RegisterBorrowerAsync("Reader");
        var loan = await BorrowAsync(await RegisterBookAsync(), reader);

        await Borrowers.DeleteBorrowerAsync(new DeleteBorrowerRequest { Id = newcomer.Id });
        ReasonOf(await ShouldFailAsync(Borrowers.GetBorrowerAsync(new GetBorrowerRequest { Id = newcomer.Id }), StatusCode.NotFound)).ShouldBe("BORROWER_NOT_FOUND");

        ReasonOf(await ShouldFailAsync(Borrowers.DeleteBorrowerAsync(new DeleteBorrowerRequest { Id = reader.Id }), StatusCode.AlreadyExists)).ShouldBe("BORROWER_HAS_LOANS");

        await ReturnAsync(loan);
        ReasonOf(await ShouldFailAsync(Borrowers.DeleteBorrowerAsync(new DeleteBorrowerRequest { Id = reader.Id }), StatusCode.AlreadyExists)).ShouldBe("BORROWER_HAS_LOANS");
    }
}

using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Loans;

public sealed class Loan : Entity
{
    private Loan()
    {
        // Materialized by EF Core.
    }

    private Loan(Guid bookId, Guid borrowerId, DateTimeOffset borrowedAt, DateTimeOffset dueAt)
        : base(Guid.CreateVersion7())
    {
        BookId = bookId;
        BorrowerId = borrowerId;
        BorrowedAt = borrowedAt;
        DueAt = dueAt;
    }

    public Guid BookId { get; private set; }

    public Guid BorrowerId { get; private set; }

    public DateTimeOffset BorrowedAt { get; private set; }

    public DateTimeOffset DueAt { get; private set; }

    public DateTimeOffset? ReturnedAt { get; private set; }

    public bool IsReturned => ReturnedAt is not null;

    public bool IsOverdueAt(DateTimeOffset now) => !IsReturned && now > DueAt;

    internal static Loan Open(Guid bookId, Guid borrowerId, DateTimeOffset now, LoanPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new Loan(bookId, borrowerId, now, policy.DueDateFor(now));
    }

    internal Result MarkReturned(DateTimeOffset now)
    {
        if (IsReturned)
        {
            return LoanErrors.AlreadyReturned(Id, ReturnedAt!.Value);
        }

        if (now < BorrowedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(now), now, "A loan cannot be returned before it was borrowed.");
        }

        ReturnedAt = now;
        return Result.Success();
    }
}

using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Borrowers;

public sealed class Borrower : Entity
{
    public const int MaxFullNameLength = 200;
    public const int MaxEmailLength = 320;

    private Borrower()
    {
        // Materialized by EF Core.
    }

    private Borrower(string fullName, string? email, DateTimeOffset joinedAt)
        : base(Guid.CreateVersion7())
    {
        FullName = fullName;
        Email = email;
        JoinedAt = joinedAt;
    }


    public string FullName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public static Borrower Register(string fullName, string? email, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fullName.Length, MaxFullNameLength);

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (normalizedEmail is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(normalizedEmail.Length, MaxEmailLength);
        }

        return new Borrower(fullName.Trim(), normalizedEmail, now);
    }
}

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
        ValidateFullName(fullName);

        return new Borrower(fullName.Trim(), NormalizeEmail(email), now);
    }

    public void Update(string fullName, string? email)
    {
        ValidateFullName(fullName);

        FullName = fullName.Trim();
        Email = NormalizeEmail(email);
    }

    private static void ValidateFullName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fullName.Length, MaxFullNameLength);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalized.Length, MaxEmailLength);
        return normalized;
    }
}

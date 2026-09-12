using Library.Lending.Domain.Common;

namespace Library.Lending.Domain.Books;

public sealed record Isbn
{
    private Isbn(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Isbn> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Error.Validation("isbn.empty", "ISBN must not be empty.");
        }

        var normalized = Normalize(raw);

        var isValid = normalized.Length switch
        {
            10 => HasValidIsbn10CheckDigit(normalized),
            13 => HasValidIsbn13CheckDigit(normalized),
            _ => false,
        };

        return isValid
            ? new Isbn(normalized)
            : Error.Validation("isbn.invalid", $"'{raw}' is not a valid ISBN-10 or ISBN-13.");
    }

    public static bool IsValid(string? raw) => Create(raw).IsSuccess;

    public static Isbn FromTrusted(string value) => new(value);

    public override string ToString() => Value;

    private static string Normalize(string raw) =>
        string.Concat(raw.Where(c => c is not ('-' or ' '))).ToUpperInvariant();

    private static bool HasValidIsbn10CheckDigit(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            var c = isbn[i];
            int digit;
            if (char.IsAsciiDigit(c))
            {
                digit = c - '0';
            }
            else if (c == 'X' && i == 9)
            {
                digit = 10;
            }
            else
            {
                return false;
            }

            sum += (10 - i) * digit;
        }

        return sum % 11 == 0;
    }

    private static bool HasValidIsbn13CheckDigit(string isbn)
    {
        if (!isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            var digit = isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        return sum % 10 == 0;
    }
}

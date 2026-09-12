// ai-touched
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Borrowers;
using Library.Lending.Domain.Loans;
using Library.Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Library.Lending.Infrastructure.Seeding;

public sealed record SeedResult(bool Seeded, int Books, int Borrowers, int Loans, int OpenLoans)
{
    public static SeedResult Skipped { get; } = new(false, 0, 0, 0, 0);
}

public sealed class SampleDataSeeder(
    LendingDbContext db,
    LoanPolicy policy,
    TimeProvider clock,
    ILogger<SampleDataSeeder> logger)
{
    public const int RandomSeed = 20260911;
    public const int TargetLoanCount = 400;
    public const int HistoryDays = 540;

    public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Books.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Sample data seeding skipped: the database already contains books");
            return SeedResult.Skipped;
        }

        var now = clock.GetUtcNow();
        var random = new Random(RandomSeed);
        var historyStart = now.AddDays(-HistoryDays);

        var books = SampleCatalog.Books
            .Select((sample, index) => Book.Register(
                sample.Title,
                sample.Author,
                Isbn.FromTrusted(SyntheticIsbn13(index)),
                sample.PageCount,
                totalCopies: random.Next(1, 5),
                historyStart.AddDays(-random.Next(0, 365))))
            .ToList();

        var borrowers = SampleCatalog.Borrowers
            .Select(sample => Borrower.Register(sample.FullName, sample.Email, historyStart.AddDays(-random.Next(0, 365))))
            .ToList();

        var loans = SimulateHistory(books, borrowers, random, now);

        db.Books.AddRange(books);
        db.Borrowers.AddRange(borrowers);
        db.Loans.AddRange(loans);
        await db.SaveChangesAsync(cancellationToken);

        var result = new SeedResult(true, books.Count, borrowers.Count, loans.Count, loans.Count(loan => !loan.IsReturned));
        logger.LogInformation(
            "Seeded {Books} books, {Borrowers} borrowers and {Loans} loans ({OpenLoans} still open)",
            result.Books,
            result.Borrowers,
            result.Loans,
            result.OpenLoans);

        return result;
    }

    private List<Loan> SimulateHistory(IReadOnlyList<Book> books, IReadOnlyList<Borrower> borrowers, Random random, DateTimeOffset now)
    {
        var bookPicker = new WeightedPicker(books.Count, exponent: 0.8);
        var borrowerPicker = new WeightedPicker(borrowers.Count, exponent: 0.5);

        var timeline = new PriorityQueue<SeedEvent, (DateTimeOffset At, int Sequence)>();
        var sequence = 0;

        for (var i = 0; i < TargetLoanCount; i++)
        {
            var borrowedAt = AtLibraryHours(now.AddDays(-random.NextDouble() * HistoryDays), now, random);
            var durationDays = 2 + Math.Round(random.NextDouble() * 26, 2);
            var checkout = SeedEvent.Checkout(bookPicker.Pick(random), borrowerPicker.Pick(random), durationDays);
            timeline.Enqueue(checkout, (borrowedAt, sequence++));
        }

        var loans = new List<Loan>(TargetLoanCount);
        var openLoansByBorrower = new Dictionary<Guid, List<Loan>>();

        while (timeline.TryDequeue(out var seedEvent, out var priority))
        {
            var at = priority.At;
            var book = books[seedEvent.BookIndex];

            if (seedEvent.Loan is { } loanToReturn)
            {
                LendingDesk.Return(loanToReturn, book, at);
                openLoansByBorrower[loanToReturn.BorrowerId].Remove(loanToReturn);
                continue;
            }

            var borrower = borrowers[seedEvent.BorrowerIndex];
            if (!openLoansByBorrower.TryGetValue(borrower.Id, out var openLoans))
            {
                openLoans = [];
                openLoansByBorrower[borrower.Id] = openLoans;
            }

            var borrowed = LendingDesk.Borrow(book, borrower, openLoans, policy, at);
            if (borrowed.IsFailure)
            {
                // No copy left or the member is at the limit
                continue;
            }

            var loan = borrowed.Value;
            loans.Add(loan);
            openLoans.Add(loan);

            var returnAt = at.AddDays(seedEvent.DurationDays);
            if (returnAt <= now)
            {
                timeline.Enqueue(SeedEvent.Return(loan, seedEvent.BookIndex), (returnAt, sequence++));
            }
        }

        return loans;
    }

    private static DateTimeOffset AtLibraryHours(DateTimeOffset moment, DateTimeOffset now, Random random)
    {
        var day = new DateTimeOffset(moment.UtcDateTime.Date, TimeSpan.Zero);
        var candidate = day.AddHours(random.Next(9, 19)).AddMinutes(random.Next(0, 60));

        return candidate > now ? candidate.AddDays(-1) : candidate;
    }

    internal static string SyntheticIsbn13(int index)
    {
        var body = $"9780000{index:D5}";
        var sum = 0;
        for (var i = 0; i < body.Length; i++)
        {
            var digit = body[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return body + checkDigit;
    }

    private sealed record SeedEvent(int BookIndex, int BorrowerIndex, double DurationDays, Loan? Loan)
    {
        public static SeedEvent Checkout(int bookIndex, int borrowerIndex, double durationDays) =>
            new(bookIndex, borrowerIndex, durationDays, Loan: null);

        public static SeedEvent Return(Loan loan, int bookIndex) =>
            new(bookIndex, BorrowerIndex: -1, DurationDays: 0, loan);
    }

    private sealed class WeightedPicker
    {
        private readonly double[] _cumulative;

        public WeightedPicker(int count, double exponent)
        {
            _cumulative = new double[count];
            var running = 0d;
            for (var i = 0; i < count; i++)
            {
                running += 1d / Math.Pow(i + 1, exponent);
                _cumulative[i] = running;
            }
        }

        public int Pick(Random random)
        {
            var target = random.NextDouble() * _cumulative[^1];
            var index = Array.BinarySearch(_cumulative, target);
            return index >= 0 ? index : Math.Min(~index, _cumulative.Length - 1);
        }
    }
}

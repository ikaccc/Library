using Library.Warmup;

Console.WriteLine("Warmup 1: Power Of Two Book IDs");
foreach (var id in new long[] { 0, 1, 2, 3, 64, 100, 1024 })
{
    Console.WriteLine($"  {id,5} -> {BookIdChecks.IsPowerOfTwo(id)}");
}

Console.WriteLine();
Console.WriteLine("Warmup 2: Reversed Titles");
foreach (var title in new[] { "Moby Dick", "War and Peace", "Ulysses", "Ivan 😊 Cekov", "Cékov Ivan", "C😊éköv Ivän" })
{
    Console.WriteLine($"  \"{title}\" -> \"{BookTitles.Reverse(title)}\"");
}

Console.WriteLine();
Console.WriteLine("Warmup 3: Title Replicas");
Console.WriteLine($"  (\"Read\", 3) -> \"{BookTitles.Replicate("Read", 3)}\"");

Console.WriteLine();
Console.WriteLine("Warmup 4: Odd Numbered Book IDs Between 0 and 100");
Console.WriteLine($"  {string.Join(' ', BookIdSequences.OddBookIds())}");

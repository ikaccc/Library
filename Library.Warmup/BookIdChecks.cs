namespace Library.Warmup;

public static class BookIdChecks
{
    public static bool IsPowerOfTwo(long bookId) => bookId > 0 && (bookId & (bookId - 1)) == 0;
}

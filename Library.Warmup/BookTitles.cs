using System.Globalization;
using System.Text;

namespace Library.Warmup;

public static class BookTitles
{
    public static string Reverse(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        if (title.Length < 2)
        {
            return title;
        }

        var graphemes = new List<string>(title.Length);
        //going through text elemnts instead of chars to handle combining characters correctly
        var enumerator = StringInfo.GetTextElementEnumerator(title);
        while (enumerator.MoveNext())
        {
            graphemes.Add(enumerator.GetTextElement());
        }

        graphemes.Reverse();
        return string.Concat(graphemes);
    }

    public static string Replicate(string title, int times)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentOutOfRangeException.ThrowIfNegative(times);

        if (times == 0 || title.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(title.Length * times);
        for (var i = 0; i < times; i++)
        {
            builder.Append(title);
        }

        return builder.ToString();
    }
}

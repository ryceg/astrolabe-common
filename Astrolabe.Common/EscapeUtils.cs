using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Astrolabe.Common;

public static class EscapeUtils
{
    private static readonly IDictionary<char, char> PipeEscape =
        new Dictionary<char, char> { { '|', 'p' } };

    public static string[] UnescapePipe(string source)
    {
        return source.Split('|').Select(x => UnescapeString(x, '\\', PipeEscape))
            .ToArray();
    }

    public static string UnescapeString(string source, char escapeChar, IDictionary<char, char> replacements)
    {
        var builder = new StringBuilder(source.Length);
        var prevEscape = false;
        foreach (var ch in source)
        {
            var isEscape = ch == escapeChar;
            if (prevEscape)
            {
                builder.Append(isEscape ? ch : replacements[ch]);
                prevEscape = false;
            }
            else if (isEscape)
            {
                prevEscape = true;
            }
            else builder.Append(ch);
        }

        return builder.ToString();
    }

    public static string EscapeString(string source, char escapeChar, IDictionary<char, char> replacements)
    {
        var builder = new StringBuilder(source.Length + 4);
        foreach (var ch in source)
        {
            if (ch == escapeChar)
            {
                builder.Append(escapeChar);
                builder.Append(escapeChar);
            }
            else if (replacements.ContainsKey(ch))
            {
                builder.Append(escapeChar);
                builder.Append(replacements[ch]);
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
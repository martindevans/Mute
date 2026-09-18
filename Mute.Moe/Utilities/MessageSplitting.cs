using System.Text;
using System.Text.RegularExpressions;

namespace Mute.Moe.Utilities;

/// <summary>
/// Helpers for splitting up messages
/// </summary>
public static class MessageSplitting
{
    private static readonly Regex BulletListItemPattern = new(@"^ {0,3}[-*](?=\s)", RegexOptions.Compiled);
    private static readonly Regex NumberedListItemPattern = new(@"^ {0,3}\d{1,9}[.)](?=\s)", RegexOptions.Compiled);

    /// <summary>
    /// Split a message up into paragraphs
    /// </summary>
    /// <remarks>
    /// A paragraph is a section of the message delimited by blank lines, with the exception that
    /// markdown multiline constructs are never split apart:
    /// <list type="bullet">
    /// <item>Code blocks are kept as a single string</item>
    /// <item>Bullet point lists are kept as a single string, attached to whatever preceded them</item>
    /// <item>Numbered lists are kept as a single string, attached to whatever preceded them</item>
    /// </list>
    /// </remarks>
    /// <param name="message">The message to split</param>
    /// <returns>The message split into paragraphs</returns>
    public static List<string> SplitIntoParagraphs(string message)
    {
        var paragraphs = new List<string>();
        var current = new List<string>();

        var inCodeBlock = false;
        var codeFenceCharacter = '\0';
        var codeFenceLength = 0;
        var holdingBlankLine = false;

        foreach (var rawLine in message.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Lines inside a code block are always part of the current paragraph
            if (inCodeBlock)
            {
                current.Add(line);
                if (IsClosingCodeFence(line, codeFenceCharacter, codeFenceLength))
                    inCodeBlock = false;
                continue;
            }

            // A blank line delimits paragraphs, unless the current paragraph ends with a list item
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count == 0)
                    continue;

                if (EndsWithListItem(current))
                {
                    holdingBlankLine = true;
                    current.Add(string.Empty);
                }
                else
                {
                    FlushParagraph(paragraphs, current);
                    holdingBlankLine = false;
                }
                continue;
            }

            // A held blank line only delimits the paragraph if the next line is not another list item
            if (holdingBlankLine)
            {
                holdingBlankLine = false;
                if (!IsListItemLine(line))
                    FlushParagraph(paragraphs, current);
            }

            current.Add(line);

            // A code fence opens a code block which is kept as a single paragraph
            if (TryParseCodeFence(line, out var fenceCharacter, out var fenceLength))
            {
                inCodeBlock = true;
                codeFenceCharacter = fenceCharacter;
                codeFenceLength = fenceLength;
            }
        }

        FlushParagraph(paragraphs, current);
        return paragraphs;
    }

    /// <summary>
    /// Merge a list of paragraphs back together, joining them with blank lines,
    /// such that each resulting string is at most <paramref name="maxLength"/> characters long
    /// </summary>
    /// <remarks>
    /// Paragraphs are merged greedily in order. A paragraph that is longer than
    /// <paramref name="maxLength"/> on its own is kept as its own entry and will exceed the max length.
    /// </remarks>
    /// <param name="paragraphs">The paragraphs to merge</param>
    /// <param name="maxLength">The maximum length of each merged string</param>
    /// <returns>The paragraphs merged together</returns>
    public static List<string> MergeParagraphs(List<string> paragraphs, int maxLength)
    {
        var result = new List<string>();
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var separatorLength = current.Length == 0 ? 0 : 2;

            if (current.Length + separatorLength + paragraph.Length <= maxLength)
            {
                if (current.Length > 0)
                    current.Append("\n\n");
                current.Append(paragraph);
            }
            else
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                current.Append(paragraph);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    /// <summary>
    /// Check if a line is a bullet point or numbered list item
    /// </summary>
    private static bool IsListItemLine(string line)
    {
        return BulletListItemPattern.IsMatch(line) || NumberedListItemPattern.IsMatch(line);
    }

    /// <summary>
    /// Check if the last non-blank line of a set of lines is a list item
    /// </summary>
    private static bool EndsWithListItem(IReadOnlyList<string> lines)
    {
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return IsListItemLine(lines[i]);
        }

        return false;
    }

    /// <summary>
    /// Try to parse a line as a code fence (i.e. the start of a code block)
    /// </summary>
    private static bool TryParseCodeFence(string line, out char fenceCharacter, out int fenceLength)
    {
        fenceCharacter = '\0';
        fenceLength = 0;

        var indent = 0;
        while (indent < line.Length && line[indent] == ' ')
            indent++;

        if (indent > 3 || indent >= line.Length)
            return false;

        var character = line[indent];
        if (character is not ('`' or '~'))
            return false;

        var length = 0;
        while (indent + length < line.Length && line[indent + length] == character)
            length++;

        if (length < 3)
            return false;

        fenceCharacter = character;
        fenceLength = length;
        return true;
    }

    /// <summary>
    /// Check if a line closes an open code block
    /// </summary>
    private static bool IsClosingCodeFence(string line, char fenceCharacter, int minLength)
    {
        var indent = 0;
        while (indent < line.Length && line[indent] == ' ')
            indent++;

        if (indent > 3)
            return false;

        var content = line[indent..];
        return content.Length >= minLength && content.All(c => c == fenceCharacter);
    }

    /// <summary>
    /// Add the current paragraph to the list of paragraphs
    /// </summary>
    private static void FlushParagraph(List<string> paragraphs, List<string> current)
    {
        if (current.Count == 0)
            return;

        paragraphs.Add(string.Join('\n', current).TrimEnd('\n'));
        current.Clear();
    }
}

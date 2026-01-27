using System.Text;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions to <see cref="StringBuilder"/>
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    /// Append a char span and then a newline
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="span"></param>
    /// <returns></returns>
    public static StringBuilder AppendLine(this StringBuilder builder, ReadOnlySpan<char> span)
    {
        builder.Append(span);
        builder.AppendLine();
        return builder;
    }
}
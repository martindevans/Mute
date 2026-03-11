namespace Mute.Moe.Utilities;

/// <summary>
/// An edit action on a string
/// </summary>
/// <param name="Type">The type of action</param>
/// <param name="Position">The location of the action</param>
/// <param name="Text">Either the text being inserted, or the text being deleted.</param>
/// <param name="EditorId">Who made this change</param>
public readonly record struct StringEdit(StringEditType Type, int Position, string Text, ulong EditorId)
{
    /// <summary>
    /// Apply this edit to a string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public string Apply(string input)
    {
        return Type switch
        {
            StringEditType.Insert => input.Insert(Position, Text),
            StringEditType.Delete => input.Remove(Position, Text.Length),
            _ => throw new InvalidOperationException($"Unknown string edit type: '{Type}'"),
        };
    }

    /// <summary>
    /// Remove this edit from a string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public string Unapply(string input)
    {
        return Type switch
        {
            StringEditType.Insert => input.Remove(Position, Text.Length),
            StringEditType.Delete => input.Insert(Position, Text),
            _ => throw new InvalidOperationException($"Unknown string edit type: '{Type}'"),
        };
    }
}

/// <summary>
/// The type of edit made to a string
/// </summary>
public enum StringEditType
{
    /// <summary>
    /// Some next text was inserted at a location
    /// </summary>
    Insert,

    /// <summary>
    /// Some text was deleted from a location
    /// </summary>
    Delete
}
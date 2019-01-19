namespace Mute.Moe.Discord.Services.Responses.Eliza.Topics
{
    /// <summary>
    /// A message from the user (annotated with metadata)
    /// </summary>
    public interface IUtterance
    {
        string Content { get; }
    }
}

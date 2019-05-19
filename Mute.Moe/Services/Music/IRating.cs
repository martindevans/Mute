namespace Mute.Moe.Services.Music
{
    /// <summary>
    /// A rating given by a user for a track
    /// </summary>
    public interface IRating
    {
        ulong UserId { get; }

        ulong TrackId { get; }

        Rating Rating { get; }
    }
}

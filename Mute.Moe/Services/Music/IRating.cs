namespace Mute.Moe.Services.Music
{
    public interface IRating
    {
        ulong UserId { get; }

        ulong TrackId { get; }

        byte Rating { get; }
    }
}

namespace Mute.Moe.Services.Music
{
    public interface IArtist
    {
        ulong ArtistId { get; }

        string Name { get; }

        string Url { get; }
    }
}

using FluidCaching;
using Mute.Anilist;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Anime info using Mute.Anilist package
/// </summary>
public class MuteAnilistInfoService
    : IAnimeInfo, IMangaInfo, ICharacterInfo
{
    private readonly AniListClient _client;

    private readonly FluidCache<MediaCacheItem> _mediaCache;
    private readonly IIndex<long, MediaCacheItem> _mediaById;

    private readonly FluidCache<CharacterCacheItem> _characterCache;
    private readonly IIndex<long, CharacterCacheItem> _characterById;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="http"></param>
    public MuteAnilistInfoService(IHttpClientFactory http)
    {
        _client = new AniListClient(http.CreateClient());

        _mediaCache = new FluidCache<MediaCacheItem>(1024, TimeSpan.Zero, TimeSpan.FromDays(1), () => DateTime.UtcNow);
        _mediaById = _mediaCache.AddIndex("byId", media => media.Id);

        _characterCache = new FluidCache<CharacterCacheItem>(1024, TimeSpan.Zero, TimeSpan.FromDays(1), () => DateTime.UtcNow);
        _characterById = _characterCache.AddIndex("byId", character => character.Id);
    }

    #region Media (anime/manga)
    private async Task<Media?> GetMediaInfoAsync(string title, MediaType type)
    {
        return await _client
            .SearchMediaAsync(title)
            .Select(CacheMedia)
            .Where(a => a.Type == type)
            .OrderBy(i => TitleDistance(i, title))
            .FirstOrDefaultAsync();
    }

    private IAsyncEnumerable<Media> GetMediasInfoAsync(string search, int limit, MediaType type)
    {
        return _client
            .SearchMediaAsync(search)
            .Select(CacheMedia)
            .Where(a => a.Type == type)
            .Take(limit);
    }

    /// <summary>
    /// Get an anime item by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IAnime?> GetAnimeInfoAsync(long id)
    {
        var item = await _mediaById.GetItem(id, GetMediaItemUncached);

        if (item?.Item == null)
            return null;
        if (item.Item.Type != MediaType.Anime)
            return null;

        return new AnimeMedia(item.Item);
    }

    /// <summary>
    /// Get a manga item by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IManga?> GetMangaInfoAsync(long id)
    {
        var item = await _mediaById.GetItem(id, GetMediaItemUncached);

        if (item?.Item == null)
            return null;
        if (item.Item.Type != MediaType.Manga)
            return null;

        return new MangaMedia(item.Item);
    }

    private async Task<MediaCacheItem> GetMediaItemUncached(long key)
    {
        return new MediaCacheItem(
            key,
            await _client.GetMediaByIdAsync(checked((int)key))
        );
    }

    /// <inheritdoc />
    public async Task<IAnime?> GetAnimeInfoAsync(string title)
    {
        var result = await GetMediaInfoAsync(title, MediaType.Anime);
        if (result == null)
            return null;

        return new AnimeMedia(result);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAnime> GetAnimesInfoAsync(string search, int limit)
    {
        return GetMediasInfoAsync(search, limit, MediaType.Anime)
            .Select(a => new AnimeMedia(a));
    }

    /// <inheritdoc />
    public async Task<IManga?> GetMangaInfoAsync(string title)
    {
        var result = await GetMediaInfoAsync(title, MediaType.Anime);
        if (result == null)
            return null;

        return new MangaMedia(result);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IManga> GetMangasInfoAsync(string search, int limit)
    {
        return GetMediasInfoAsync(search, limit, MediaType.Manga)
           .Select(a => new MangaMedia(a));
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAnime> GetSeasonAnimes(int year, int season)
    {
        var mediaSeason = season switch
        {
            0 => MediaSeason.Winter,
            1 => MediaSeason.Spring,
            2 => MediaSeason.Summer,
            3 => MediaSeason.Fall,
            _ => throw new ArgumentOutOfRangeException(nameof(season), "Season must be 0 to 3 (inclusive)"),
        };

        return _client
            .GetSeasonalMediaAsync(mediaSeason, year)
            .Select(CacheMedia)
            .Select(a => new AnimeMedia(a));
    }

    private async ValueTask<Media> CacheMedia(Media media, CancellationToken cancellation)
    {
        // Get the item from the cache
        var cachedMedia = await _mediaById.GetItem(media.Id);
        if (cachedMedia?.Item != null)
            return cachedMedia.Item;

        // Add the media item to the cache
        _mediaCache.Add(new MediaCacheItem(media.Id, media));

        // Cache each of the characters referred to by this media
        foreach (var edge in media.Characters?.Edges ?? [ ])
        {
            if (edge.Node == null)
                continue;
            await CacheCharacter(edge.Node, cancellation);
        }

        return media;
    }

    private static uint TitleDistance(Media media, string searchTerm)
    {
        var title = media.Title?.English
                 ?? media.Title?.Romaji
                 ?? media.Title?.Native
                 ?? "";
        title = title.ToLowerInvariant();

        if (title.Equals(searchTerm))
            return 0;
        if (title.Contains(searchTerm))
            return 1;
        return title.Levenshtein(searchTerm) + 1;
    }

    private class AnimeMedia(Media media)
        : IAnime
    {
        public long Id => media.Id;

        public string? TitleEnglish => media.Title?.English ?? media.Title?.Romaji;
        public string? TitleJapanese => media.Title?.Romaji ?? media.Title?.Native;

        public string Description => media.Description ?? "";
        public bool Adult => media.IsAdult;

        public string Url => media.SiteUrl ?? "";
        public string ImageUrl => media.CoverImage?.LargeUrl ?? media.CoverImage?.MediumUrl ?? media.CoverImage?.ExtraLargeUrl ?? "";

        public DateTimeOffset? StartDate => media.StartDate?.ToDateTimeOffset();
        public DateTimeOffset? EndDate => media.EndDate?.ToDateTimeOffset();

        public IReadOnlyList<string> Genres => media.Genres ?? [ ];
        public uint? TotalEpisodes => (uint?)media.Episodes;
    }

    private class MangaMedia(Media media)
        : IManga
    {
        public long Id => media.Id;
        public string? TitleEnglish => media.Title?.English ?? media.Title?.Romaji;
        public string? TitleJapanese => media.Title?.Romaji ?? media.Title?.Native;
        public string Description => media.Description ?? "";
        public string Url => media.SiteUrl ?? "";
        public string ImageUrl => media.CoverImage?.LargeUrl ?? media.CoverImage?.MediumUrl ?? media.CoverImage?.ExtraLargeUrl ?? "";
    }
    #endregion

    #region Characters
    /// <summary>
    /// Get info about a character by their ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<ICharacter?> GetCharacterInfoAsync(long id)
    {
        var item = await _characterById.GetItem(id, GetCharacterInfoUncached);

        if (item?.Item == null)
            return null;

        return new CharacterWrap(item.Item);
    }

    private async Task<CharacterCacheItem> GetCharacterInfoUncached(long key)
    {
        return new CharacterCacheItem(
            key,
            await _client.GetCharacterByIdAsync((int)key)
        );
    }

    /// <inheritdoc />
    public async Task<ICharacter?> GetCharacterInfoAsync(string search)
    {
        var character = await _client
                    .SearchCharactersAsync(search)
                    .Select(CacheCharacter)
                    .OrderBy(i => NameDistance(i, search))
                    .FirstOrDefaultAsync();
        if (character == null)
            return null;

        return new CharacterWrap(character);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ICharacter> GetCharactersInfoAsync(string search)
    {
        return _client
            .SearchCharactersAsync(search)
            .Select(CacheCharacter)
            .Select(a => new CharacterWrap(a));
    }

    private async ValueTask<Character> CacheCharacter(Character character, CancellationToken cancellation)
    {
        var cachedCharacter = await _characterById.GetItem(character.Id);
        if (cachedCharacter != null)
            return cachedCharacter.Item!;

        _characterCache.Add(new CharacterCacheItem(character.Id, character));
        return character;
    }

    private static uint NameDistance(Character character, string search)
    {
        var dist = uint.MaxValue;

        dist = Math.Min(dist, Dist(character.Name?.Full, search));
        dist = Math.Min(dist, Dist(character.Name?.Native, search));
        foreach (var alt in character.Name?.Alternative ?? [])
            dist = Math.Min(dist, Dist(alt, search));

        return dist;

        static uint Dist(string? name, string search)
        {
            if (name == null)
                return int.MaxValue;

            if (name.Equals(search, StringComparison.InvariantCultureIgnoreCase))
                return 0;
            return name.Levenshtein(search);
        }
    }

    private class CharacterWrap(Character character)
        : ICharacter
    {
        public long Id => character.Id;
        public string GivenName => character.Name?.First ?? character.Name?.Full ?? character.Name?.Native ?? "";
        public string FamilyName => character.Name?.Last ?? character.Name?.Full ?? character.Name?.Native ?? "";
        public string Description => character.Description ?? "";
        public string Url => character.Description ?? "";
        public string ImageUrl => character.Image?.Large ?? character.Image?.Medium ?? "";
    }
    #endregion

    private record MediaCacheItem(long Id, Media? Item);
    private record CharacterCacheItem(long Id, Character? Item);
}
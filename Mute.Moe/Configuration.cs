namespace Mute.Moe;

/// <summary>
/// Bot configuration, loaded from config.json file
/// </summary>
public class Configuration
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [UsedImplicitly] public AuthConfig? Auth;
    [UsedImplicitly] public AvatarConfig? Avatar;
    [UsedImplicitly] public AlphaAdvantageConfig? AlphaAdvantage;
    [UsedImplicitly] public CoinMarketCapConfig? CoinMarketCap;
    [UsedImplicitly] public DatabaseConfig? Database;
    [UsedImplicitly] public UrbanDictionaryConfig? UrbanDictionary;
    [UsedImplicitly] public STTConfig? STT;
    [UsedImplicitly] public LLMConfig? LLM;
    [UsedImplicitly] public Automatic1111Config? Automatic1111;
    [UsedImplicitly] public GlobalImageGenerationConfig? ImageGeneration;
    [UsedImplicitly] public LocationConfig? Location;
    [UsedImplicitly] public OpenWeatherMapConfig? OpenWeatherMap;
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Set if the bot process messages from itself
    /// </summary>
    [UsedImplicitly] public bool ProcessMessagesFromSelf = false;

    /// <summary>
    /// Set the command prefix character
    /// </summary>
    [UsedImplicitly] public char PrefixCharacter = '!';
}

/// <summary>
/// Configuration for avatars
/// </summary>
public class AvatarConfig
{
    /// <summary>
    /// A set of avatars, which applies within a certain date range
    /// </summary>
    public class AvatarSet
    {
        /// <summary>
        /// Start day-of-year (inclusive) that this avatar can be used
        /// </summary>
        [UsedImplicitly] public int StartDay;

        /// <summary>
        /// End day-of-years (inclusive) that this avatar can be used
        /// </summary>
        [UsedImplicitly] public int EndDay;

        /// <summary>
        /// Path to a directory that contains images
        /// </summary>
        [UsedImplicitly] public string? Path;

        /// <summary>
        /// If any active avatar sets are exclusive, then all non-exclusive sets are disabled
        /// </summary>
        [UsedImplicitly] public bool Exclusive;
    }

    /// <summary>
    /// All sets of avatars
    /// </summary>
    [UsedImplicitly] public AvatarSet[]? Avatars;
}

/// <summary>
/// Discord Auth
/// </summary>
public class AuthConfig
{
    /// <summary>
    /// Discord API token
    /// </summary>
    [UsedImplicitly] public string? Token;

    /// <summary>
    /// Discord API client ID
    /// </summary>
    [UsedImplicitly] public string? ClientId;
}

/// <summary>
/// Alpha Advantage API service config
/// </summary>
public class AlphaAdvantageConfig
{
    /// <summary>
    /// Key for Alpha Advantage API
    /// </summary>
    [UsedImplicitly] public string? Key;

    /// <summary>
    /// Number of items to store in cache
    /// </summary>
    [UsedImplicitly] public int CacheSize = 128;

    /// <summary>
    /// Maximum age before items timeout from the cache.
    /// </summary>
    [UsedImplicitly] public int CacheMaxAgeSeconds = (int)TimeSpan.FromHours(6).TotalSeconds;
}

/// <summary>
/// CoinMarketCap API service config
/// </summary>
public class CoinMarketCapConfig
{
    /// <summary>
    /// Key for Coin Market Cap API
    /// </summary>
    [UsedImplicitly] public string? Key;

    /// <summary>
    /// Number of items to store in cache
    /// </summary>
    [UsedImplicitly] public int CacheSize = 128;

    /// <summary>
    /// Maximum age before items timeout from the cache.
    /// </summary>
    [UsedImplicitly] public int CacheMaxAgeSeconds = (int)TimeSpan.FromHours(6).TotalSeconds;
}

/// <summary>
/// SQLite database config
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// SQLite connection string
    /// </summary>
    [UsedImplicitly] public string? ConnectionString;
}

/// <summary>
/// Urban dictionary API service config
/// </summary>
public class UrbanDictionaryConfig
{
    /// <summary>
    /// Number of items to store in cache
    /// </summary>
    [UsedImplicitly] public uint CacheSize;

    /// <summary>
    /// Maximum age before items timeout from the cache.
    /// </summary>
    [UsedImplicitly] public uint CacheMaxTimeSeconds;
}

/// <summary>
/// Config for Speech-To-Text
/// </summary>
public class STTConfig
{
    /// <summary>
    /// Config for Whisper Speech-To-Text
    /// </summary>
    [UsedImplicitly] public WhisperConfig? Whisper;

    /// <summary>
    /// Config for Whisper Speech-To-Text
    /// </summary>
    public class WhisperConfig
    {
        /// <summary>
        /// Absolute path of the whisper model
        /// </summary>
        [UsedImplicitly] public string? ModelPath;

        /// <summary>
        /// Number of threads to use
        /// </summary>
        [UsedImplicitly] public uint? Threads;
    }
}

/// <summary>
/// Config for large language models
/// </summary>
public class LLMConfig
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [UsedImplicitly] public GoogleConfig? Google;
    [UsedImplicitly] public OpenAIConfig? OpenAI;
    [UsedImplicitly] public SelfHostConfig? SelfHost;

    public class GoogleConfig
    {
        [UsedImplicitly] public string? Key;
    }

    public class OpenAIConfig
    {
        [UsedImplicitly] public string? Key;
    }

    public class SelfHostConfig
    {
        [UsedImplicitly] public LocalModelEndpoint? ChatLanguageModel = null;
        [UsedImplicitly] public LocalModelEndpoint? VisionLanguageModel = null;
        [UsedImplicitly] public LocalEmbeddingModelEndpoint? EmbeddingModel = null;
        [UsedImplicitly] public LocalModelEndpoint? RerankingModel = null;
    }

    public class LocalModelEndpoint
    {
        [UsedImplicitly] public required string Endpoint;
        [UsedImplicitly] public required string Key;
        [UsedImplicitly] public required string ModelName;

        [UsedImplicitly] public required int ContextSize;
    }

    public class LocalEmbeddingModelEndpoint
        : LocalModelEndpoint
    {
        [UsedImplicitly] public int EmbeddingDims;
    }
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// Config for Automatic1111 image generation backends
/// </summary>
public class Automatic1111Config
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [UsedImplicitly] public Backend[] Backends = null!;

    [UsedImplicitly] public string? Text2ImageSampler;
    [UsedImplicitly] public string? Image2ImageSampler;
    [UsedImplicitly] public int? SamplerSteps;
    [UsedImplicitly] public int? OutpaintSteps;
    [UsedImplicitly] public string? Checkpoint;
    [UsedImplicitly] public uint? Width;
    [UsedImplicitly] public uint? Height;
    [UsedImplicitly] public string? Upscaler;

    [UsedImplicitly] public int? GenerationTimeOutSeconds;
    [UsedImplicitly] public int? FastTimeOutSeconds;

    [UsedImplicitly] public uint? Image2ImageClipSkip;
    [UsedImplicitly] public uint? Text2ImageClipSkip;
    [UsedImplicitly] public int? RecheckDeadBackendTime;

    [UsedImplicitly] public ADetailer? AfterDetail;

    [UsedImplicitly] public class ADetailer
    {
        [UsedImplicitly] public float? HandMinSize;
        [UsedImplicitly] public float? FaceMinSize;
    }

    public class Backend
    {
        [UsedImplicitly] public bool Enabled;
        [UsedImplicitly] public string? Url;

        [UsedImplicitly] public int? GenerationTimeOutSeconds;
        [UsedImplicitly] public int? FastTimeOutSeconds;
        [UsedImplicitly] public float? StepsMultiplier;
    }
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// General config for image generation (non A1111 specific)
/// </summary>
public class GlobalImageGenerationConfig
{
    /// <summary>
    /// How many images to generate per request
    /// </summary>
    [UsedImplicitly] public int? BatchSize;
}

/// <summary>
/// Provide the bot with it's approximate physical location on Earth
/// </summary>
public class LocationConfig
{
    /// <summary>
    /// Location Latitude
    /// </summary>
    [UsedImplicitly] public float Latitude;

    /// <summary>
    /// Location Longitude
    /// </summary>
    [UsedImplicitly] public float Longitude;
}

/// <summary>
/// Config for OpenWeatherMap API service
/// </summary>
public class OpenWeatherMapConfig
{
    /// <summary>
    /// API key for OpenWeatherMap
    /// </summary>
    [UsedImplicitly] public string? ApiKey;

    /// <summary>
    /// Max items in cache
    /// </summary>
    [UsedImplicitly] public int CacheSize = 32;

    /// <summary>
    /// Max age of items before they must be culled
    /// </summary>
    [UsedImplicitly] public int CacheMaxAgeSeconds = (int)TimeSpan.FromMinutes(7).TotalSeconds;
}
using System.Diagnostics.CodeAnalysis;
using Mute.Moe.Services.LLM;

namespace Mute.Moe;

/// <summary>
/// Bot configuration, loaded from config.json file
/// </summary>
[ExcludeFromCodeCoverage]
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
    [UsedImplicitly] public required AgentConfig Agent;
    [UsedImplicitly] public Automatic1111Config? Automatic1111;
    [UsedImplicitly] public GlobalImageGenerationConfig? ImageGeneration;
    [UsedImplicitly] public LocationConfig? Location;
    [UsedImplicitly] public OpenWeatherMapConfig? OpenWeatherMap;
    [UsedImplicitly] public BraveWebSearchConfig? BraveWebSearch;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Set if the bot process messages from itself
    /// </summary>
    [UsedImplicitly] public bool ProcessMessagesFromSelf;

    /// <summary>
    /// Set the command prefix character
    /// </summary>
    [UsedImplicitly] public char PrefixCharacter = '!';

    /// <summary>
    /// Prefix characters which will cause a message to be completely ignored
    /// </summary>
    [UsedImplicitly] public char[] IgnorePrefixCharacters = [ ];
}

/// <summary>
/// Configuration for avatars
/// </summary>
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
public class DatabaseConfig
{
    /// <summary>
    /// SQLite connection string
    /// </summary>
    [UsedImplicitly] public string? ConnectionString;

    /// <summary>
    /// SQLite connection string for backup
    /// </summary>
    [UsedImplicitly] public string? BackupConnectionString;
}

/// <summary>
/// Urban dictionary API service config
/// </summary>
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
public class LLMConfig
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [UsedImplicitly] public required LLamaServerEndpoint[] Endpoints;

    [UsedImplicitly] public required LocalModel ChatLanguageModel;
    [UsedImplicitly] public required LocalModel FactLanguageModel;
    [UsedImplicitly] public required LocalModel VisionLanguageModel;
    [UsedImplicitly] public required LocalEmbeddingModel EmbeddingModel;
    [UsedImplicitly] public required LocalModel RerankingModel;

    [UsedImplicitly] public required string ChatSystemPromptPath;

    public class LocalModel
    {
        [UsedImplicitly] public required string ModelName;
        [UsedImplicitly] public required int ContextSize;

        [UsedImplicitly] public SamplingParameters? Sampling;
    }

    public class LocalEmbeddingModel
        : LocalModel
    {
        [UsedImplicitly] public int EmbeddingDims;
    }

    public class LLamaServerEndpoint
    {
        [UsedImplicitly] public required string ID;
        [UsedImplicitly] public required string Endpoint;
        [UsedImplicitly] public required string Key;

        [UsedImplicitly] public int Slots = 4;
        [UsedImplicitly] public string HealthCheck = "health";

        [UsedImplicitly] public string[] ModelsBlacklist = [ ];
    }
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// Config for agent stuff
/// </summary>
[ExcludeFromCodeCoverage]
public class AgentConfig
{
    /// <summary>
    /// Memory decay over time settings
    /// </summary>
    [UsedImplicitly] public required MemoryDecayConfig MemoryDecay;

    /// <summary>
    /// Extracting facts from conversation transcripts
    /// </summary>
    [UsedImplicitly] public required FactExtractionConfig FactExtraction;

    /// <summary>
    /// Config for memory decay
    /// </summary>
    [UsedImplicitly]
    public class MemoryDecayConfig
    {
        /// <summary>
        /// Time of day (hour) to apply decay
        /// </summary>
        [UsedImplicitly] public int? Hour;

        /// <summary>
        /// Time of day (minute) to apply decay
        /// </summary>
        [UsedImplicitly] public int? Minute;

        /// <summary>
        /// Time of day (second) to apply decay
        /// </summary>
        [UsedImplicitly] public int? Second;

        /// <summary>
        /// All memories with logit below this value will decay
        /// </summary>
        [UsedImplicitly] public float Threshold;

        /// <summary>
        /// Amount to decay logits by (additive)
        /// </summary>
        [UsedImplicitly] public float DecayValue = 0.01f;
    }

    /// <summary>
    /// Config for memory extraction
    /// </summary>
    public class FactExtractionConfig
    {
        /// <summary>
        /// Path to the system prompt to use for fact extraction
        /// </summary>
        [UsedImplicitly] public required string SystemPromptFacts;
    }
}

/// <summary>
/// Config for brave web search
/// </summary>
[ExcludeFromCodeCoverage]
public class BraveWebSearchConfig
{
    /// <summary>
    /// API key for brave web search
    /// </summary>
    [UsedImplicitly] public string ApiKey = "";
}

/// <summary>
/// Config for Automatic1111 image generation backends
/// </summary>
[ExcludeFromCodeCoverage]
public class Automatic1111Config
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [UsedImplicitly] public Backend[] Backends = null!;

    [UsedImplicitly] public string? Text2ImageSampler;
    [UsedImplicitly] public string? Text2ImageScheduler;
    [UsedImplicitly] public float? Text2ImageGuidanceScale;
    [UsedImplicitly] public string[]? Text2ImageLoras;

    [UsedImplicitly] public string? Image2ImageSampler;
    [UsedImplicitly] public string? Image2ImageScheduler;
    [UsedImplicitly] public float? Image2ImageGuidanceScale;
    [UsedImplicitly] public string[]? Image2ImageLoras;

    [UsedImplicitly] public int? SamplerSteps;
    [UsedImplicitly] public int? OutpaintSteps;
    [UsedImplicitly] public string? Checkpoint;
    [UsedImplicitly] public uint? Width;
    [UsedImplicitly] public uint? Height;
    [UsedImplicitly] public string? Upscaler;

    [UsedImplicitly] public string ExtraPositive = "";
    [UsedImplicitly] public string ExtraNegative = "";

    [UsedImplicitly] public int? GenerationTimeOutSeconds;
    [UsedImplicitly] public int? FastTimeOutSeconds;

    [UsedImplicitly] public uint? Image2ImageClipSkip;
    [UsedImplicitly] public uint? Text2ImageClipSkip;
    [UsedImplicitly] public int? RecheckDeadBackendTime;

    public class Backend
    {
        [UsedImplicitly] public bool Enabled;
        [UsedImplicitly] public string? Url;
        [UsedImplicitly] public string? PingEndpoint;
        [UsedImplicitly] public bool EnableProgress = true;

        [UsedImplicitly] public int? GenerationTimeOutSeconds;
        [UsedImplicitly] public int? FastTimeOutSeconds;
        [UsedImplicitly] public float? StepsMultiplier;
    }
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// General config for image generation (non A1111 specific)
/// </summary>
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
[ExcludeFromCodeCoverage]
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
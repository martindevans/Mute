using JetBrains.Annotations;

namespace Mute.Moe;

public class Configuration
{
    [UsedImplicitly] public AuthConfig? Auth;
    [UsedImplicitly] public AvatarConfig? Avatar;
    [UsedImplicitly] public AlphaAdvantageConfig? AlphaAdvantage;
    [UsedImplicitly] public CoinMarketCapConfig? CoinMarketCap;
    [UsedImplicitly] public DatabaseConfig? Database;
    [UsedImplicitly] public SteamConfig? Steam;
    [UsedImplicitly] public DictionaryConfig? Dictionary;
    [UsedImplicitly] public SentimentReactionConfig? SentimentReactions;
    [UsedImplicitly] public UrbanDictionaryConfig? UrbanDictionary;
    [UsedImplicitly] public STTConfig? STT;
    [UsedImplicitly] public LLMConfig? LLM;
    [UsedImplicitly] public Automatic1111Config? Automatic1111;
    [UsedImplicitly] public GlobalImageGenerationConfig? ImageGeneration;
    [UsedImplicitly] public LocationConfig? Location;
    [UsedImplicitly] public OpenWeatherMapConfig? OpenWeatherMap;

    [UsedImplicitly] public bool ProcessMessagesFromSelf;
    [UsedImplicitly] public char PrefixCharacter = '!';
}

public class AvatarConfig
{
    public class AvatarSet
    {
        [UsedImplicitly] public int StartDay;
        [UsedImplicitly] public int EndDay;
        [UsedImplicitly] public string? Path;
        [UsedImplicitly] public bool Exclusive;
    }

    [UsedImplicitly] public AvatarSet[]? Avatars;
}

public class AuthConfig
{
    [UsedImplicitly] public string? Token;
    [UsedImplicitly] public string? ClientId;
}

public class AlphaAdvantageConfig
{
    [UsedImplicitly] public string? Key;

    [UsedImplicitly] public int CacheSize = 128;
    [UsedImplicitly] public int CacheMinAgeSeconds = (int)TimeSpan.FromMinutes(5).TotalSeconds;
    [UsedImplicitly] public int CacheMaxAgeSeconds = (int)TimeSpan.FromHours(6).TotalSeconds;
}

public class CoinMarketCapConfig
{
    [UsedImplicitly] public string? Key;

    [UsedImplicitly] public int CacheSize = 4096;
    [UsedImplicitly] public int CacheMinAgeSeconds = (int)TimeSpan.FromMinutes(5).TotalSeconds;
    [UsedImplicitly] public int CacheMaxAgeSeconds = (int)TimeSpan.FromHours(6).TotalSeconds;
}

public class DatabaseConfig
{
    [UsedImplicitly] public string? ConnectionString;
}

public class SteamConfig
{
    [UsedImplicitly] public string? WebApiKey;
}

public class DictionaryConfig
{
    [UsedImplicitly] public string? WordListPath;
}

public class SentimentReactionConfig
{
    [UsedImplicitly] public double CertaintyThreshold = 0.8;
    [UsedImplicitly] public double ReactionChance = 0.05;
    [UsedImplicitly] public double MentionReactionChance = 0.25;
}

public class UrbanDictionaryConfig
{
    [UsedImplicitly] public uint CacheSize;
    [UsedImplicitly] public uint CacheMinTimeSeconds;
    [UsedImplicitly] public uint CacheMaxTimeSeconds;
}

public class STTConfig
{
    public class WhisperConfig
    {
        [UsedImplicitly] public string? ModelPath;
        [UsedImplicitly] public uint? Threads;
    }
    
    [UsedImplicitly] public WhisperConfig? Whisper;
}

public class LLMConfig
{
    [UsedImplicitly] public string? ModelPath;
}

public class Automatic1111Config
{
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
}

public class GlobalImageGenerationConfig
{
    [UsedImplicitly] public int? BatchSize;
}

public class LocationConfig
{
    [UsedImplicitly] public float Latitude;
    [UsedImplicitly] public float Longitude;
}

public class OpenWeatherMapConfig
{
    [UsedImplicitly] public string? ApiKey;
}
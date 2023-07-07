using JetBrains.Annotations;
using Microsoft.ML.OnnxRuntime;

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
    [UsedImplicitly] public TTSConfig? TTS;
    [UsedImplicitly] public STTConfig? STT;
    [UsedImplicitly] public MusicLibraryConfig? MusicLibrary;
    [UsedImplicitly] public LLMConfig? LLM;
    [UsedImplicitly] public ONNXConfig? ONNX;
    [UsedImplicitly] public Automatic1111Config? Automatic1111;
    [UsedImplicitly] public GlobalImageGenerationConfig? ImageGeneration;

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

public class TTSConfig
{
}

public class STTConfig
{
    public class WhisperConfig
    {
        [UsedImplicitly] public string? ModelPath;
        [UsedImplicitly] public uint? Threads;
    }
    
    public WhisperConfig? Whisper;
}

public class MusicLibraryConfig
{
    [UsedImplicitly] public string? MusicFolder;
}

public class LLMConfig
{
    [UsedImplicitly] public int? MaxTokens;
    [UsedImplicitly] public float? TopP;
    [UsedImplicitly] public float? Temperature;
    [UsedImplicitly] public float? TopK;
}

public class ONNXConfig
{
    public int? CudaDevice = null;

    public SessionOptions? GetOptions()
    {
        if (CudaDevice is null or < 0)
            return null;

        return SessionOptions.MakeSessionOptionWithCudaProvider(CudaDevice.Value);
    }
}

public class Automatic1111Config
{
    public string[] Urls = null!;
    public string? Text2ImageSampler = null;
    public string? Image2ImageSampler = null;
    public int? SamplerSteps = null;
    public string? Checkpoint = null;
    public uint? Width = null;
    public uint? Height = null;
    public string? Upscaler = null;
    public int? GenerationTimeOutSeconds = null;
}

public class GlobalImageGenerationConfig
{
    public int? BatchSize;
}
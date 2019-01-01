using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mute
{
    public class Configuration
    {
        [UsedImplicitly] public AuthConfig Auth;
        [UsedImplicitly] public AlphaAdvantageConfig AlphaAdvantage;
        [UsedImplicitly] public DatabaseConfig Database;
        [UsedImplicitly] public YoutubeDlConfig YoutubeDl;
        [UsedImplicitly] public SentimentConfig Sentiment;
        [UsedImplicitly] public ElizaConfig ElizaConfig;
        [UsedImplicitly] public SteamConfig Steam;
        [UsedImplicitly] public SoundEffectConfig SoundEffects;
        [UsedImplicitly] public DictionaryConfig Dictionary;
        [UsedImplicitly] public SentimentReactionConfig SentimentReactions;
        [UsedImplicitly] public WordVectorsConfig WordVectors;

        [UsedImplicitly] public bool ProcessMessagesFromSelf;
    }

    public class AuthConfig
    {
        [UsedImplicitly] public string Token;
    }

    public class AlphaAdvantageConfig
    {
        [UsedImplicitly] public string Key;
    }

    public class DatabaseConfig
    {
        [UsedImplicitly] public string ConnectionString;
    }

    public class YoutubeDlConfig
    {
        [UsedImplicitly] public string RateLimit;
        [UsedImplicitly] public string InProgressDownloadFolder;
        [UsedImplicitly] public string CompleteDownloadFolder;

        [UsedImplicitly] public string YoutubeDlBinaryPath;
        [UsedImplicitly] public string FprobeBinaryPath;
    }

    public class SentimentConfig
    {
        [UsedImplicitly] public string SentimentModelPath;
    }

    public class ElizaConfig
    {
        [UsedImplicitly] public List<string> Scripts;
    }

    public class SteamConfig
    {
        [UsedImplicitly] public string WebApiKey;
    }

    public class SoundEffectConfig
    {
        [UsedImplicitly] public string SfxFolder;
    }

    public class DictionaryConfig
    {
        [UsedImplicitly] public string WordListPath;
    }

    public class SentimentReactionConfig
    {
        [UsedImplicitly] public double CertaintyThreshold = 0.8;
        [UsedImplicitly] public double ReactionChance = 0.05;
        [UsedImplicitly] public double MentionReactionChance = 0.25;
    }

    public class WordVectorsConfig
    {
        [UsedImplicitly] public string WordVectorsBaseUrl;
        [UsedImplicitly] public uint CacheSize;
        [UsedImplicitly] public uint CacheMinTimeSeconds;
        [UsedImplicitly] public uint CacheMaxTimeSeconds;
    }
}

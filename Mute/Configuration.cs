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
        [UsedImplicitly] public MlConfig MlConfig;
        [UsedImplicitly] public ElizaConfig ElizaConfig;
        [UsedImplicitly] public SteamConfig Steam;
        [UsedImplicitly] public SoundEffectConfig SoundEffects;
        [UsedImplicitly] public DictionaryConfig Dictionary;
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

    public class MlConfig
    {
        [UsedImplicitly] public string BaseModelPath;
        [UsedImplicitly] public string BaseDatasetsPath;
        [UsedImplicitly] public string TempTrainingCache;

        [UsedImplicitly] public SentimentConfig Sentiment;
    }

    public class SentimentConfig
    {
        [UsedImplicitly] public string ModelDirectory;
        [UsedImplicitly] public string TrainingDatasetDirectory;
        [UsedImplicitly] public string EvalDatasetDirectory;
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
}

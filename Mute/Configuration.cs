namespace Mute
{
    public class Configuration
    {
        public AuthConfig Auth;
        public AlphaAdvantageConfig AlphaAdvantage;
        public DatabaseConfig Database;
        public YoutubeDlConfig YoutubeDl;
        public MlConfig MlConfig;
    }

    public class AuthConfig
    {
        public string Token;
    }

    public class AlphaAdvantageConfig
    {
        public string Key;
    }

    public class DatabaseConfig
    {
        public string ConnectionString;
    }

    public class YoutubeDlConfig
    {
        public string RateLimit;
        public string InProgressDownloadFolder;
        public string CompleteDownloadFolder;

        public string YoutubeDlBinaryPath;
        public string FprobeBinaryPath;
    }

    public class MlConfig
    {
        public string BaseModelPath;
        public string BaseDatasetsPath;
        public string TempTrainingCache;

        public SentimentConfig Sentiment;
    }

    public class SentimentConfig
    {
        public string ModelDirectory;
        public string TrainingDatasetDirectory;
        public string EvalDatasetDirectory;
    }
}

namespace Mute
{
    public class Configuration
    {
        public AlphaAdvantageConfig AlphaAdvantage;
        public DatabaseConfig Database;
        public YoutubeDlConfig YoutubeDl;

        public HandlerConfiguration Handler;
    }

    public class HandlerConfiguration
    {
        public DiscordProviderConfiguration Discord;
        public LocalProviderConfiguration Local;
    }

    public class DiscordProviderConfiguration
    {
        public string Token;
    }

    public class LocalProviderConfiguration
    {

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
}

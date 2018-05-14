namespace Mute
{
    public class Configuration
    {
        public AuthConfig Auth;
        public AlphaAdvantageConfig AlphaAdvantage;
        public DatabaseConfig Database { get; set; }
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
        
    }
}

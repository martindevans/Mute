using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Services;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Sentiment;

namespace Mute.Moe.Discord.Services
{
    public class SentimentTrainingService
    {
        private const string InsertTaggedSentimentData = "INSERT INTO TaggedSentimentData (Content, Score) values(@Content, @Score)";
        private const string SelectTaggedSentimentData = "SELECT * FROM TaggedSentimentData";

        private readonly IDatabaseService _database;

        public SentimentTrainingService([NotNull] Configuration config, IDatabaseService database)
        {
            _database = database;

            // Create database structure
            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `TaggedSentimentData` (`Content` TEXT NOT NULL UNIQUE, `Score` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task Teach([NotNull] string text, Sentiment sentiment)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertTaggedSentimentData;
                cmd.Parameters.Add(new SQLiteParameter("@Content", System.Data.DbType.String) { Value = text.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@Score", System.Data.DbType.String) { Value = ((int)sentiment).ToString() });
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Sentiment learned: `{text}` == {sentiment}");
        }
    }
}

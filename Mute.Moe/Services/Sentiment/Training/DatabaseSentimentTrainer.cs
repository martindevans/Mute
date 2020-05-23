using System;
using System.Data.SQLite;
using System.Threading.Tasks;

using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Sentiment.Training
{
    public class DatabaseSentimentTrainer
        : ISentimentTrainer
    {
        private const string InsertTaggedSentimentData = "INSERT INTO TaggedSentimentData (Content, Score) values(@Content, @Score)";

        private readonly IDatabaseService _database;

        public DatabaseSentimentTrainer( Configuration config, IDatabaseService database)
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

        public async Task Teach(string text, Sentiment sentiment)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertTaggedSentimentData;
                cmd.Parameters.Add(new SQLiteParameter("@Content", System.Data.DbType.String) { Value = text.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@Score", System.Data.DbType.String) { Value = ((int)sentiment).ToString() });
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}

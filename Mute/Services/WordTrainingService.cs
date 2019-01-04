using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services
{
    public class WordTrainingService
    {
        private const string InsertWordExampleData = "INSERT INTO WordExampleData (Word, Example) values(@Word, @Example)";
        private const string SelectWordExampleData = "SELECT * FROM WordExampleData";

        private readonly IDatabaseService _database;

        public WordTrainingService([NotNull] Configuration config, IDatabaseService database)
        {
            _database = database;

            // Create database structure
            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `WordExampleData` (`Word` TEXT NOT NULL, `Example` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task Teach([NotNull] string word, [NotNull] string exampleSentence)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertWordExampleData;
                cmd.Parameters.Add(new SQLiteParameter("@Word", System.Data.DbType.String) { Value = word.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@Example", System.Data.DbType.String) { Value = exampleSentence.ToLowerInvariant() });
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Example learned: `{word}` e.g. `{exampleSentence}`");
        }
    }
}

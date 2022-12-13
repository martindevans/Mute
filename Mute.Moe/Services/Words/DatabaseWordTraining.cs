using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Words;

public class DatabaseWordTraining
    : IWordTraining
{
    private const string InsertWordExampleData = "INSERT INTO WordExampleData (Word, Example) values(@Word, @Example)";
    private const string SelectWordExampleData = "SELECT * FROM WordExampleData WHERE (Word = @Word OR @Word IS NULL)";

    private readonly IDatabaseService _database;

    public DatabaseWordTraining(IDatabaseService database)
    {
        _database = database;

        // Create database structure
        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `WordExampleData` (`Word` TEXT NOT NULL, `Example` TEXT NOT NULL, PRIMARY KEY(`Word`,`Example`))");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task Train(string word, string exampleSentence)
    {
        await using (var cmd = _database.CreateCommand())
        {
            cmd.CommandText = InsertWordExampleData;
            cmd.Parameters.Add(new SQLiteParameter("@Word", System.Data.DbType.String) {Value = word.ToLowerInvariant()});
            cmd.Parameters.Add(new SQLiteParameter("@Example", System.Data.DbType.String) {Value = exampleSentence.ToLowerInvariant()});
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"Example learned: `{word}` e.g. `{exampleSentence}`");
    }

    public IAsyncEnumerable<(string word, string example)> GetData(string? word)
    {
        static (string, string) ParseSubscription(DbDataReader reader)
        {
            return (
                reader["Word"].ToString() ?? "",
                reader["Example"].ToString() ?? ""
            );
        }

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = SelectWordExampleData;
            cmd.Parameters.Add(new SQLiteParameter("@Word", System.Data.DbType.String) { Value = (object?)word ?? DBNull.Value });
            return cmd;
        }

        return new SqlAsyncResult<(string, string)>(_database, PrepareQuery, ParseSubscription);
    }
}